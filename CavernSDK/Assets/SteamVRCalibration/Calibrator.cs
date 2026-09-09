using TMPro;
using UnityEngine;

namespace Spelunx.XR.Vive.SteamVRCalibration
{
    public class Calibrator : MonoBehaviour
    {
        [SerializeField] private ViveTracker tracker;
        [SerializeField] private TextMeshProUGUI yawField;
        [SerializeField] private TextMeshProUGUI positionField;


        // Update is called once per frame
        void Update()
        {
            if (!tracker.IsCurrentlyTracking)
            {
                yawField.color = Color.red;
                yawField.text = "not detected";
                positionField.color = Color.red;
                positionField.text = "not detected";
                return;
            }
            tracker.transform.GetPositionAndRotation(out var position, out var rotation);
            // yawField.text = rotation.ConstrainYaw().eulerAngles.ToString();
            yawField.text = $"{Mathf.Deg2Rad * (rotation.eulerAngles.y % 360)}";
            yawField.color = Color.white;
            positionField.text = position.ToString();
            positionField.color = Color.white;
        }

        public void DoCalibrate()
        {
            // TODO: implement
            // C:\Program Files (x86)\Steam\config\chaperone_info.vrchap
            //
        }
    }

}

/*
{
   "jsonid" : "chaperone_info",
   "universes" : [
      {
         "collision_bounds" : [
            [
               [ -0.5, 0, -0.5 ],
               [ -0.5, 2.43000007, -0.5 ],
               [ -0.5, 2.43000007, 0.5 ],
               [ -0.5, 0, 0.5 ]
            ],
            [
               [ -0.5, 0, 0.5 ],
               [ -0.5, 2.43000007, 0.5 ],
               [ 0.5, 2.43000007, 0.5 ],
               [ 0.5, 0, 0.5 ]
            ],
            [
               [ 0.5, 0, 0.5 ],
               [ 0.5, 2.43000007, 0.5 ],
               [ 0.5, 2.43000007, -0.5 ],
               [ 0.5, 0, -0.5 ]
            ],
            [
               [ 0.5, 0, -0.5 ],
               [ 0.5, 2.43000007, -0.5 ],
               [ -0.5, 2.43000007, -0.5 ],
               [ -0.5, 0, -0.5 ]
            ]
         ],
         "play_area" : [ 1, 1 ],
         "seated" : {
            "translation" : [ 0, 1.20000005, 0 ],
            "yaw" : 0
         },
         "standing" : {
            "translation" : [ 0.373, 2.532, 3.141 ],
            "yaw" : -0.995
         },
         "time" : "Wed Jun 24 10:28:18 2026",
         "universeID" : "2"
      }
   ],
   "version" : 5
}
*/