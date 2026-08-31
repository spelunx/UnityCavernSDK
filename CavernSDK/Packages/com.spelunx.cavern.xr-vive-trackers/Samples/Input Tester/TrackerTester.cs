using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spelunx.XR.Vive
{
    public class TrackerTester : MonoBehaviour
    {
        [SerializeField] private ViveTracker tracker;
        [SerializeField] private TextMeshProUGUI positionField;
        [SerializeField] private TextMeshProUGUI rotationField;
        [SerializeField] private TextMeshProUGUI isTrackedField;
        [SerializeField] private TextMeshProUGUI trackingStateField;
        [SerializeField] private TextMeshProUGUI triggerField;
        [SerializeField] private TextMeshProUGUI squeezeField;
        [SerializeField] private TextMeshProUGUI menuField;
        [SerializeField] private TextMeshProUGUI trackpadField;
        [SerializeField] private TextMeshProUGUI systemField;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        async void Start()
        {
            tracker.PositionAction.performed += UpdateField<Vector3>(positionField);
            tracker.PositionAction.canceled += SetField(positionField, Vector3.zero);
            tracker.RotationAction.performed += UpdateField<Quaternion>(rotationField);
            tracker.RotationAction.canceled += SetField(rotationField, Quaternion.identity);
            tracker.TriggerPress.performed += UpdateButtonField(triggerField);
            tracker.TriggerPress.canceled += SetField(triggerField, false);
            tracker.SqueezePress.performed += UpdateButtonField(squeezeField);
            tracker.SqueezePress.canceled += SetField(squeezeField, false);
            tracker.MenuPress.performed += UpdateButtonField(menuField);
            tracker.MenuPress.canceled += SetField(menuField, false);
            tracker.TrackpadPress.performed += UpdateButtonField(trackpadField);
            tracker.TrackpadPress.canceled += SetField(trackpadField, false);
            tracker.IsTracked.performed += UpdateButtonField(isTrackedField);
            tracker.IsTracked.canceled += SetField(isTrackedField, false);
            tracker.TrackingState.performed += UpdateTrackingField(trackingStateField);
            tracker.TrackingState.canceled += SetField(trackingStateField, UnityEngine.XR.InputTrackingState.None);
            tracker.MenuPress.performed += UpdateButtonField(systemField);
            tracker.MenuPress.canceled += SetField(systemField, false);
            await Awaitable.WaitForSecondsAsync(2);
            tracker.TriggerVibration(1, 4);
        }

        private Action<InputAction.CallbackContext> UpdateButtonField(TextMeshProUGUI field)
        {
            void Updater(InputAction.CallbackContext context)
            {
                field.text = (context.ReadValue<float>() >= 0.5f).ToString();
            }
            return Updater;
        }

        private Action<InputAction.CallbackContext> UpdateTrackingField(TextMeshProUGUI field)
        {
            void Updater(InputAction.CallbackContext context)
            {
                field.text = ((UnityEngine.XR.InputTrackingState)context.ReadValue<int>()).ToString();
            }
            return Updater;
        }

        private Action<InputAction.CallbackContext> UpdateField<T>(TextMeshProUGUI field) where T : struct
        {
            void Updater(InputAction.CallbackContext context)
            {
                field.text = context.ReadValue<T>().ToString();
            }
            return Updater;
        }

        private Action<InputAction.CallbackContext> SetField(TextMeshProUGUI field, object val)
        {
            void Updater(InputAction.CallbackContext context)
            {
                field.text = val.ToString();
            }
            return Updater;
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}
