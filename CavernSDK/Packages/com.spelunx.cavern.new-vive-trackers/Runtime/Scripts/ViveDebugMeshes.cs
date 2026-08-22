using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spelunx.XR.Vive
{
    /// <summary>
    /// This class loads the vive tracker mesh, to be used by ViveTracker when rendering a gizmo.
    /// </summary>
    public static class ViveDebugMeshes
    {
#if UNITY_EDITOR
        public static readonly Mesh trackerMesh = (Mesh)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.new-vive-trackers/Editor/Models/vive_tracker/vive_tracker_3_0.obj", typeof(Mesh));
        public static readonly Material trackerMaterial = (Material)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.new-vive-trackers/Editor/Models/vive_tracker/vive_tracker_textured.mat", typeof(Material));
        public static readonly Mesh indexControllerLeftMesh = (Mesh)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.new-vive-trackers/Editor/Models/index_controller/left_controller.obj", typeof(Mesh));
        public static readonly Material indexControllerLeftMaterial = (Material)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.new-vive-trackers/Editor/Models/index_controller/left_material.mat", typeof(Material));
        public static readonly Mesh indexControllerRightMesh = (Mesh)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.new-vive-trackers/Editor/Models/index_controller/right_controller.obj", typeof(Mesh));
        public static readonly Material indexControllerRightMaterial = (Material)AssetDatabase.LoadAssetAtPath("Packages/com.spelunx.cavern.new-vive-trackers/Editor/Models/index_controller/right_material.mat", typeof(Material));

#endif
    }
}
