using TMPro;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace Arena.Client.Baking
{
    public struct TextMeshProData : IComponentData
    {
        // костыль для исправления проблемы с отсечением TextMeshPro при смене текста
        public Bounds Bounds;
    }
    
    public class TextMeshProBaker : Baker<TextMeshPro>
    {
        public override void Bake(TextMeshPro authoring)
        {
            AddComponentObject(authoring);
            var mr = GetComponent<MeshRenderer>();
            
            AddComponentObject(mr);
            var rt = GetComponent<RectTransform>();
            AddComponentObject(rt);
            
            var localize = GetComponent<LocalizeStringEvent>();
            if (localize)
            {
                AddComponentObject(localize);
            }

            var max = math.max(rt.sizeDelta.x, rt.sizeDelta.y);
            var bounds = new Bounds(default, new Vector3(max,max,max));
            var data = new TextMeshProData
            {
                Bounds = bounds
            };
            AddComponent(data);
        }
    }
}
