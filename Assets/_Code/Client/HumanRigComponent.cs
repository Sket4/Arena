using System;
using TzarGames.GameCore;
using Unity.Entities;
using UnityEngine;

namespace Arena.Client
{
    [Serializable]
    public struct HumanRig : IComponentData
    {
        public Entity Head;
        public Entity Neck;
        public Entity LeftShoulder;
        public Entity LeftUpperArm;
        public Entity RightShoulder;
        public Entity RightUpperArm;
        public Entity UpperChest;
    }
    
    [UseDefaultInspector]
    public class HumanRigComponent : ComponentDataBehaviour<HumanRig>
    {
        public Avatar Avatar;

        private IGCBaker baker;
        private Transform root;

        protected override void Bake<K>(ref HumanRig serializedData, K baker)
        {
            base.Bake(ref serializedData, baker);

            if (Avatar == false)
            {
                return;
            }
            
            //"Hips";
            //"Chest"
            //"LeftUpperLeg";
            //"LeftLowerLeg";
            //"RightUpperLeg";
            //"RightLowerLeg";
            //"LeftLowerArm";
            //"RightLowerArm";

            root = transform;
            this.baker = baker;

            setupBone(ref serializedData.Head, "Head");
            setupBone(ref serializedData.Neck, "Neck");
            setupBone(ref serializedData.LeftShoulder, "LeftShoulder");
            setupBone(ref serializedData.LeftUpperArm, "LeftUpperArm");
            setupBone(ref serializedData.RightShoulder, "RightShoulder");
            setupBone(ref serializedData.RightUpperArm, "RightUpperArm");
            setupBone(ref serializedData.UpperChest, "UpperChest");
        }

        void setupBone(ref Entity entity, string boneName)
        {
            var bone = HumanRigTools.FindBoneByAvatar(Avatar, root, boneName);
            if (bone == false)
            {
                entity = Entity.Null;
                return;
            }
            entity = baker.GetEntity(bone);
        }
    }

    public static class HumanRigTools
    {
        public static Transform FindBoneByAvatar(Avatar avatar, Transform root, string boneName)
        {
            var bones = avatar.humanDescription.human;

            foreach (var humanBone in bones)
            {
                if (humanBone.humanName == boneName)
                {
                    return FindChild(root, humanBone.boneName);
                }
            }
            return null;
        }
        
        public static Transform FindChild(Transform root, string childName)
        {
            foreach (Transform child in root)
            {
                if (child.name == childName)
                {
                    return child;
                }
                var otherChild = FindChild(child, childName);
                if (otherChild)
                {
                    return otherChild;
                }
            }
            return null;
        }
    }
}
