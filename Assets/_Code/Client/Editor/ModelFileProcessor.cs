using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Arena.Editor
{
    public class ModelFileProcessor : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            var guid = AssetDatabase.GUIDFromAssetPath(assetPath);

            var labels = AssetDatabase.GetLabels(guid);

            if (labels.Contains("Custom-compression"))
            {
                return;
            }
            
            var modelImporter = assetImporter as ModelImporter;
            var compression = modelImporter.meshCompression;
            var assPath = assetPath.ToLower();

            if (assPath.Contains("water") || assPath.Contains("terrain"))
            {
                if (compression == ModelImporterMeshCompression.High ||
                    compression == ModelImporterMeshCompression.Medium)
                {
                    compression = ModelImporterMeshCompression.Low;
                }
            }
            else
            {
                if (compression == ModelImporterMeshCompression.Low ||
                    compression == ModelImporterMeshCompression.Off)
                {
                    compression = ModelImporterMeshCompression.Medium;
                }
            }
            
            if (compression != modelImporter.meshCompression)
            {
                Debug.Log($"changed mesh compression to {compression} for {assetPath}");
                modelImporter.meshCompression = compression;
            }
        }
    }
}
