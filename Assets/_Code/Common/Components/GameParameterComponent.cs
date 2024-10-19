using System;
using TzarGames.GameCore;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    public struct GameParameter : IBufferElementData
    {
        public FixedString32Bytes Key;
        public FixedString32Bytes Value;
    }
    
    [UseDefaultInspector]
    public class GameParameterComponent : DynamicBufferBehaviour<GameParameter>
    {
        [Serializable]
        public class QuestParameterAuthoring
        {
            public string Key;
            public string Value;
        }
        
        [Header("! макс длина ключа и значения - 14 букв !")]
        public QuestParameterAuthoring[] QuestParameters;

        protected override void Bake<K>(ref DynamicBuffer<GameParameter> serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);
            
            if (QuestParameters == null || QuestParameters.Length == 0)
            {
                return;
            }
            
            foreach (var parameter in QuestParameters)
            {
                if (parameter.Key.Length == 0)
                {
                    Debug.LogError($"Пустой ключ, хранится в {name}");
                    continue;
                }
                if (parameter.Key.Length > 14)
                {
                    Debug.LogError($"Длина ключа {parameter.Key} слишком большая!, хранится в {name}");
                    continue;
                }
                if (parameter.Value.Length > 14)
                {
                    Debug.LogError($"Длина значения {parameter.Value} слишком большая!, ключ {parameter.Key} хранится в {name}");
                    continue;
                }
                    
                serializedData.Add(new GameParameter
                {
                    Key = parameter.Key,
                    Value = parameter.Value
                });
            }
        }
    }
}
