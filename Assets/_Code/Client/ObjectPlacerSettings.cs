using UnityEngine;

namespace Arena.Tools
{
    [System.Serializable]
    public class PlacerSettings
    {
        public GameObject TargetMeshObject;
        public MeshFilter TargetMesh
        {
            get
            {
                if(TargetMeshObject == null)
                {
                    return null;
                }
                return TargetMeshObject.GetComponentInChildren<MeshFilter>();
            }
        }
        public GameObject TargetPrefab;
        public GameObject TargetParent;
        public float FixedInterval = 1;
        public bool Reverse = false;
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
