#if UNITY_EDITOR
using System.Collections.Generic;
using MagicaCloth2;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Arena.Client.Baking
{
    /// <summary>
    /// ТРЕБУЕТ НАЛИЧИЯ InternalsVisibleSourceGenerator.dll !!!
    /// </summary>
    public static class SetupHybridComponentsForBaking
    {
        [InitializeOnLoadMethod]    
        static void init()
        {
            var types = Unity.Entities.Conversion.CompanionComponentSupportedTypes.Types;

            var list = new List<ComponentType>(types);
            
            list.Add(new ComponentType(typeof(SkinnedMeshRenderer)));
            list.Add(new ComponentType(typeof(MagicaCapsuleCollider)));
            
            Unity.Entities.Conversion.CompanionComponentSupportedTypes.Types = list.ToArray();
        }
    } 
}
#endif