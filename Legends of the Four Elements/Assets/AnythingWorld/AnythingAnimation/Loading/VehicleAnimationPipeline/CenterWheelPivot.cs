using AnythingWorld.Utilities;
using System;
using UnityEngine;

namespace AnythingWorld.Animation.Vehicles
{
    [Serializable]
    public static class CenterWheelPivot
    {
        /// <summary>
        /// Return game object 
        /// </summary>
        /// <param name="modelPart"></param>
        /// <returns></returns>
        public static GameObject CenterWheel(GameObject modelPart)
        {
            //Find object in wheel with mesh on
            var partMesh = modelPart.GetComponentInChildren<MeshFilter>().gameObject;
            //Create new object to pivot on
            var verticalPivotObject = new GameObject("VerticalMeshPivot").gameObject;
            //Parent to model part.
            verticalPivotObject.transform.parent = modelPart.transform;
            //Set location of pivot to model part zero;
            verticalPivotObject.transform.localPosition = Vector3.zero;
            
            //Get Wheel mesh
            var partMeshFilter = partMesh.GetComponent<MeshFilter>();
            //Get renderer
            var wheelRenderer = partMesh.GetComponent<MeshRenderer>();
            //Get position of center of mesh in local coordinates
            var meshCenterPositionLocal = partMeshFilter.sharedMesh.bounds.center;
            //Convert local mesh center position to world coordinates
            var meshCenterPositionWorld = partMesh.transform.TransformPoint(meshCenterPositionLocal);
            //Set position ov pivot object to world centre location of mesh
            verticalPivotObject.transform.position = meshCenterPositionWorld;
            //Create new wheel mesh
            var newWheel = UnityEngine.Object.Instantiate(partMesh, modelPart.transform, true);
            //Set parent to model part.
            //Set scale, position and rotation to same as meshobject
            newWheel.transform.SetPositionAndRotation(partMesh.transform.position, partMesh.transform.rotation);
            newWheel.transform.localScale = partMesh.transform.localScale;
            //Enable mesh renderer.
            newWheel.GetComponent<MeshRenderer>().enabled = true;
            //parent new wheel to new pivot object.
            newWheel.transform.parent = verticalPivotObject.transform;
            var lateralPitchObject = new GameObject("LateralMeshPivot").gameObject;
            lateralPitchObject.transform.parent = modelPart.transform;
            lateralPitchObject.transform.position = verticalPivotObject.transform.position;
            verticalPivotObject.transform.parent = lateralPitchObject.transform;
            //destroy wheel renderer
            Destroy.GameObject(wheelRenderer.gameObject);

            return lateralPitchObject;
        }

        /// <summary>
        /// Return game object 
        /// </summary>
        /// <param name="modelPart"></param>
        /// <param name="meshCenterPositionWorld"></param>
        /// <returns></returns>
        public static GameObject CenterMeshCustomPivot(GameObject modelPart, Vector3 meshCenterPositionWorld)
        {
            //Find object in wheel with mesh on
            var partMesh = modelPart.GetComponentInChildren<MeshFilter>().gameObject;
            //Create new object to pivot on
            var verticalPivotObject = new GameObject("VerticalMeshPivot").gameObject;
            //Parent to model part.
            verticalPivotObject.transform.parent = modelPart.transform;
            //Set location of pivot to model part zero;
            verticalPivotObject.transform.localPosition = Vector3.zero;
            
            //Get renderer
            var wheelRenderer = partMesh.GetComponent<MeshRenderer>();
            //Set position ov pivot object to world centre location of mesh
            verticalPivotObject.transform.position = meshCenterPositionWorld;
            //Create new wheel mesh
            var newWheel = UnityEngine.Object.Instantiate(partMesh, modelPart.transform, true);
            //Set parent to model part.
            //Set scale, position and rotation to same as meshobject
            newWheel.transform.SetPositionAndRotation(partMesh.transform.position, partMesh.transform.rotation);
            newWheel.transform.localScale = partMesh.transform.localScale;
            //Enable mesh renderer.
            newWheel.GetComponent<MeshRenderer>().enabled = true;
            //parent new wheel to new pivot object.
            newWheel.transform.parent = verticalPivotObject.transform;
            var lateralPitchObject = new GameObject("LateralMeshPivot").gameObject;
            lateralPitchObject.transform.parent = modelPart.transform;
            lateralPitchObject.transform.position = verticalPivotObject.transform.position;
            verticalPivotObject.transform.parent = lateralPitchObject.transform;
            //destroy wheel renderer
            Destroy.GameObject(wheelRenderer.gameObject);

            return lateralPitchObject;
        }
    }
}
