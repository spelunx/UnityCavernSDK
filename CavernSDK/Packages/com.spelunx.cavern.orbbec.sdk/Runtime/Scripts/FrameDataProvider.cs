using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Kinect.BodyTracking;

namespace Spelunx.Orbbec {
    public abstract class FrameDataProvider : IDisposable {
        private FrameData data = new FrameData();
        private object mutex = new object();
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
        private bool hasData = false;

        public bool HasStarted { get; protected set; } = false;

        public FrameDataProvider(int id) {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.quitting += OnEditorClose;
#endif
            cancellationTokenSource = new CancellationTokenSource();
            cancellationToken = cancellationTokenSource.Token;
            Task.Run(() => RunBackgroundThreadAsync(id, cancellationToken));
        }

        private void OnEditorClose() { Dispose(); }

        protected abstract void RunBackgroundThreadAsync(int id, CancellationToken token);

        public bool HasData() { return hasData; }

        public void SetData(ref FrameData input) {
            lock (mutex) {
                hasData = true;
                var temp = data;
                data = input;
                input = temp;
            }
        }

        public bool ExtractData(ref FrameData output) {
            lock (mutex) {
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