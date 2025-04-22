using System;
using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshSurface))]
public class NavMeshGenA : MonoBehaviour
{
    Terrain terrain;
    TerrainData terrainData;
    Vector3 terrainPos;

    public string NavAgentLayer = "Default";
    public string defaultarea = "Walkable";
    public bool includeTrees;
    public float timeLimitInSecs = 30;
    public int step = 10;
    public List<string> areaID;

    [SerializeField] bool _destroyTempObjects;
    [SerializeField] bool _break;

    [ContextMenu("Generate NavAreas")]
    void Build()
    {
        EditorCoroutineUtility.StartCoroutine(GenMeshes(), this);
    }

    IEnumerator GenMeshes()
    {
        terrain = GetComponent<Terrain>();
        terrainData = terrain.terrainData;
        terrainPos = terrain.transform.position;

        Vector3 size = terrainData.size;
        Vector3 tpos = terrain.GetPosition();
        float minX = tpos.x;
        float maxX = minX + size.x;
        float minZ = tpos.z;
        float maxZ = minZ + size.z;

        GameObject attachParent;
        Transform childA = terrain.transform.Find("Delete me");

        if (childA != null)
        {
            attachParent = childA.gameObject;
        }
        else
        {
            attachParent = new GameObject("Delete me");
            attachParent.transform.SetParent(terrain.transform);
            attachParent.transform.localPosition = Vector3.zero;
        }

        yield return null;

        int terrainLayer = LayerMask.NameToLayer(NavAgentLayer);
        if (terrainLayer == -1)
        {
            Debug.LogError($"Invalid layer: {NavAgentLayer}. Using Default.");
            terrainLayer = LayerMask.NameToLayer("Default");
        }

        float[,,] splatmapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
        float alphaWidth = terrainData.alphamapWidth;
        float alphaHeight = terrainData.alphamapHeight;
        float tWidth = terrainData.size.x;
        float tHeight = terrainData.size.z;
        float startTime = Time.realtimeSinceStartup;
        float xStepsize = tWidth / alphaWidth;
        float zStepsize = tHeight / alphaHeight;

        int volumeCount = 0;
        for (int dx = 0; dx < alphaWidth; dx += step)
        {
            float xOff = tWidth * (dx / alphaWidth);
            for (int dz = 0; dz < alphaHeight; dz += step)
            {
                if (_break || Time.realtimeSinceStartup > startTime + timeLimitInSecs)
                {
                    Debug.LogWarning($"NavMesh generation interrupted: _break={_break}, TimeLimit={Time.realtimeSinceStartup - startTime}s");
                    yield break;
                }

                float zOff = tHeight * (dz / alphaHeight);
                int surface = GetMainTextureA(dz, dx, ref splatmapData);
                Debug.Log($"Texture at ({dx}, {dz}): {surface}");

                if (!areaID.Contains(surface.ToString()))
                {
                    Debug.Log($"Skipping texture {surface} (not in areaID: {string.Join(",", areaID)})");
                    continue;
                }

                Vector3 pos = new Vector3(minX + xOff, terrain.SampleHeight(new Vector3(minX + xOff, 0, minZ + zOff)), minZ + zOff);

                GameObject obj = new GameObject($"NavMod_{dx}_{dz}");
                obj.layer = terrainLayer;
                Transform objT = obj.transform;
                objT.SetParent(attachParent.transform);
                objT.position = pos;

                NavMeshModifierVolume nmmv = obj.AddComponent<NavMeshModifierVolume>();
                nmmv.size = new Vector3(xStepsize * step, 1, zStepsize * step);
                nmmv.center = Vector3.zero;
                int areaIndex = NavMesh.GetAreaFromName(defaultarea);
                if (areaIndex == -1)
                {
                    Debug.LogError($"Invalid NavMesh area: {defaultarea}. Using Walkable.");
                    areaIndex = NavMesh.GetAreaFromName("Walkable");
                }
                nmmv.area = areaIndex;
                volumeCount++;
                Debug.Log($"Created NavMeshModifierVolume {volumeCount} at ({pos.x}, {pos.z}) with area {defaultarea} (index {areaIndex})");

                yield return null;
            }
        }

        if (includeTrees)
        {
            TreeInstance[] instances = terrainData.treeInstances;
            TreePrototype[] prototypes = terrainData.treePrototypes;
            Vector3 tsize = terrainData.size;

            foreach (TreeInstance inst in instances)
            {
                TreePrototype prototype = prototypes[inst.prototypeIndex];
                Vector3 pos = Vector3.Scale(inst.position, tsize) + terrainPos;
                GameObject tree = Instantiate(prototype.prefab, attachParent.transform);
                tree.layer = terrainLayer;
                Transform objT = tree.transform;
                objT.position = pos;
                objT.rotation = Quaternion.Euler(0, inst.rotation * Mathf.Rad2Deg, 0);
                objT.localScale = new Vector3(inst.widthScale, inst.heightScale, inst.widthScale);
                NavMeshObstacle obstacle = tree.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.size = new Vector3(1, 2, 1);
                tree.isStatic = true;
            }
        }

        NavMeshSurface nsurface = GetComponent<NavMeshSurface>();
        if (nsurface != null)
        {
            nsurface.BuildNavMesh();
            Debug.Log("NavMesh built successfully.");
            yield return null;
        }
        else
        {
            Debug.LogError("No NavMeshSurface found on this GameObject.");
        }

        if (_destroyTempObjects)
        {
            Debug.Log($"Destroying {attachParent.transform.childCount} temporary objects.");
            DestroyImmediate(attachParent);
        }
    }

    private float[] GetTextureMixA(int z, int x, ref float[,,] splatmapData)
    {
        float[] cellMix = new float[splatmapData.GetLength(2)];
        try
        {
            for (int n = 0; n < cellMix.Length; n++)
            {
                cellMix[n] = splatmapData[z, x, n];
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accessing splatmap at ({z}, {x}): {e}");
        }
        return cellMix;
    }

    private int GetMainTextureA(int z, int x, ref float[,,] splatmapData)
    {
        float[] mix = GetTextureMixA(z, x, ref splatmapData);
        float maxMix = 0;
        int maxIndex = 0;
        for (int n = 0; n < mix.Length; n++)
        {
            if (mix[n] > maxMix)
            {
                maxIndex = n;
                maxMix = mix[n];
            }
        }
        return maxIndex;
    }
}