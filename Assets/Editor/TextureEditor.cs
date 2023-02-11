using System;
using System.IO;
using UnityEditor;
using UnityEngine;

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

                        if (GUILayout.Button("Применить коррекцию цвета"))
                        {
                            colorCorrection(contrast);
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


        void modifyTexture(Action<Color[]> actionCallback)
        {
            clearTexture();

            resultTexture = new Texture2D(sourceTexture.width, sourceTexture.height, TextureFormat.RGB24, false);

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

        void colorCorrection(float contrast)
        {
            modifyTexture((sourcePixels) =>
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    ref Color pixel = ref sourcePixels[i];
                    pixel.r = Mathf.Pow(pixel.r, contrast);
                    pixel.g = Mathf.Pow(pixel.g, contrast);
                    pixel.b = Mathf.Pow(pixel.b, contrast);
                }
            });
        }

        void normalStrength(float normalStrength)
        {
            modifyTexture((sourcePixels) =>
            {
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    ref Color pixel = ref sourcePixels[i];
                    pixel.r = (pixel.r - 0.5f) * 2;
                    pixel.g = (pixel.g - 0.5f) * 2;
                    pixel.b = (pixel.b - 0.5f) * 2;

                    pixel.g *= normalStrength;
                    //pixel.b *= normalStrength;

                    var normalized = new Vector3(pixel.r, pixel.g, pixel.b).normalized;
                    normalized.x = (normalized.x + 1) * 0.5f;
                    normalized.y = (normalized.y + 1) * 0.5f;
                    normalized.z = (normalized.z + 1) * 0.5f;

                    pixel.r = normalized.x;
                    pixel.g = normalized.y;
                    pixel.b = normalized.z;

                    pixel.a = 0;
                }
            });
        }
    }
}
