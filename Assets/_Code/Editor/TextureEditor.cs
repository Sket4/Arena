using System;
using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Arena.Editor
{
    public class VerticalGUILayout : IDisposable
    {
        public VerticalGUILayout()
        {
            GUILayout.BeginVertical();
        }

        public void Dispose()
        {
            GUILayout.EndVertical();
        }
    }

    public class HorizontalGUILayout : IDisposable
    {
        public HorizontalGUILayout()
        {
            GUILayout.BeginHorizontal();
        }

        public void Dispose()
        {
            GUILayout.EndHorizontal();
        }
    }

    public class TextureEditor : EditorWindow
    {
        [SerializeField]
        float texturePreviewSize = 512;

        [SerializeField]
        Texture2D sourceTexture;

        [SerializeField]
        Texture2D resultTexture;

        [SerializeField]
        float normalStrengthScale = 2;

        [SerializeField]
        float contrast = 2;

        [SerializeField] private float saturation = 1;

        [SerializeField] private float brightness = 1;

        [MenuItem("Arena/Утилиты/Редактор текстур")]
        static void show()
        {
            var window = GetWindow<TextureEditor>();
            window.Show();
        }

        private void OnGUI()
        {
            using(new HorizontalGUILayout())
            {
                const float settingsWidth = 200;

                //if(GUILayout.Button("create texture"))
                //{
                //    Texture2D tt = new Texture2D(4, 4, TextureFormat.RGBA32, default);
                //    var pixels = tt.GetPixels();
                //    for (int i = 0; i < pixels.Length; i++)
                //    {
                //        pixels[i] = new Color(0, 0, 0, 0);
                //    }
                //    tt.SetPixels(pixels);

                //    var bytes = tt.EncodeToTGA();
                //    File.WriteAllBytes("black.tga", bytes);
                //}

                using(new VerticalGUILayout())
                {
                    sourceTexture = EditorGUILayout.ObjectField("Исходная текстура", sourceTexture, typeof(Texture2D), false, GUILayout.Width(settingsWidth)) as Texture2D;
                    GUILayout.Space(10);
                    texturePreviewSize = EditorGUILayout.FloatField("Размер превью", texturePreviewSize, GUILayout.Width(settingsWidth));
                }
                
                if(sourceTexture != null)
                {
                    GUILayout.Space(10);

                    using(new VerticalGUILayout())
                    {
                        normalStrengthScale = EditorGUILayout.FloatField("Усиление нормали", normalStrengthScale);

                        if (GUILayout.Button("Усилить нормали"))
                        {
                            normalStrength(normalStrengthScale);
                        }
                    }

                    GUILayout.Space(10);

                    using (new VerticalGUILayout())
                    {
                        contrast = EditorGUILayout.FloatField("Контраст", contrast);
                        brightness = EditorGUILayout.FloatField("Яркость", brightness);
                        saturation = EditorGUILayout.FloatField("Сатурация", saturation);

                        if (GUILayout.Button("Применить коррекцию цвета"))
                        {
                            colorCorrection(contrast, brightness, saturation);
                        }
                    }
                    
                    using (new VerticalGUILayout())
                    {
                        if (GUILayout.Button("Roughness -> Smoothness"))
                        {
                            roughnessToSmoothness();
                        }
                        
                        if (GUILayout.Button("Restore normals"))
                        {
                            restoreNormals();
                        }
                    }
                }

                if (resultTexture != null)
                {
                    GUILayout.Space(10);

                    if (GUILayout.Button("Сохранить результат"))
                    {
                        saveResultTexture();
                    }
                }
            }

            GUILayout.BeginHorizontal();
            {
                GUILayout.Box(sourceTexture, GUILayout.Width(texturePreviewSize), GUILayout.Height(texturePreviewSize));
                GUILayout.Space(10);
                GUILayout.Box(resultTexture, GUILayout.Width(texturePreviewSize), GUILayout.Height(texturePreviewSize));
            }
            GUILayout.EndHorizontal();
        }

        private void saveResultTexture()
        {
            var bytes = resultTexture.EncodeToPNG();

            var defaultName = sourceTexture != null ? sourceTexture.name : "result";
            var savePath = EditorUtility.SaveFilePanelInProject("Сохранить результат", defaultName, "png", "Выберите путь сохранения");

            if(string.IsNullOrEmpty(savePath))
            {
                return;
            }
            File.WriteAllBytes(savePath, bytes);
            AssetDatabase.ImportAsset(savePath);
        }

        void clearTexture()
        {
            if(resultTexture == null)
            {
                return;
            }
            Debug.Log("Destroying temp result texture...");
            DestroyImmediate(resultTexture);
            resultTexture = null;
        }

        private void OnDisable()
        {
            clearTexture();
        }


        void modifyTexture(Action<Color[]> actionCallback, bool useAlpha = false)
        {
            clearTexture();

            if (useAlpha)
            {
                resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);    
            }
            else
            {
                resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGB24, false);    
            }
            
            var rt = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height);

            try
            {
                Graphics.Blit(sourceTexture, rt);
                RenderTexture.active = rt;

                resultTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                resultTexture.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                rt = null;

                var sourcePixels = resultTexture.GetPixels();

                actionCallback(sourcePixels);

                resultTexture.SetPixels(sourcePixels);
                resultTexture.Apply();
            }
            finally
            {
                if (rt != null)
                {
                    RenderTexture.ReleaseTemporary(rt);
                }
            }
        }

        void colorCorrection(float contrast, float brightness, float saturation)
        {
            modifyTexture((sourcePixels) =>
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    ref var pixel = ref sourcePixels[i];
                    
                    Color.RGBToHSV(pixel, out var h, out var s, out var v);
                    s = Mathf.Clamp01(saturation * s);
                    pixel = Color.HSVToRGB(h, s, v);
                    
                    pixel.r = Mathf.Pow(pixel.r, contrast);
                    pixel.g = Mathf.Pow(pixel.g, contrast);
                    pixel.b = Mathf.Pow(pixel.b, contrast);

                    var br = brightness;// - 1.0f;
                    pixel.r = Mathf.Max(0, pixel.r * br);
                    pixel.g = Mathf.Max(0, pixel.g * br);
                    pixel.b = Mathf.Max(0, pixel.b * br);
                }
            });
        }

        void roughnessToSmoothness()
        {
            modifyTexture((sourcePixels) =>
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    ref Color pixel = ref sourcePixels[i];
                    pixel.r = 1.0f - pixel.r;
                    pixel.g = 1.0f - pixel.g;
                    pixel.b = 1.0f - pixel.b;
                }
            });
        }

        void restoreNormals()
        {
            modifyTexture((sourcePixels =>
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    ref Color pixel = ref sourcePixels[i];

                    var normal = new Vector3(pixel.r * 2 - 1, pixel.g * 2 - 1, 1);
                    normal.z = Mathf.Sqrt(1.0f - Mathf.Clamp01(Vector2.Dot(normal, normal)));
                    
                    normal.Normalize();

                    normal.x = (normal.x + 1.0f) / 2.0f;
                    normal.y = (normal.y + 1.0f) / 2.0f;
                    normal.z = (normal.z + 1.0f) / 2.0f;
                    
                    pixel.r = normal.x;
                    pixel.g = normal.y;
                    pixel.b = normal.z;
                }
            }));
        }

        void normalStrength(float normalStrength)
        {
            modifyTexture((sourcePixels) =>
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    ref Color pixel = ref sourcePixels[i];
                    //pixel.r = (pixel.r - 0.5f) * 2;
                    //pixel.g = (pixel.g - 0.5f) * 2;
                    //pixel.b = (pixel.b - 0.5f) * 2;

                    pixel.r *= normalStrength;
                    pixel.g *= normalStrength;
                    pixel.b = math.lerp(1.0f, pixel.b, math.saturate(normalStrength));
                    
                    // var normalized = new Vector3(pixel.r, pixel.g, pixel.b).normalized;
                    // normalized.x = (normalized.x + 1) * 0.5f;
                    // normalized.y = (normalized.y + 1) * 0.5f;
                    // normalized.z = (normalized.z + 1) * 0.5f;
                    //
                    // pixel.r = normalized.x;
                    // pixel.g = normalized.y;
                    // pixel.b = normalized.z;

                    pixel.a = 0;
                }
            });
        }
    }
}
