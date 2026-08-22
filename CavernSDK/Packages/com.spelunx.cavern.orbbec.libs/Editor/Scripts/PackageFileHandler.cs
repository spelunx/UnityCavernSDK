using System.IO;
using System.Linq;
using UnityEditor;

namespace Spelunx.Orbbec {
    [InitializeOnLoad]
    public class PackageFileHandler {
        const string PACKAGE_NAME = "com.spelunx.cavern.orbbec.libs";
        const string LIBS_DIR = "Packages/" + PACKAGE_NAME + "/Libs/";
        const string ROOT_DIR = "";

        static PackageFileHandler() {
            // Copy library files into the root folder.
            var sources = Directory.GetFiles(LIBS_DIR).Where(name => !name.EndsWith(".meta"));
            foreach (string source in sources) {
                string destination = Path.Combine(ROOT_DIR, Path.GetFileName(source));

                // Only copy if file doesn't exist or is different.
                if (!File.Exists(destination) || File.GetLastWriteTime(destination) < File.GetLastWriteTime(source)) {
                    File.Copy(source, destination, true);
                    UnityEngine.Debug.Log($"Spelunx Cavern ORBBEC Libraries: Copied file {source} to {destination}.");
                }
            }
        }
    }
}