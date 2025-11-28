using UnityEngine;
using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.Collections.Generic;

namespace Spelunx.Orbbec {
    // ********************************* IMPORTANT: There should only be one instance of BodyTrackerManager in the scene. ********************************* //
    // If anyone ever wants to add support for more sensors at a time, put the work in to modify this class instead of just adding more BodyTrackerManager.
    // That is because this class opens and closes devices, and I am not sure if we should allow more than one instance to possibly do that simultaneously and cause multi-threading problems.

    /// Manager class to pass data from FrameDataProvider to BodyTracker.
    public class BodyTrackerManager : MonoBehaviour {
        [Header("References")]
        [SerializeField, Tooltip("The skeleton to control.")] private BodyTracker bodyTracker; // One for each skeleton on the screen. For now we only support 1.

        [Header("Settings")]
        [SerializeField, Tooltip("How is the sensor mounted?")] private SensorOrientation sensorOrientation;
        [SerializeField, Tooltip("Serial number of the device we want to connect to.")] private string deviceSerial = "<Insert device serial number here.>";
        [SerializeField, Tooltip("If no serial numbers match, connect to first device found.")] private bool connectDefaultIfNoSerialMatch = true;

        // Internal Variables
        private List<string> availableSerials = new List<string>();
        private FrameData frameData = new FrameData();
        private FrameDataProvider frameDataProvider = null; // One for each Femto Bolt. One Femto Bolt can support multiple (like 20?) skeletons.
        private bool isReady = true; // A flag to ensure that a new frame data provider waits for the old one to shutdown completely, so that it is impossible for them to open the same device.
        private bool hasDetectedBodies = false;

        private static BodyTrackerManager instance;
        public void SetBodyTracker(BodyTracker bodyTracker) { this.bodyTracker = bodyTracker; }
        public BodyTracker GetBodyTracker() { return this.bodyTracker; }

        public void SetSensorOrientation(SensorOrientation sensorOrientation) { this.sensorOrientation = sensorOrientation; }
        public SensorOrientation GetSensorOrientation() { return this.sensorOrientation; }

        public void SetDeviceSerial(string deviceSerial) { this.deviceSerial = deviceSerial; }
        public string GetDeviceSerial() { return deviceSerial; }
        public List<string> GetAvailableSerials() { return availableSerials; }

        public bool HasDetectedBodies() { return hasDetectedBodies; }

        private void Awake() {
            // This ensures that only 1 BodyTrackerManager exists at a time.
            if (instance == null) {
                instance = this;
            } else {
                Destroy(this);
            }
        }

        private void OnDestroy() {
            if (frameDataProvider != null) {
                frameDataProvider.Dispose();
                frameDataProvider = null;
            }

            if (instance == this) {
                instance = null;
            }
        }

        private void Update() {
            // Disconnect the currently connected device if the serial number no longer matches what we want.
            if (frameDataProvider != null &&
                frameDataProvider.HasStarted &&
                (frameDataProvider.DeviceSerial != deviceSerial || frameDataProvider.Orientation != sensorOrientation)) {
                Debug.Log("New serial number " + deviceSerial + "selected. Shutting down " + frameDataProvider.DeviceSerial + ".");
                frameDataProvider.Dispose();
                frameDataProvider = null;
            }

            // Connect to the new device.
            if (isReady && null == frameDataProvider) {
                // Scan for devices.
                ScanDeviceSerials();

                // See if any of the devices match.
                for (int i = 0; i < availableSerials.Count; ++i) {
                    if (availableSerials[i] == deviceSerial) {
                        Debug.Log("Attempting to start " + deviceSerial + ".");
                        frameDataProvider = new FrameDataProvider(i, sensorOrientation, OnFrameDataProviderFinish);
                        isReady = false;
                    }
                }

                // If none match, start index 0 if connectDefaultIfNoSerialMatch is set to true.
                if (connectDefaultIfNoSerialMatch && 0 < availableSerials.Count && frameDataProvider == null) {
                    deviceSerial = availableSerials[0];
                }
            }

            // Check if the frame data provider has started.
            if (null == frameDataProvider || !frameDataProvider.HasStarted) {
                hasDetectedBodies = false;
                return;
            }

            // Update the skeleton if there is new data.
            if (frameDataProvider.GetData(ref frameData)) {
                if (0 < frameData.NumDetectedBodies) {
                    hasDetectedBodies = true;
                    bodyTracker.UpdateSkeleton(frameData, sensorOrientation);
                } else {
                    hasDetectedBodies = false;
                }
            }
        }

        /// Scan through the ORBBEC devices and retrieve their serial numbers.
        private void ScanDeviceSerials() {
            int deviceCount = Device.GetInstalledCount();
            availableSerials.Clear();
            for (int i = 0; i < deviceCount; ++i) {
                try {
                    using (Device device = Device.Open(i)) {
                        availableSerials.Add(device.SerialNum);
                        Debug.Log("BodyTrackerManager::ScanDeviceSerials - Found device with serial number " + device.SerialNum + ".");
                        device.Dispose();
                    }
                } catch (Exception e) {
                    Debug.LogError(e.ToString());
                }
            }
        }

        private void OnFrameDataProviderFinish() { isReady = true; }
    }
}