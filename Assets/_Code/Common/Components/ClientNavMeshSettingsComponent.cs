using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public sealed class ClientNavMeshSettings : IComponentData
    {
        public Material MapMaterial;
    }

    public class ClientNavMeshSettingsComponent : ComponentDataClassBehaviour<ClientNavMeshSettings>
    {
    }
}
