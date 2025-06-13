using System;
using System.Threading.Tasks;
using TzarGames.GameCore.Baking;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace TzarGames.GameCore.Items
{
    [Serializable]
    public struct DialogueIcon : IComponentData
    {
        public WeakObjectReference<Texture2D> Value;
        
        public static async Task<Texture2D> LoadIcon(WeakObjectReference<Texture2D> spriteRef)
        {
            if(spriteRef.LoadingStatus == ObjectLoadingStatus.None)
            {
                spriteRef.LoadAsync();
            }
            while(spriteRef.LoadingStatus != ObjectLoadingStatus.Completed && spriteRef.LoadingStatus != ObjectLoadingStatus.Error)
            {
                await Task.Yield();
            }
            return spriteRef.Result;
        }
    }

    [UseDefaultInspector]
    public class DialogueIconComponent : ComponentDataBehaviour<DialogueIcon>
    {
        [SerializeField]
        private Texture2D texture;

        public Texture2D Texture => texture;

        protected override void Bake<K>(ref DialogueIcon serializedData, K baker)
        {
            if(texture == false)
            {
                if (baker.UnityBaker.IsBakingForEditor() == false)
                {
                    Debug.LogError($"Null texture reference in component {nameof(DialogueIconComponent)} added to {gameObject.name}");
                }
                serializedData.Value = default;
                return;
            }
#if UNITY_EDITOR
            serializedData.Value = new WeakObjectReference<Texture2D>(texture);
#endif
        }

        protected override ConversionTargetOptions GetDefaultConversionOptions()
        {
            return LocalAndClient();
        }
    }
}
