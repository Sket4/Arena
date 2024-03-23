using UnityEngine;

namespace Arena.Tools
{
    [System.Serializable]
    public class PlacerSettings
    {
        [Header("Path settings")]
        public GameObject PathObject;
        public Vector3 PathObjectScale = Vector3.one;
        public bool Reverse = false;
        
        [Header("Instance placement settings")]
        public GameObject TargetPrefab;
        public GameObject TargetParent;
        public float FixedInterval = 1;
        public bool RandomYaw = false;
        public bool ChangeScale = false;
        public Vector3 MinScale = Vector3.one;
        public Vector3 MaxScale = Vector3.one;
        public float Offset = 0;
        public Vector3 WorldSpaceOffset;
        public Vector3 AdditionalRotation;
        public float NormalOffset = 0;
        public int MaximumObjects = 0;
    }

    public class ObjectPlacerSettings : MonoBehaviour
    {
        public PlacerSettings[] Settings;
    }
}
