using UnityEditor;
#if !UNITY_2020_2_OR_NEWER
using UnityEditor.Experimental.AssetImporters;
#else
using UnityEditor.AssetImporters;
#endif
using UnityEngine;

namespace AnythingWorld.GLTFUtility {
	[ScriptedImporter(1, "gltf")]
	public class GLTFImporter : ScriptedImporter {

		public ImportSettings importSettings;

		public override void OnImportAsset(AssetImportContext ctx) {
			EditorUtility.DisplayDialog("GLTF File Format Not Supported", "Using GLTF formats in your Anything World project is currently unsupported. Please convert your file into a supported format.", "I understand");

			//// Load asset
            //AnimationClip[] animations;
			//if (importSettings == null) importSettings = new ImportSettings();
			//GameObject root = Importer.LoadFromFile(ctx.assetPath, importSettings, out animations, Format.GLTF);
			//
			//// Save asset
			//GLTFAssetUtility.SaveToAsset(root, animations, ctx, importSettings);
		}
	}
}