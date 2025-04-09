using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;
using System.Text;

namespace Spelunx.Orbbec {
    public class BodyTrackerAvatar : MonoBehaviour {
        [Header("References")]
        [SerializeField] private BodyTracker bodyTracker;
        [SerializeField] private Animator avatarAnimator;
        [SerializeField] private Transform skeletonRoot;
        [SerializeField] private Transform avatarRoot;

        [Header("Settings")]
        [SerializeField] private Vector3 avatarOffset = Vector3.zero;

        // Internal variables.
        private Dictionary<JointId, Quaternion> absoluteOffsetMap;

        // Map Microsoft Kinect's joints to Unity's joints.
        private static HumanBodyBones MapKinectJoint(JointId joint) {
            // https://docs.microsoft.com/en-us/azure/Kinect-dk/body-joints
            switch (joint) {
                case JointId.Pelvis: return HumanBodyBones.Hips;
                case JointId.SpineNavel: return HumanBodyBones.Spine;
                case JointId.SpineChest: return HumanBodyBones.Chest;
                case JointId.Neck: return HumanBodyBones.Neck;
                case JointId.Head: return HumanBodyBones.Head;
                case JointId.HipLeft: return HumanBodyBones.LeftUpperLeg;
                case JointId.KneeLeft: return HumanBodyBones.LeftLowerLeg;
                case JointId.AnkleLeft: return HumanBodyBones.LeftFoot;
                case JointId.FootLeft: return HumanBodyBones.LeftToes;
                case JointId.HipRight: return HumanBodyBones.RightUpperLeg;
                case JointId.KneeRight: return HumanBodyBones.RightLowerLeg;
                case JointId.AnkleRight: return HumanBodyBones.RightFoot;
                case JointId.FootRight: return HumanBodyBones.RightToes;
                case JointId.ClavicleLeft: return HumanBodyBones.LeftShoulder;
                case JointId.ShoulderLeft: return HumanBodyBones.LeftUpperArm;
                case JointId.ElbowLeft: return HumanBodyBones.LeftLowerArm;
                case JointId.WristLeft: return HumanBodyBones.LeftHand;
                case JointId.ClavicleRight: return HumanBodyBones.RightShoulder;
                case JointId.ShoulderRight: return HumanBodyBones.RightUpperArm;
                case JointId.ElbowRight: return HumanBodyBones.RightLowerArm;
                case JointId.WristRight: return HumanBodyBones.RightHand;
                default: return HumanBodyBones.LastBone;
            }
        }

        // WARNING: DO NOT RENAME YOUR MODEL'S GAMEOBJECT IN UNITY!
        // What this function does is that it gets the skeleton names from the FBX file, and tries to map it to a GameObject in Unity.
        // If the GameObject in Unity does not match the name in the FBX file, it breaks!
        private static SkeletonBone GetSkeletonBone(Animator animator, string boneName) {
            int count = 0;
            string cloneName = boneName + "(Clone)";
            foreach (SkeletonBone sb in animator.avatar.humanDescription.skeleton) {
                if (sb.name == boneName || sb.name == cloneName) {
                    return animator.avatar.humanDescription.skeleton[count];
                }
                count++;
            }
            return new SkeletonBone();
        }

        private void Start() {
            Transform rootJointTransform = avatarRoot;
            absoluteOffsetMap = new Dictionary<JointId, Quaternion>();
            for (int i = 0; i < (int)JointId.Count; i++) {
                HumanBodyBones hbb = MapKinectJoint((JointId)i);
                if (hbb != HumanBodyBones.LastBone) {
                    Transform boneTransform = avatarAnimator.GetBoneTransform(hbb);
                    Quaternion absOffset = GetSkeletonBone(avatarAnimator, boneTransform.name).rotation;
                    // find the absolute offset for the tpose
                    while (!ReferenceEquals(boneTransform, rootJointTransform)) {
                        boneTransform = boneTransform.parent;
                        absOffset = GetSkeletonBone(avatarAnimator, boneTransform.name).rotation * absOffset;
                    }
                    absoluteOffsetMap[(JointId)i] = absOffset;
                }
            }
        }

        private void LateUpdate() {
            for (int j = 0; j < (int)JointId.Count; j++) {
                if (MapKinectJoint((JointId)j) != HumanBodyBones.LastBone && absoluteOffsetMap.ContainsKey((JointId)j)) {
                    // get the absolute offset
                    Quaternion absOffset = absoluteOffsetMap[(JointId)j];
                    Transform finalJoint = avatarAnimator.GetBoneTransform(MapKinectJoint((JointId)j));
                    finalJoint.rotation = absOffset * Quaternion.Inverse(absOffset) * bodyTracker.absoluteJointRotations[j] * absOffset;
                    if (j == 0) {
                        // character root plus translation reading from the kinect, plus the offset from the script public variables
                        finalJoint.position = avatarRoot.position + new Vector3(skeletonRoot.localPosition.x, skeletonRoot.localPosition.y, skeletonRoot.localPosition.z) + avatarOffset;
                    }
                }
            }
        }

    }
}