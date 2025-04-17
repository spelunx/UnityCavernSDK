using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Collections.Generic;

namespace Spelunx.Orbbec {
    // ********************************* IMPORTANT: There should only be one instance of BodyTrackerManager in the scene. ********************************* //
    // If anyone ever wants to add support for more sensors at a time, put the work in to modify this class instead of just adding more BodyTrackerManager.
    // That is because this class opens and closes devices, and I am not sure if we should allow more than one instance to possibly do that simultaneously and cause multi-threading problems.
    public class BodyTrackerManager : MonoBehaviour {
        [Header("References")]
        [SerializeField, Tooltip("The skeleton to control.")] private BodyTracker bodyTracker; // One for each skeleton on the screen. For now we only support 1.

        [Header("Settings")]
        [SerializeField, Tooltip("Serial number of the device we want to connect to. Can be changed on the fly, but you may need to wait for a few seconds.")] private string deviceSerial = "<Insert device serial number here.>";
        [SerializeField, Tooltip("How is the sensor mounted? This needs to be set before entering play mode, and cannot be changed on the fly.")] private SensorOrientation sensorOrientation;
        [SerializeField, Tooltip("List of found serials. You can copy the values onto your clipboard.")] private List<string> foundSerials = new List<string>();

        // Internal Variables
        private FrameData frameData = new FrameData();
        private SkeletalFrameDataProvider skeletalFrameDataProvider = null; // One for each Femto Bolt. One Femto Bolt can support multiple (like 20?) skeletons.
        private bool isReady = true; // A flag to ensure that a new frame data provider waits for the old one to shutdown completely, so that it is impossible for them to open the same device.

        public void SetDeviceSerial(string deviceSerial) { this.deviceSerial = deviceSerial; }
        public string GetDeviceSerial() { return deviceSerial; }
        public void SetSensorOrientation(SensorOrientation sensorOrientation) { this.sensorOrientation = sensorOrientation; }
        public SensorOrientation GetSensorOrientation() { return this.sensorOrientation; }

        private void Awake() {
        }

        private void Start() {
            ScanDeviceSerials();
        }

        private void OnDestroy() {
            if (skeletalFrameDataProvider != null) {
                skeletalFrameDataProvider.Dispose();
                skeletalFrameDataProvider = null;
            }
        }

        private void Update() {
            // Disconnect the currently connected device if the serial number no longer matches what we want.
            if (skeletalFrameDataProvider != null && skeletalFrameDataProvider.GetDeviceSerial() != deviceSerial) {
                Debug.Log("Switching shit out." + skeletalFrameDataProvider.GetDeviceSerial() + " is different from " + deviceSerial);
                skeletalFrameDataProvider.Dispose();
                skeletalFrameDataProvider = null;
            }

            // Connect to the new device.
            if (isReady && null == skeletalFrameDataProvider) {
                for (int i = 0; i < foundSerials.Count; ++i) {
                    if (foundSerials[i] == deviceSerial) {
                        skeletalFrameDataProvider = new SkeletalFrameDataProvider(deviceSerial, sensorOrientation, i, OnFrameDataProviderFinish);
                        isReady = false;
                    }
                }
            }

            // Update the skeleton.
            if (null == skeletalFrameDataProvider) { return; }
            if (!skeletalFrameDataProvider.HasStarted) { return; }
            if (!skeletalFrameDataProvider.ExtractData(ref frameData)) { return; }
            if (frameData.NumOfBodies == 0) { return; }
            bodyTracker.UpdateSkeleton(frameData, sensorOrientation);
        }

        /// Scan through the ORBBEC devices and retrieve their serial numbers.
        private void ScanDeviceSerials() {
            int deviceCount = Device.GetInstalledCount();
            foundSerials.Clear();
            for (int i = 0; i < deviceCount; ++i) {
                try {
                    using (Device device = Device.Open(i)) {
                        foundSerials.Add(device.SerialNum);
                        Debug.Log("BodyTrackerManager::ScanDeviceSerials - Found device with serial number " + device.SerialNum + ".");
                        device.Dispose();
                    }
                } catch (Exception e) {
                    Debug.LogError(e.ToString());
                }
            }
        }

        private void OnFrameDataProviderFinish() {
            isReady = true;
        }
    }
}