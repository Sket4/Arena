using System;
using TzarGames.GameCore;
using TzarGames.MultiplayerKit;
using Unity.Entities;
using UnityEngine;

namespace Arena
{
    [Serializable]
    [Sync]
    public struct SyncedColor : IComponentData
    {
        [HideInAuthoring]
        public PackedColor Value;

        public SyncedColor(Color color)
        {
            var c = (Color32)color;
            Value = new PackedColor(c.r, c.g, c.b, c.a);
        }

        public SyncedColor(int color)
        {
            Value = new PackedColor(color);
        }
    }
    
    public class SyncedColorComponent : ComponentDataBehaviour<SyncedColor>
    {
    }
}
