using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Text;
using Arena;
using Arena.Client;
using Arena.Client.Anima;
using UniRx;
using TzarGames.GameCore.Abilities.Editor;
using TzarGames.GameCore.Editor.Abilities;
using TzarGames.CodeGeneration;
using TzarGames.Common;
using TzarGames.GameCore.Client.Physics;
using Unity.Mathematics;
using Console = System.Console;
using Object = UnityEngine.Object;

public static class Tools
{
    [InitializeOnLoadMethod]
    static void initMenus()
    {
        GameState.UseLocalServer = EditorPrefs.GetBool(localServerModeKey, false);
        GameState.IsOfflineMode = EditorPrefs.GetBool(offlineModeKey, true);

        EditorApplication.delayCall += () =>
        {
            Menu.SetChecked(localServerMode, GameState.UseLocalServer);
            Menu.SetChecked(offlineMode, GameState.IsOfflineMode);
        };
    }


    const string localServerMode = "Arena/Локальный сервер";
    const string localServerModeKey = "localServerMode";
    const string offlineMode = "Arena/Локальный режим";
    const string offlineModeKey = "offlineMode";

    [MenuItem(localServerMode)]
    static void toggleLocalServerMode()
    {
        GameState.UseLocalServer = !GameState.UseLocalServer;
        Menu.SetChecked(localServerMode, GameState.UseLocalServer);
        EditorPrefs.SetBool(localServerModeKey, GameState.UseLocalServer);
        Debug.Log($"Режим локального сервера: {GameState.UseLocalServer}");
    }
    
    [MenuItem(offlineMode)]
    static void toggleOfflineMode()
    {
        GameState.IsOfflineMode = !GameState.IsOfflineMode;
        Menu.SetChecked(offlineMode, GameState.IsOfflineMode);
        EditorPrefs.SetBool(offlineModeKey, GameState.IsOfflineMode);
        Debug.Log($"Режим оффлайн игры: {GameState.IsOfflineMode}");
    }

    
    [MenuItem("Arena/Документация/Настройка префабов персонажей")]
    static void openDoc_characterSetup()
    {
        var path = getDocFilePath("character_setup.md");
        UnityEngine.Debug.Log(path);
        System.Diagnostics.Process.Start(path);
        //Application.OpenURL(path);
    }
    
    [MenuItem("Arena/Утилиты/Показать скрытые объекты на сцене")]
    static void showHiddenObjectsInScene()
    {
        var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

        var rootGameObjects = activeScene.GetRootGameObjects();

        foreach (var go in rootGameObjects)
        {
            go.hideFlags &= ~HideFlags.HideInHierarchy;
            EditorUtility.SetDirty(go);
        }
    }

    const string clientAbilityCodegenSettingsPath = "Assets/Data/Codegen/Client ability codegen settings.asset";
    const string serverAbilityCodegenSettingsPath = "Assets/Data/Codegen/Server ability codegen settings.asset";
    const string commonAssemblyCodegenSettingsPath = "Assets/Data/Codegen/Common assembly codegen settings.asset";
    const string clientAssemblyCodegenSettingsPath = "Assets/Data/Codegen/Client assembly codegen settings.asset";

    [MenuItem("Arena/Генерация/Сгенерировать код для клиента")]
    static void generateClientCode()
    {
        var asset = loadAbilityCodegenAsset(clientAbilityCodegenSettingsPath);
        AbilityCodegenSettingsAssetEditor.GenerateAll(asset, asset.Abilities);

        generateClientAssemblyCode();
    }

    [MenuItem("Arena/Генерация/Сгенерировать код для сервера")]
    static void generateServerCode()
    {
        var asset = loadAbilityCodegenAsset(serverAbilityCodegenSettingsPath);
        AbilityCodegenSettingsAssetEditor.GenerateAll(asset, asset.Abilities);
    }

    [MenuItem("Arena/Генерация/Сгенерировать код для всех платформ")]
    static void generateAllCode()
    {
        generateClientCode();
        generateServerCode();
        generateCommonAssemblyCode();
    }

    private static void generateCommonAssemblyCode()
    {
        var commonAssembly = loadAssemblyCodegenAsset(commonAssemblyCodegenSettingsPath);
        CodeGeneratorTools.GenerateAssemblyCode(commonAssembly);
    }

    private static void generateClientAssemblyCode()
    {
        var assembly = loadAssemblyCodegenAsset(clientAssemblyCodegenSettingsPath);
        CodeGeneratorTools.GenerateAssemblyCode(assembly);
    }

    [MenuItem("Arena/Генерация/Удалить код")]
    static void deleteCode()
    {
        var serverSettings = loadAbilityCodegenAsset(serverAbilityCodegenSettingsPath);
        Directory.Delete(serverSettings.FullSavePath, true);
        var clientSettings = loadAbilityCodegenAsset(clientAbilityCodegenSettingsPath);
        Directory.Delete(clientSettings.FullSavePath, true);
        AssetDatabase.Refresh();

        var commonAssembly = loadAssemblyCodegenAsset(commonAssemblyCodegenSettingsPath);
        CodeGeneratorTools.Fix(commonAssembly);
    }

    static AbilityCodegenSettingsAsset loadAbilityCodegenAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath(path, typeof(AbilityCodegenSettingsAsset)) as AbilityCodegenSettingsAsset;
        return asset;
    }

    static AssemblyCodeGenerationSettingsAsset loadAssemblyCodegenAsset(string path)
    {
        var asset = AssetDatabase.LoadAssetAtPath(path, typeof(AssemblyCodeGenerationSettingsAsset)) as AssemblyCodeGenerationSettingsAsset;
        return asset;
    }

    static string getDocFilePath(string fileName)
    {
        return Path.Combine(Application.dataPath, "../Documentation/", fileName);
    }

    [ConsoleCommand]
    static void testAvatar()
    {
        var avt = Selection.activeObject as Avatar;

        if (avt.humanDescription.human != null)
        {
            foreach (var humanBone in avt.humanDescription.human)
            {
                Debug.Log($"Human bone name: {humanBone.boneName}, human name: {humanBone.humanName}");
            }    
        }
        if (avt.humanDescription.skeleton != null)
        {
            foreach (var skelBone in avt.humanDescription.skeleton)
            {
                Debug.Log($"Skel bone name: {skelBone.name}");
            }    
        }
    }
    
    [InitializeOnLoadMethod]
    static void registerDeInitCallback()
    {
            
        EditorApplication.playModeStateChanged += change =>
        {
            if (EditorSettings.enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload))
            {
                if (//change == PlayModeStateChange.ExitingPlayMode
                    change == PlayModeStateChange.EnteredEditMode
                )
                {
                    Debug.Log("Уничтожение UniRx при включенном Domain Reload (временное решение)");
                    setVal<MainThreadDispatcher>("initialized", false);
                    setVal<MainThreadDispatcher>("instance", null);
                    setVal<MainThreadDispatcher>("isQuitting", false);
                    setVal<MainThreadDispatcher>("mainThreadToken", null);
                    setVal<ScenePlaybackDetector>("_isPlaying", false);   
                }
                else if(change == PlayModeStateChange.EnteredPlayMode)
                {
                    Debug.Log("Инициализация UniRx при включенном Domain Reload (временное решение)");
                    setVal<ScenePlaybackDetector>("_isPlaying", true);   
                }
            }
        };
    }

        static void setVal<T>(string memberName, object value)
        {
            var type = typeof(T);
            var flags = BindingFlags.Static |
                        BindingFlags.NonPublic;
            
            var prop = type.GetField(memberName, flags);
            prop.SetValue(null, value);
        }

    class ToolsWindow : EditorWindow
    {
        string currentTypeHash;
        Material material1;
        Material material2;
        Shader shader;

        // prefab copier
        GameObject sourceObj;
        GameObject destinationObj;
        private string currentAssetGuid;

        [MenuItem("Arena/Утилиты/Органайзер")]
        static void show()
        {
            var w = GetWindow<ToolsWindow>();
            w.Show();
        }

        private void OnGUI()
        {
            material1 = EditorGUILayout.ObjectField("Материал #1", material1, typeof(Material), false) as Material;
            material2 = EditorGUILayout.ObjectField("Материал #2", material2, typeof(Material), false) as Material;
            shader = EditorGUILayout.ObjectField("Шейдер #1", shader, typeof(Shader), false) as Shader;

            if (material1 != null && material2 != null && GUILayout.Button("Заменить материал #1 на материал #2 на всей сцене"))
            {
                replaceMaterialsInScene();
            }

            if (Selection.activeGameObject != null && shader != null
                && GUILayout.Button("Заменить шейдер#1 на всех материалах на всех рендерерах под выбранным объектом"))
            {
                replaceShaderUnderSelection();
            }

            if (GUILayout.Button("Выбрать все меш рендереры под выбранными объектами"))
            {
                selectAllWithComponent<MeshRenderer>();
            }

            if(GUILayout.Button("Заменить текстуры на PNG"))
            {
                var lmArray = new LightmapData[LightmapSettings.lightmaps.Length];

                for (int i = 0; i < LightmapSettings.lightmaps.Length; i++)
                {
                    var lm = LightmapSettings.lightmaps[i];
                    SetLightmapTextureImporterFormat(lm.lightmapColor, true);

                    var png = lm.lightmapColor.EncodeToPNG();
                    var path = AssetDatabase.GetAssetPath(lm.lightmapColor);
                    var newPath = path.Replace(".exr", ".png");

                    File.WriteAllBytes(newPath, png);
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(newPath);

                    var newData = new LightmapData();
                    lm.lightmapColor = texture;
                    lmArray[i] = lm;

                    Debug.Log(path);
                }

                LightmapSettings.lightmaps = lmArray;

                //EditorUtility.SetDirty(Lightmapping.lightingDataAsset);

                //UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            GUILayout.Space(20);
            drawPrefabCopier();

            GUILayout.Space(20);
            drawHashType();
            
            GUILayout.Space(20);
            drawAssetGuidChecker();
        }

        void drawHashType()
        {
            currentTypeHash = EditorGUILayout.TextField("Хеш типа", currentTypeHash);

            if (ulong.TryParse(currentTypeHash, out ulong typeHash) == false)
            {
                currentTypeHash = "0";
            }

            if (GUILayout.Button("Проверить хеш тип"))
            {
                //13453056286499847482
                var index = Unity.Entities.TypeManager.GetTypeIndexFromStableTypeHash(typeHash);
                Debug.Log("TypeIndex: " + index);
                var type = Unity.Entities.TypeManager.GetType(index);
                Debug.Log("Type: " + type.Name);
            }
        }

        void drawAssetGuidChecker()
        {
            currentAssetGuid = EditorGUILayout.TextField("Asset GUID", currentAssetGuid);

            if (GUILayout.Button("Проверить AssetGUID"))
            {
                var path = AssetDatabase.GUIDToAssetPath(currentAssetGuid);
                if (string.IsNullOrEmpty(path))
                {
                    Debug.Log("Не найден путь для ассета с указанным GUID");
                }
                else
                {
                    Debug.Log(path);
                }
            }
        }

        void drawPrefabCopier()
        {
            GUILayout.Label("Скопировать компоненты");
            sourceObj = EditorGUILayout.ObjectField("Источник", sourceObj, typeof(GameObject), true) as GameObject;
            destinationObj = EditorGUILayout.ObjectField("Цель", destinationObj, typeof(GameObject), true) as GameObject;

            if(sourceObj != null && destinationObj != null)
            {
                if(GUILayout.Button("Скопировать"))
                {
                    var components = sourceObj.GetComponents<Component>();

                    foreach (var component in components)
                    {
                        try
                        {
                            UnityEditorInternal.ComponentUtility.CopyComponent(component);
                            UnityEditorInternal.ComponentUtility.PasteComponentAsNew(destinationObj);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        public static void SetLightmapTextureImporterFormat(Texture2D texture, bool isReadable)
        {
            if (null == texture) return;

            string assetPath = AssetDatabase.GetAssetPath(texture);
            var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (tImporter != null)
            {
                tImporter.textureType = TextureImporterType.Lightmap;
                tImporter.textureCompression = TextureImporterCompression.Uncompressed; 
                tImporter.isReadable = isReadable;

                AssetDatabase.ImportAsset(assetPath);
                AssetDatabase.Refresh();
            }
        }

        void selectAllWithComponent<T>() where T : Component
        {
            if (Selection.gameObjects == null || Selection.gameObjects.Length == 0)
            {
                return;
            }

            List<Object> selection = new List<Object>();

            foreach (var obj in Selection.gameObjects)
            {
                var cmps = obj.GetComponentsInChildren<T>();
                foreach (var c in cmps)
                {
                    if (selection.Contains(c.gameObject) == false)
                    {
                        selection.Add(c.gameObject);
                    }
                }
            }

            Selection.objects = selection.ToArray();
        }

        void replaceShaderUnderSelection()
        {
            var target = Selection.activeGameObject;

            var renderers = target.GetComponentsInChildren<Renderer>();

            foreach (var r in renderers)
            {
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null)
                    {
                        continue;
                    }
                    m.shader = shader;
                }
            }
        }

        void replaceMaterialsInScene()
        {
            var renderers = FindObjectsOfType<Renderer>();

            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                bool change = false;

                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == material1)
                    {
                        mats[i] = material2;
                        change = true;
                    }
                }

                if (change)
                {
                    r.sharedMaterials = mats;
                }
            }
        }

        [MenuItem("Arena/Утилиты/Удалить пустые материалы из рендереров")]
        static void deleteMissingMats()
        {
            var renderers = FindObjectsOfType<Renderer>();
            var temp = new List<Material>();

            foreach (var r in renderers)
            {
                temp.Clear();
                temp.AddRange(r.sharedMaterials);
                bool changed = false;

                for (int i = temp.Count - 1; i >= 0; i--)
                {
                    var m = temp[i];

                    if (m == null)
                    {
                        Debug.LogFormat("removing missing mat for {0}", r.name);
                        temp.RemoveAt(i);
                        changed = true;
                    }
                }

                if (changed)
                {
                    r.sharedMaterials = temp.ToArray();
                }
            }
        }

        [MenuItem("Arena/Утилиты/Показать рендереры без материалов")]
        static void showEmptyRenderers()
        {
            var renderers = FindObjectsOfType<Renderer>();

            foreach (var r in renderers)
            {
                if(r.sharedMaterials.Length == 0)
                {
                    Debug.Log(r.name, r.gameObject);
                    continue;
                }

                foreach(var m in r.sharedMaterials)
                {
                    if(m == null)
                    {
                        Debug.Log($"null material in {r.name}", r.gameObject);
                    }
                }
            }
        }

        [MenuItem("Arena/Утилиты/Удалить меш фильтры без рендереров")]
        static void deleteEmptyFilters()
        {
            var filters = FindObjectsOfType<MeshFilter>();

            foreach (var r in filters)
            {
                if(r.GetComponent<Renderer>() == null)
                {
                    Debug.Log("removing mesh filter from " + r.name);
                    DestroyImmediate(r);
                }
            }
        }

        static void dialog(string message)
        {
            EditorUtility.DisplayDialog("Сообщение", message, "OK");
        }

        [MenuItem("Arena/Утилиты/Настройка префаба модели ArmorSet")]
        static void setupArmorSetPrefab()
        {
            var sb = new StringBuilder();
            var obj = Selection.activeGameObject;

            try
            {
                var retargetCmp = obj.GetComponent<RetargetComponent>();

                if (retargetCmp == null)
                {
                    sb.AppendLine($"нет {nameof(RetargetComponent)}");
                    return;
                }

                if (retargetCmp.RetargetAvatar == null)
                {
                    sb.AppendLine($"не назначен {nameof(RetargetComponent.RetargetAvatar)}");
                    return;
                }

                var avatar = retargetCmp.RetargetAvatar;

                setupArmorSetComponent(sb, obj, avatar, retargetCmp);
                setupRagdoll(sb, obj, avatar, retargetCmp);
            }
            catch (System.Exception ex)
            {
                dialog(ex.Message);
            }
            finally
            {
                dialog(sb.ToString());
            }
        }

        public static Transform findBone(Avatar avatar, Transform root, string boneName)
        {
            return HumanRigTools.FindBoneByAvatar(avatar, root, boneName);
        }

        static void setupRagdoll(StringBuilder sb, GameObject obj, Avatar avatar,
            RetargetComponent retargetCmp)
        {
            var ragdoll = obj.GetComponent<RagdollComponent>();
            
            if(ragdoll.Pelvis == false) ragdoll.Pelvis = findBone(avatar, retargetCmp.RetargetRootTransform, "Hips");
            if(ragdoll.MiddleSpine == false) ragdoll.MiddleSpine = findBone(avatar, retargetCmp.RetargetRootTransform, "Chest");
            if(ragdoll.Head == false) ragdoll.Head = findBone(avatar, retargetCmp.RetargetRootTransform, "Head");
            if(ragdoll.LeftHips == false) ragdoll.LeftHips = findBone(avatar, retargetCmp.RetargetRootTransform, "LeftUpperLeg");
            if(ragdoll.LeftKnee == false) ragdoll.LeftKnee = findBone(avatar, retargetCmp.RetargetRootTransform, "LeftLowerLeg");
            if(ragdoll.RightHips == false) ragdoll.RightHips = findBone(avatar, retargetCmp.RetargetRootTransform, "RightUpperLeg");
            if(ragdoll.RightKnee == false) ragdoll.RightKnee = findBone(avatar, retargetCmp.RetargetRootTransform, "RightLowerLeg");
            if(ragdoll.LeftUpperArm == false) ragdoll.LeftUpperArm = findBone(avatar, retargetCmp.RetargetRootTransform, "LeftUpperArm");
            if(ragdoll.LeftForeArm == false) ragdoll.LeftForeArm = findBone(avatar, retargetCmp.RetargetRootTransform, "LeftLowerArm");
            if(ragdoll.RightUpperArm == false) ragdoll.RightUpperArm = findBone(avatar, retargetCmp.RetargetRootTransform, "RightUpperArm");
            if(ragdoll.RightForeArm == false) ragdoll.RightForeArm = findBone(avatar, retargetCmp.RetargetRootTransform, "RightLowerArm");
        }

        static void setupArmorSetComponent(StringBuilder sb, GameObject obj, Avatar avatar, RetargetComponent retargetCmp)
        {
            var armorSetCmp = obj.GetComponent<ArmorSetAppearanceComponent>();

            if (armorSetCmp == null)
            {
                Debug.LogWarning($"не найден компонент {nameof(ArmorSetAppearanceComponent)}, настраиваю без него");
                return;
            }

                Func<string, Transform> setupArmorSetBone = (string boneName) =>
                {
                    var bone = findBone(avatar, retargetCmp.RetargetRootTransform, boneName);
                    if (bone)
                    {
                        return bone;
                    }
                    sb.AppendLine($"{boneName} не найден");
                    return null;
                };

                if (armorSetCmp.LeftFoot == null)
                {
                    armorSetCmp.LeftFoot = setupArmorSetBone("LeftFoot");
                }
                if (armorSetCmp.RightFoot == null)
                {
                    armorSetCmp.RightFoot = setupArmorSetBone("RightFoot");
                }
                
                Func<string, string, Transform> setupArmorSetSocket = (boneName, socketName) =>
                {
                    var bone = HumanRigTools.FindBoneByAvatar(avatar, retargetCmp.RetargetRootTransform, boneName);
                    
                    if (bone)
                    {
                        var socket = new GameObject($"{boneName} {socketName}");
                        socket.transform.SetParent(bone);
                        socket.transform.localPosition = Vector3.zero;
                        socket.transform.localRotation = quaternion.identity;
                        socket.transform.localScale = Vector3.one;
                        return socket.transform;
                    }
                    sb.AppendLine($"Кость {boneName} не найдена");
                    return null;
                };

                if (armorSetCmp.RightHandWeaponSocket == false)
                {
                    armorSetCmp.RightHandWeaponSocket = setupArmorSetSocket("RightHand", "weapon socket");
                }

                if (armorSetCmp.LeftHandBowSocket == false)
                {
                    armorSetCmp.LeftHandBowSocket = setupArmorSetSocket("LeftHand", "bow socket");
                }

                sb.AppendLine("Настройка завершена");
        }
        
        [MenuItem("Arena/Утилиты/Обработать нормали травы в OBJ файле")]
        static void processGrassNormals()
        {
            var selected = Selection.activeObject;

            if (selected == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Сначала выберите файл", "OK");
                return;
            }

            var path = AssetDatabase.GetAssetPath(selected);

            var lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];

                if (line.StartsWith("vn") == false)
                {
                    continue;
                }

                lines[index] = "vn 0 1 0";
            }

            File.WriteAllLines(path, lines);
            AssetDatabase.ImportAsset(path);
        }
    }
}

