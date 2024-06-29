#if UNITY_EDITOR

using UnityEngine;
using Unity.Mathematics;
using System.Collections.Generic;
using TzarGames.AnimationFramework;

namespace Arena.Client.Anima
{
    public class RetargetComponent : MonoBehaviour, IRemapper
    {
        public GameObject SourceRigPrefab;
        public Avatar SourceAvatar;
        public Transform RetargetRootTransform;
        public Avatar RetargetAvatar;

        private RemapData cachedRemapData;

        public Transform GetSourceObjectRoot()
        {
            return SourceRigPrefab.transform;
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
        
        RemapData CreateRemapData(Transform srcRig, Transform dstRig, Avatar sourceAvatar, Avatar retargetAvatar)
        {
            List<TranslationOffsetRemapInfo> translationOffsets = new();
            List<RotationOffsetRemapInfo> rotationOffsets = new();

            quaternion srcRootRotInv = math.inverse(srcRig.rotation);
            quaternion destRootRotInv = math.inverse(dstRig.rotation);

            var targetBones = retargetAvatar.humanDescription.human;
            
            for (var boneIter = 0; boneIter < targetBones.Length; boneIter++)
            {
                var targetBone = targetBones[boneIter];
                var targetBoneName = targetBone.boneName;
                var targetHumanName = targetBone.humanName;
                string sourceBoneName = null;

                foreach (var sourceHumanBone in sourceAvatar.humanDescription.human)
                {
                    if (sourceHumanBone.humanName == targetHumanName)
                    {
                        sourceBoneName = sourceHumanBone.boneName;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(sourceBoneName))
                {
                    continue;
                }

                var sourceBoneTransform = FindChild(srcRig, sourceBoneName);

                if (sourceBoneTransform == false)
                {
                    continue;
                }
                
                var targetBoneTransform = FindChild(dstRig, targetBoneName);

                if (targetBoneTransform == false)
                {
                    continue;
                }

                if (targetHumanName == "Hips")
                {
                    // heuristic that computes retarget scale based on translation node (ex: hips) height (assumed to be y)
                    var translationOffsetScale = targetBoneTransform.position.y / sourceBoneTransform.position.y;

                    quaternion dstParentRot = math.mul(destRootRotInv, targetBoneTransform.parent.rotation);
                    quaternion srcParentRot = math.mul(srcRootRotInv, sourceBoneTransform.parent.rotation);

                    var translationOffsetRotation = math.mul(math.inverse(dstParentRot), srcParentRot);

                    translationOffsets.Add(new TranslationOffsetRemapInfo
                    {
                        SourceRigBone = sourceBoneTransform,
                        RemappedBone = targetBoneTransform,
                        Rotation = translationOffsetRotation,
                        Scale = translationOffsetScale 
                    });
                }

                //if (splitMap[2] == "TR" || splitMap[2] == "R")
                {
                    quaternion dstParentRot = math.mul(destRootRotInv, targetBoneTransform.parent.rotation);
                    quaternion srcParentRot = math.mul(srcRootRotInv, sourceBoneTransform.parent.rotation);

                    quaternion dstRot = math.mul(destRootRotInv, targetBoneTransform.rotation);
                    quaternion srcRot = math.mul(srcRootRotInv, sourceBoneTransform.rotation);

                    var rotationOffsetPreRotation = math.mul(math.inverse(dstParentRot), srcParentRot);
                    var rotationOffsetPostRotation = math.mul(math.inverse(srcRot), dstRot);

                    rotationOffsets.Add(new RotationOffsetRemapInfo
                    {
                        SourceRigBone = sourceBoneTransform,
                        RemappedBone = targetBoneTransform,
                        PreRotation = rotationOffsetPreRotation,
                        PostRotation = rotationOffsetPostRotation
                    });
                }
            }
            
            return new RemapData
            {
                RotationOffsets = rotationOffsets.ToArray(),
                TranslationOffsets = translationOffsets.ToArray()
            };
        }
        public RemapData GetRemapData()
        {
            if (cachedRemapData == null)
            {
                var dstRig = RetargetRootTransform ? RetargetRootTransform : transform;
                cachedRemapData = CreateRemapData(SourceRigPrefab.transform, dstRig, SourceAvatar, RetargetAvatar);
            }
            return cachedRemapData;
        }
    }
}
#endif
