using UnityEngine;

namespace Spelunx
{
    public class DisplayActivator : MonoBehaviour
    {
        void Start()
        {
            for (int i = 1; i < Display.displays.Length; i++)
            {
                Display.displays[i].Activate();
            }
        }
    }
}
