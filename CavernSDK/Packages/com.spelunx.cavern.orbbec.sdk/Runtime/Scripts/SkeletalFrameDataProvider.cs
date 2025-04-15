using Microsoft.Azure.Kinect.BodyTracking;
using Microsoft.Azure.Kinect.Sensor;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace Spelunx.Orbbec {
    public class SkeletalFrameDataProvider : FrameDataProvider {
        // Internal variables.
        private SensorOrientation sensorOrientation;

        // For logging. Currently not enabled.
        private System.Runtime.Serialization.Formatters.Binary.BinaryFormatter binaryFormatter { get; set; } = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        private Stream rawDataLoggingFile = null;

        public SkeletalFrameDataProvider(int id, SensorOrientation sensorOrientation) : base(id) {
            this.sensorOrientation = sensorOrientation;
        }

        protected override void RunBackgroundThreadAsync(int id, CancellationToken token) {
            try {
                UnityEngine.Debug.Log("Starting body tracker background thread.");

                // Allocate data buffer.
                FrameData currentFrameData = new FrameData();
                
                // Check if this device ID is valid.
                if (Device.GetInstalledCount() <= id) {
                    UnityEngine.Debug.Log("SkeletalFrameDataProvider - Cannot open device ID " + id + ". Only " + Device.GetInstalledCount() + " devices are connected. Terminating thread.");
                    return;
                }

                // Open device. The keyword "using" ensures that an IDisposable is properly disposed of even if an exception occurs within the block.
                using (Device device = Device.Open(id)) { // TODO: Play around with ID
                    // Start Sensor Cameras.
                    device.StartCameras(new DeviceConfiguration() {
                        CameraFPS = FPS.FPS30,
                        ColorResolution = ColorResolution.Off,
                        DepthMode = DepthMode.NFOV_Unbinned,
                        WiredSyncMode = WiredSyncMode.Standalone,
                    });

                    UnityEngine.Debug.Log("SkeletalFrameDataProvider - Open K4A device successfully. Device ID: " + id + ", Serial Number: " + device.SerialNum);

                    // Get tracker calibration and configuration.
                    var trackerCalibration = device.GetCalibration();
                    TrackerConfiguration trackerConfig = new TrackerConfiguration() {
                        ProcessingMode = TrackerProcessingMode.Cpu, // Use CPU so we don't have to download the CUDA binaries. This also means that we don't need a NVIDIA GPU.
                        // This is where we can rotate the sensor orientation.
                        // This doesn't actually orientate the skeleton the right way up. That still needs to be implemented.
                        // But for example if the sensor was set to Default and flipped 180 degrees, then the recording will be jiterry.
                        // Once the orientation is correct, the sensor is much smoother.
                        SensorOrientation = sensorOrientation
                    };

                    bool isFirstFrame = true;
                    TimeSpan initialTimestamp = new TimeSpan(0);
                    using (Tracker tracker = Tracker.Create(trackerCalibration, trackerConfig)) {
                        while (!token.IsCancellationRequested) { // Run until the thread is closed.
                            // Queue latest capture from the device, so that the tracker can process the information.
                            using (Capture sensorCapture = device.GetCapture()) {
                                tracker.EnqueueCapture(sensorCapture);
                            }

                            // Now that the tracker has processed the information, try popping the result.
                            using (Frame frame = tracker.PopResult(TimeSpan.Zero, throwOnTimeout: false)) {
                                if (frame == null) {
                                    UnityEngine.Debug.Log($"SkeletalFrameDataProvider - ID: {id}, Pop result from tracker timeout!");
                                    continue;
                                }

                                // Flag that the thread has started.
                                HasStarted = true;

                                // Copy bodies.
                                currentFrameData.NumOfBodies = frame.NumberOfBodies;
                                for (uint i = 0; i < currentFrameData.NumOfBodies; i++) {
                                    currentFrameData.Bodies[i].CopyFromBodyTrackingSdk(frame.GetBody(i), trackerCalibration);
                                }

                                // Store depth image.
                                Capture bodyFrameCapture = frame.Capture;
                                Image depthImage = bodyFrameCapture.Depth;
                                if (isFirstFrame) {
                                    isFirstFrame = false;
                                    initialTimestamp = depthImage.DeviceTimestamp;
                                }
                                currentFrameData.TimestampInMs = (float)(depthImage.DeviceTimestamp - initialTimestamp).TotalMilliseconds;
                                currentFrameData.DepthImageWidth = depthImage.WidthPixels;
                                currentFrameData.DepthImageHeight = depthImage.HeightPixels;

                                // Read image data from the SDK.
                                var depthFrame = MemoryMarshal.Cast<byte, ushort>(depthImage.Memory.Span);

                                // Repack data and store image data.
                                int byteCounter = 0;
                                currentFrameData.DepthImageSize = currentFrameData.DepthImageWidth * currentFrameData.DepthImageHeight * 3;
                                for (int it = currentFrameData.DepthImageWidth * currentFrameData.DepthImageHeight - 1; it > 0; it--) {
                                    var y = (ConfigLoader.Instance.Configs.SkeletalTracking.MaximumDisplayedDepthInMillimeters);
                                    byte b = (byte)(depthFrame[it] / (ConfigLoader.Instance.Configs.SkeletalTracking.MaximumDisplayedDepthInMillimeters) * 255);
                                    currentFrameData.DepthImage[byteCounter++] = b;
                                    currentFrameData.DepthImage[byteCounter++] = b;
                                    currentFrameData.DepthImage[byteCounter++] = b;
                                }

                                // Log to file.
                                if (rawDataLoggingFile != null && rawDataLoggingFile.CanWrite) {
                                    binaryFormatter.Serialize(rawDataLoggingFile, currentFrameData);
                                }

                                // Update data variable that is being read in the UI thread.
                                SetData(ref currentFrameData);
                            }
                        }
                        tracker.Dispose();
                    }
                    device.Dispose();
                }

                // Close log file.
                if (rawDataLoggingFile != null) { rawDataLoggingFile.Close(); }
            } catch (Exception e) {
                Debug.Log($"SkeletalFrameDataProvider - ID: {id}, Catching exception for background thread: {e.Message}");
                token.ThrowIfCancellationRequested();
            }
        }
    }
}