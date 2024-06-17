using TMPro;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client.Baking
{
    public class TextMeshProBaker : Baker<TextMeshPro>
    {
        public override void Bake(TextMeshPro authoring)
        {
            AddComponentObject(authoring);
            var mr = GetComponent<MeshRenderer>();
            AddComponentObject(mr);
            var rt = GetComponent<RectTransform>();
            AddComponentObject(rt);
        }
    }
}
