#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Arena.Client
{
    public class ScenePreviewRender : MonoBehaviour
    {
        [Header("Context menu => Render")]
        public Camera Camera;
        public Vector2Int Resolution = new(1920, 1080);
        public string SavePath;

        private void Reset()
        {
            Camera = GetComponent<Camera>();
        }

        [ContextMenu("Render")]
        void Render()
        {
            var temp = RenderTexture.GetTemporary(Resolution.x, Resolution.y);
            Camera.targetTexture = temp;
            Camera.Render();
            Camera.targetTexture = null;
            RenderTexture.active = temp;
            var result = new Texture2D(Resolution.x, Resolution.y);
            result.ReadPixels(new Rect(0,0,Resolution.x, Resolution.y), 0, 0);
            result.Apply();

            var bytes = result.EncodeToPNG();
            var path = SavePath + gameObject.scene.name + ".png";
            File.WriteAllBytes(Path.Combine(Application.dataPath, path), bytes);
            UnityEditor.AssetDatabase.ImportAsset("Assets/" + path);
        
            Destroy(result);
            RenderTexture.ReleaseTemporary(temp);
        }
    }    
}
#endif