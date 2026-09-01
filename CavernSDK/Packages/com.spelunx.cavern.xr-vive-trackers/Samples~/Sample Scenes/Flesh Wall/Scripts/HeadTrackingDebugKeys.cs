using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Spelunx.XR.Vive.Samples.FleshWall{
[DisallowMultipleComponent]
public class SampleDebugKeys : MonoBehaviour, ICavernDebugKeysFeature
{
    [SerializeField] GameObject head;

    [Header("Input Actions")]
    [SerializeField, Tooltip("Toggle head tracking")]
    private InputAction headTracking = new("Toggle Head Tracking", InputActionType.Value, "<Keyboard>/x");

    private Vector3 cameraStartPos;
    private bool doHeadTracking = false;

    public List<(string Key, string Description)> KeyDescriptions()
    {
        return new(){
                (headTracking.GetBindingDisplayString(), "Toggle head tracking")
            };
    }

    // enable the input actions on play mode start
    void OnEnable()
    {
        headTracking.Enable();
    }


    // disable the input actions on play mode stop
    void OnDisable()
    {
        headTracking.Disable();
    }

    // bind the proper callbacks to each action.performed
    // using the saved key managers
    // This must happen in play mode, not in edit mode, or it won't work.
    void Awake()
    {
        headTracking.performed += ToggleHeadTrackAction;
    }

    void Start()
    {
        cameraStartPos = head.transform.position;
        head.GetComponent<FollowInteraction>().enabled = false;
    }

    public void ToggleHeadTrackAction(InputAction.CallbackContext ctx)
    {
        doHeadTracking = !doHeadTracking;
        if (doHeadTracking)
        {
            head.GetComponent<FollowInteraction>().enabled = true;
        }
        else
        {
            head.GetComponent<FollowInteraction>().enabled = false;
            head.transform.position = cameraStartPos;

        }
    }
    public void DoExtraGUI()
    {
    }
}
}