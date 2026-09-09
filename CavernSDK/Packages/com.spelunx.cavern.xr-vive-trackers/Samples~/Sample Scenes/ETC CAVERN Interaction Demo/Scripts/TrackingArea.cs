using UnityEngine;

namespace Spelunx.XR.Vive.Samples.MainSample{
public class TrackingArea : MonoBehaviour
{
    private bool crouching = false;

    public bool Crouching()
    {
        return crouching;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CrouchArea"))
        {
            Debug.Log("Crouching");
            crouching = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("CrouchArea"))
        {
            Debug.Log("Not crouching");
            crouching = false;
        }
    }
}
}