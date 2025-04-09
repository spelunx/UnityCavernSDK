using System.IO;
using Mono.Cecil;
using UnityEditor;
using UnityEngine;

namespace Spelunx.Orbbec {
    [InitializeOnLoad]
    public class DependencyFileHandler {
        static DependencyFileHandler() {
            CreateDirectories();
            CopyBodyTrackingConfig();
        }

        private static void CreateDirectories() {
            // Create StreamingAssets directory.
            if (!Directory.Exists(Application.streamingAssetsPath)) { Directory.CreateDirectory(Application.streamingAssetsPath); }
        }

        private static void CopyBodyTrackingConfig() {
            string source = Path.Combine(DependecyFiles.BODY_TRACKER_CONFIG_DIR, DependecyFiles.BODY_TRACKER_CONFIG_FILE);
            string destination = Path.Combine(Application.streamingAssetsPath, DependecyFiles.BODY_TRACKER_CONFIG_FILE);
            if (!File.Exists(destination)) {
                File.Copy(source, destination, true);
                UnityEngine.Debug.Log($"Spelunx Cavern ORBBEC SDK: Copied file {source} to {destination}.");
            }
        }
    }
}