#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arena.Client.PreviewRendering
{
    public class ItemRenderStudio : MonoBehaviour
    {
        public Camera Camera;
        public string SavePath;
        public RenderTexture RT;
        public Material RT_Material;
        public Transform ItemContainer;
        public string RenderTag;
        public int Width;
        public int Height;

        [ContextMenu("Render")]
        public void Render()
        {
            var childs = ItemContainer.GetComponentsInChildren<Transform>();
            var renderList = new List<Transform>();

            foreach (var child in childs)
            {
                if (child.gameObject.activeInHierarchy == false)
                {
                    continue;
                }
                
                if (child.CompareTag(RenderTag))
                {
                    renderList.Add(child);
                }
            }

            foreach (var renderObj in renderList)
            {
                renderObj.gameObject.SetActive(false);
            }

            foreach (var renderObj in renderList)
            {
                renderObj.gameObject.SetActive(true);

                var renderers = renderObj.GetComponentsInChildren<Renderer>();

                Dictionary<Renderer, Material> replaced = new();

                foreach (var renderer1 in renderers)
                {
                    if (renderer1.sharedMaterial && renderer1.sharedMaterial.HasProperty("_Cull"))
                    {
                        replaced.Add(renderer1, renderer1.sharedMaterial);
                        
                        var instance = Instantiate(renderer1.sharedMaterial);
                        instance.name += "IRS_CUSTOM";
                        instance.SetFloat("_Cull", 0);
                        instance.SetInt("_Cull", 0);
                        renderer1.sharedMaterial = instance;
                    }
                }
                
                Camera.Render();
                var temp = RenderTexture.GetTemporary(Width, Height);
                Graphics.Blit(RT, temp, RT_Material);
                RenderTexture.active = temp;
                var format = /*hdr ? TextureFormat.RGBAFloat : */TextureFormat.RGBA32;
                var result = new Texture2D(Width, Height, format, false);
                result.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(temp);
                result.Apply();

                var bytes = result.EncodeToPNG();
                var filePath = SavePath + renderObj.name + ".png";
                var fullPath = Path.Combine(Application.dataPath, filePath);
                File.WriteAllBytes(fullPath, bytes);
                AssetDatabase.ImportAsset("Assets/" + filePath);

                foreach (var rm in replaced)
                {
                    rm.Key.sharedMaterial = rm.Value;
                }
                
                renderObj.gameObject.SetActive(false);
            }
            
            foreach (var renderObj in renderList)
            {
                renderObj.gameObject.SetActive(true);
            }
        }
    }
}
#endif