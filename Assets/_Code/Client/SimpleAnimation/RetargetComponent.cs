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
        
        public static RemapData CreateRemapData(Transform srcRig, Transform dstRig, Avatar sourceAvatar, Avatar retargetAvatar, Transform fallbackRootTransform)
        {
#if UNITY_EDITOR
            // если объект находится на сцене, то он, скорее всего, не находится в начале координат,
            // поэтому смещаем его корень в начало координат, чтобы не ломался ретаргетинг
            Transform modifiedRootTransform = null;
            Transform prevParent = null;
            Vector3 prevRootPosition = default;
            Quaternion prevRootRotation = default;

            if (dstRig.gameObject.scene.IsValid())
            {
                if(UnityEditor.PrefabUtility.IsPartOfAnyPrefab(dstRig.gameObject))
                {
                    modifiedRootTransform = UnityEditor.PrefabUtility.GetOutermostPrefabInstanceRoot(dstRig.gameObject).transform;
                }
                else
                {
                    modifiedRootTransform = fallbackRootTransform;
                }

                prevParent = modifiedRootTransform.parent;
                modifiedRootTransform.SetParent(null);

                prevRootPosition = modifiedRootTransform.position;
                prevRootRotation = modifiedRootTransform.rotation;
                    
                modifiedRootTransform.position = Vector3.zero;
                modifiedRootTransform.rotation = quaternion.identity;
            }
#endif
            
            quaternion destRootRotInv = math.inverse(dstRig.rotation);
            quaternion srcRootRotInv = math.inverse(srcRig.rotation);
            
            List<TranslationOffsetRemapInfo> translationOffsets = new();
            List<RotationOffsetRemapInfo> rotationOffsets = new();

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

                var sourceBoneTransform = HumanRigTools.FindChild(srcRig, sourceBoneName);

                if (sourceBoneTransform == false)
                {
                    continue;
                }
                
                var targetBoneTransform = HumanRigTools.FindChild(dstRig, targetBoneName);

                if (targetBoneTransform == false)
                {
                    continue;
                }

                //if (targetHumanName == "Hips")
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
            
            #if UNITY_EDITOR
            if (modifiedRootTransform)
            {
                if (prevParent)
                {
                    modifiedRootTransform.SetParent(prevParent);    
                }
                modifiedRootTransform.position = prevRootPosition;
                modifiedRootTransform.rotation = prevRootRotation;
            }
            #endif
            
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
                cachedRemapData = CreateRemapData(SourceRigPrefab.transform, dstRig, SourceAvatar, RetargetAvatar, transform);
            }
            return cachedRemapData;
        }
    }
}
