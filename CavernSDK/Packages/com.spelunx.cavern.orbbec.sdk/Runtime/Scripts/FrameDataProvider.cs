using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Azure.Kinect.Sensor;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Spelunx.Orbbec {
    /// Processes data from the ORBBEC sensor in a background thread to produce FrameData.
    public class FrameDataProvider : IDisposable {
        public delegate void FinishCallback();

        /// Flag to determine of the background thread has started.
        public bool HasStarted { get; protected set; } = false;
        public bool HasData { get; private set; } = false;
        public string DeviceSerial { get; private set; }
        public SensorOrientation Orientation { get; private set; }

        // Internal variables.
        private FrameData frontBuffer = new FrameData();
        private FrameData backBuffer = new FrameData();
        private object dataMutex = new object();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task backgroundThread = null;

        public FrameDataProvider(int deviceId, SensorOrientation orientation, FinishCallback onFinish) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting += OnEditorClose;
#endif

            Orientation = orientation;

            backgroundThread = Task.Run(() => RunBackgroundThreadAsync(deviceId, cancellationTokenSource.Token, onFinish));
        }

        public void Dispose() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting -= OnEditorClose;
#endif

            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;

            backgroundThread?.Wait();
        }

        public bool GetData(ref FrameData output) {
            lock (dataMutex) {
                if (!HasData) { return false; }

                var temp = frontBuffer;
                frontBuffer = output;
                output = temp;
                HasData = false;

                return true;
            }
        }

        protected void RunBackgroundThreadAsync(int deviceId, CancellationToken token, FinishCallback onFinish) {
            try {
                UnityEngine.Debug.Log("Starting body tracker background thread.");

                // Check if this device ID is valid.
                if (Device.GetInstalledCount() <= deviceId) {
                    throw new Exception("SkeletalFrameDataProvider - Cannot open device ID " + deviceId + ". Only " + Device.GetInstalledCount() + " devices are connected. Terminating thread.");
                }

                // Open device. The keyword "using" ensures that an IDisposable is properly disposed of even if an exception occurs within the block.
                using (Device device = Device.Open(deviceId)) { // TODO: Play around with ID
                    DeviceSerial = device.SerialNum; // Assign device serial.

                    // Start Sensor Cameras.
                    device.StartCameras(new DeviceConfiguration() {
                        CameraFPS = FPS.FPS30,
                        ColorResolution = ColorResolution.Off,
                        DepthMode = DepthMode.NFOV_Unbinned,
                        WiredSyncMode = WiredSyncMode.Standalone,
                        DisableStreamingIndicator = false // Ensure that the LED light on the sensor is on so that we can visually see what we are connected to.
                    });

                    UnityEngine.Debug.Log("SkeletalFrameDataProvider - Open K4A device successfully. Device ID: " + deviceId + ", Serial Number: " + device.SerialNum);

                    // Get tracker calibration and configuration.
                    var trackerCalibration = device.GetCalibration();
                    TrackerConfiguration trackerConfig = new TrackerConfiguration() {
                        ProcessingMode = TrackerProcessingMode.Cpu, // Use CPU so we don't have to download the CUDA binaries. This also means that we don't need a NVIDIA GPU.
                        /* SensorOrientation doesn't orientate the skeleton the right way up.
                         * That is implemented seperately in BodyTracker.
                         * This helps the sensor to determine it's orientation so that the data produced is smoother.
                         * For example if the sensor was set to Default and flipped 180 degrees, then the data will be jitery.
                         * Once the orientation is correct, the data is much smoother. */
                        // https://github.com/microsoft/Azure-Kinect-Sensor-SDK/issues/1039
                        SensorOrientation = Orientation
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
                                    UnityEngine.Debug.Log($"SkeletalFrameDataProvider - ID: {deviceId}, Pop result from tracker timeout!");
                                    continue;
                                }

                                // Flag that the thread has started.
                                HasStarted = true;

                                // Copy bodies.
                                backBuffer.NumDetectedBodies = frame.NumberOfBodies;
                                for (uint i = 0; i < backBuffer.NumDetectedBodies; i++) {
                                    backBuffer.Bodies[i].CopyFromBodyTrackingSdk(frame.GetBody(i), trackerCalibration);
                                }

                                // Store depth image.
                                Capture bodyFrameCapture = frame.Capture;
                                Image depthImage = bodyFrameCapture.Depth;
                                if (isFirstFrame) {
                                    isFirstFrame = false;
                                    initialTimestamp = depthImage.DeviceTimestamp;
                                }
                                backBuffer.TimestampInMs = (float)(depthImage.DeviceTimestamp - initialTimestamp).TotalMilliseconds;
                                backBuffer.DepthImageWidth = depthImage.WidthPixels;
                                backBuffer.DepthImageHeight = depthImage.HeightPixels;

                                // Read image data from the SDK.
                                var depthFrame = MemoryMarshal.Cast<byte, ushort>(depthImage.Memory.Span);

                                // Repack data and store image data.
                                const float MAX_DISPLAYED_DEPTH_IN_MILLIMETERS = 5000.0f;
                                int byteCounter = 0;
                                backBuffer.DepthImageSize = backBuffer.DepthImageWidth * backBuffer.DepthImageHeight * 3;
                                for (int it = backBuffer.DepthImageWidth * backBuffer.DepthImageHeight - 1; it > 0; it--) {
                                    byte b = (byte)(depthFrame[it] / MAX_DISPLAYED_DEPTH_IN_MILLIMETERS * 255);
                                    backBuffer.DepthImage[byteCounter++] = b;
                                    backBuffer.DepthImage[byteCounter++] = b;
                                    backBuffer.DepthImage[byteCounter++] = b;
                                }

                                // Update data variable that is being read in the UI thread.
                                SwapBuffers();
                            }
                        }
                        tracker.Dispose();
                    }
                    device.Dispose();
                }
            } catch (Exception e) {
                UnityEngine.Debug.Log($"SkeletalFrameDataProvider - ID: {deviceId}, Catching exception for background thread: {e.Message}");
            } finally {
                UnityEngine.Debug.Log($"SkeletalFrameDataProvider - ID: {deviceId}, Shutting down background thread.");
                onFinish?.Invoke();
            }
        }

        private void OnEditorClose() { Dispose(); }

        private void SwapBuffers() {
            lock (dataMutex) {
                var temp = backBuffer;
                backBuffer = frontBuffer;
                frontBuffer = temp;
                HasData = true;
            }
        }
    }
}