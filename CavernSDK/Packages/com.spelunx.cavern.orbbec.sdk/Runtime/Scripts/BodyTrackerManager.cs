using Microsoft.Azure.Kinect.BodyTracking;
using UnityEngine;

namespace Spelunx.Orbbec {
    public class BodyTrackerManager : MonoBehaviour {
        // Handler for SkeletalTracking thread.
        public BodyTracker bodyTracker;

        [Header("Settings")]
        [SerializeField] private SensorOrientation sensorOrientation;

        // Internal Variables
        private SkeletalFrameDataProvider skeletalFrameDataProvider;
        private FrameData frameData = new FrameData();

        private void Start() {
            //tracker ids needed for when there are two trackers
            const int TRACKER_ID = 0;
            skeletalFrameDataProvider = new SkeletalFrameDataProvider(TRACKER_ID, sensorOrientation);
        }

        private void Update() {
            if (!skeletalFrameDataProvider.HasStarted) { return; }
            if (!skeletalFrameDataProvider.ExtractData(ref frameData)) { return; }
            if (frameData.NumOfBodies == 0) { return; }
            bodyTracker.UpdateSkeleton(frameData);
        }

        private void OnDestroy() {
            if (skeletalFrameDataProvider != null) {
                skeletalFrameDataProvider.Dispose();
            }
        }
    }
}