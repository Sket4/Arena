using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [System.Serializable]
    public struct AutoAim : IComponentData
    {
        public float Angle;
    }
    public class AutoAimComponent : ComponentDataBehaviour<AutoAim>
    {
        private void OnDrawGizmosSelected()
        {
            float dist = 50;
            var forward = transform.forward * dist;
            var pos = transform.position;
            var p1 = pos + Quaternion.AngleAxis(Value.Angle * 0.5f, Vector3.up) * forward;
            var p2 = pos + Quaternion.AngleAxis(Value.Angle * -0.5f, Vector3.up) * forward;
            Gizmos.DrawLine(pos, p1);
            Gizmos.DrawLine(pos, p2);
            Gizmos.DrawLine(p1, p2);
        }
    }
}
