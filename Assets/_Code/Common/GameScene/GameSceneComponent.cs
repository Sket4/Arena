using System;
using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.GameSceneCode
{
    [Serializable]
    public struct GameSceneDescription : IComponentData
    {
        public Entity MainSubScene;

        [SerializeField] private byte flags;

        public bool ShouldWaitForLoadOnClient
        {
            get => (flags &= 1 << 0) != 0;
            set => flags |= 1 << 0;
        }
    }

    public struct AutoLoadSceneSection : IBufferElementData
    {
        public int SectionIndex;
    }

    public enum SceneLoadingStates : byte
    {
        None,
        PendingStartLoading,
        PendingStartUnloading,
        Loading,
        Unloading,
        Unloaded,
        Running
    }

    public struct SceneLoadingState : IComponentData
    {
        public SceneLoadingStates Value;
    }
    
    // ссылка на игровую сессию (матч), которая иницировало загрузку игровой сцены
    public struct SessionEntityReference : IComponentData
    {
        public Entity Value;
    }
    
    [UseDefaultInspector]
    public class GameSceneComponent : ComponentDataBehaviour<GameSceneDescription>
    {
        public SceneKey MainSubScene;
        
        /// <summary>
        /// если true, то эта сцена на сервере не будет загружена до тех пор, пока загрузка этой сцены не завершится на всех клиентах
        /// </summary>
        public bool ShouldWaitForLoadOnClient = true;

        public int[] StartingSceneSections = new int[] { 0 };

        protected override void Bake<K>(ref GameSceneDescription serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if(MainSubScene != null)
            {
                var sceneEntity = baker.ConvertObjectKey(MainSubScene);
                serializedData.MainSubScene = sceneEntity;
            }

            serializedData.ShouldWaitForLoadOnClient = ShouldWaitForLoadOnClient;
            
            baker.AddComponent(new SessionEntityReference());
            baker.AddComponent(new SceneLoadingState());

            var sections = baker.AddBuffer<AutoLoadSceneSection>();
            foreach(var ss in StartingSceneSections)
            {
                sections.Add(new AutoLoadSceneSection
                {
                    SectionIndex = ss
                });
            }
        }
    }
}
