using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Spelunx.Orbbec {
    public abstract class FrameDataProvider : IDisposable {
        // Public variables.
        public delegate void FinishCallback();
        public bool HasStarted { get; protected set; } = false;

        // Internal variables.
        private FrameData data = new FrameData();
        private object dataMutex = new object();
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private bool hasData = false;

        public FrameDataProvider(int id, FinishCallback onFinish = null) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting += OnEditorClose;
#endif
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            Task.Run(() => RunBackgroundThreadAsync(id, cancellationToken, onFinish));
        }

        private void OnEditorClose() { Dispose(); }

        protected abstract void RunBackgroundThreadAsync(int id, CancellationToken token, FinishCallback onFinish);

        public bool HasData() { return hasData; }

        public void SetData(ref FrameData input) {
            lock (dataMutex) {
                hasData = true;
                var temp = data;
                data = input;
                input = temp;
            }
        }

        public bool ExtractData(ref FrameData output) {
            lock (dataMutex) {
                if (!hasData) return false;

                hasData = false;
                var temp = data;
                data = output;
                output = temp;
                return true;
            }
        }

        public void Dispose() {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting -= OnEditorClose;
#endif
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = null;
        }
    }
}