using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace Spelunx.XR.Vive
{
    public class PoseTester : MonoBehaviour
    {
        public InputAction poseAction;
        public InputAction positionAction;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            poseAction.Enable();
            positionAction.Enable();
        }
        

        // Update is called once per frame
        void Update()
        {
            var poseState = poseAction.ReadValue<PoseState>();
            transform.SetPositionAndRotation(poseState.position, poseState.rotation);
            // var pos = positionAction.ReadValue<Vector3>();
            // transform.position = pos;
        }
    }
}
