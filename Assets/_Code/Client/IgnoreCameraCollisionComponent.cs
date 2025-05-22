using System.Collections;
using System.Collections.Generic;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    public struct IgnoreCameraCollision : IComponentData
    {
    }

    public class IgnoreCameraCollisionComponent : ComponentDataBehaviour<IgnoreCameraCollision>
    {
    }
}
