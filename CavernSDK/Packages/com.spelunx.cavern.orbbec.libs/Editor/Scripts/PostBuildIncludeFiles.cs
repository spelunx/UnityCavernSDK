using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Spelunx.Orbbec
{
#if UNITY_6000_3_OR_NEWER
    public class PostBuildIncludeFiles : IPostprocessBuildWithContext
    {
        const string PACKAGE_NAME = "com.spelunx.cavern.orbbec.libs";
        readonly string[] copyPaths = { "Plugins", "Libs" };
        public int callbackOrder => 0;
        public void OnPostprocessBuild(BuildCallbackContext ctx)
        {
            if (ctx.Report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) return; // Only need to copy files on successful build

            string buildDirectory = Path.GetDirectoryName(ctx.Report.summary.outputPath);
            string exeName = Path.GetFileNameWithoutExtension(ctx.Report.summary.outputPath);
            string buildPluginsFolder = Path.Combine(buildDirectory, $"{exeName}_Data", "Plugins", "x86_64");

            foreach (string dir in copyPaths)
            {
                var pluginDirectory = Path.Combine("Packages", PACKAGE_NAME, dir);
                var sources = Directory.GetFiles(pluginDirectory).Where(name => !name.EndsWith(".meta"));
                foreach (string source in sources)
                {
                    string destination = Path.Combine(buildPluginsFolder, Path.GetFileName(source));
                    if (!File.Exists(destination) || File.GetLastWriteTime(destination) < File.GetLastWriteTime(source))
                    {
                        File.Copy(source, destination, true);
                    }
                }
            }
        }
    }
#elif UNITY_6000_0_OR_NEWER
public class PostBuildIncludeFiles
    {
        const string PACKAGE_NAME = "com.spelunx.cavern.orbbec.libs";

        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);
            string exeName = Path.GetFileNameWithoutExtension(pathToBuiltProject);
            string buildPluginsFolder = Path.Combine(buildDirectory, $"{exeName}_Data", "Plugins", "x86_64");

            foreach(string dir in copyPaths)
            {
                var pluginDirectory = Path.Combine("Packages", PACKAGE_NAME, dir);
                var sources = Directory.GetFiles(pluginDirectory).Where(name => !name.EndsWith(".meta"));
                foreach (string source in sources)
                {
                    string destination = Path.Combine(buildPluginsFolder, Path.GetFileName(source));
                    if (!File.Exists(destination) || File.GetLastWriteTime(destination) < File.GetLastWriteTime(source))
                    {
                        File.Copy(source, destination, true);
                    }
                }
            }
        }
    }

#endif
}
