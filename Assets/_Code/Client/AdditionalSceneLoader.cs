using UnityEngine;
using UnityEngine.SceneManagement;

namespace Arena.Client
{
    [ExecuteInEditMode]
    public class AdditionalSceneLoader : MonoBehaviour
    {
        public string[] ScenePaths;

#if UNITY_EDITOR
        bool pendingSceneLoad = false;
#endif

        private void OnEnable()
        {
#if UNITY_EDITOR
            if(Application.isPlaying)
            {
                load();
            }
            else
            {
                pendingSceneLoad = true;
                UnityEditor.EditorApplication.update += EditorUpdate;
            }
#else
            load();
#endif
        }

#if UNITY_EDITOR
        private void OnDisable()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
        }

        void EditorUpdate()
        {
            if(pendingSceneLoad == false)
            {
                return;
            }
            pendingSceneLoad = false;

            load();
        }
#endif

        void load()
        {
            if (ScenePaths == null || ScenePaths.Length == 0)
            {
                return;
            }

            foreach (var scenePath in ScenePaths)
            {
                if (string.IsNullOrEmpty(scenePath))
                {
                    continue;
                }

                var scene = SceneManager.GetSceneByPath(scenePath);
                var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                if (scene.isLoaded)
                {
                    continue;
                }

                bool ignore = false;

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var sc = SceneManager.GetSceneAt(i);
                    if (sc.path == scenePath)
                    {
                        ignore = true;
                        break;
                    }
                }

                if (ignore)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (Application.isPlaying)
                {

                    SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
                }
                else
                {
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                }
#else
                SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
#endif
            }
        }
    }
}
