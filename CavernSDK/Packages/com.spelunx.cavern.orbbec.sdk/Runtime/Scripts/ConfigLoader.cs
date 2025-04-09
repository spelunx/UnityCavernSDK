using System;
using System.IO;
using UnityEngine;

namespace Spelunx.Orbbec {
    // IMPORTANT NOTE: I JUST COPIED THIS FROM THE AZURE SAMPLE. RIGHT NOW THIS DOES ABSOLUTELY JACK SHIT!
    // TODO: Maybe actually make this thing do something one day, or just delete it.
    public class ConfigLoader : MonoBehaviour {
        public static ConfigLoader Instance { get; private set; }
        public Configs Configs { get; private set; } = new Configs();

        private void Awake() {
            if (Instance == null) {
                Instance = this;
            }

            // Path.Combine combines strings into a file path.
            // Application.StreamingAssets points to Assets/StreamingAssets in the Editor, and the StreamingAssets folder in a build.
            string filePath = Path.Combine(Application.streamingAssetsPath, DependecyFiles.BODY_TRACKER_CONFIG_FILE);
            if (File.Exists(filePath)) {
                // Read the json from the file into a string.
                string dataAsJson = File.ReadAllText(filePath);
                // Pass the json to JsonUtility, and tell it to create a Configs object from it.
                Configs = JsonUtility.FromJson<Configs>(dataAsJson);
                UnityEngine.Debug.Log("Successfully loaded config file.");
            } else {
                Debug.LogError("Cannot load game data!");
            }
        }
    }
}