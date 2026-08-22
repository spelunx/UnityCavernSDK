using UnityEngine;

public class SoundController : MonoBehaviour
{
    public enum SpeakerPosition
    {
        Speaker_0_FrontLeft,
        Speaker_1_FrontRight,
        Speaker_2_Center,
        Speaker_4_RearLeft,
        Speaker_5_RearRight,
        Speaker_6_SideLeft,
        Speaker_7_SideRight
    }

    [Header("Assign AudioSources for each speaker")]
    [SerializeField] private AudioSource speaker0_FrontLeft;
    [SerializeField] private AudioSource speaker1_FrontRight;
    [SerializeField] private AudioSource speaker2_Center;
    [SerializeField] private AudioSource speaker4_RearLeft;
    [SerializeField] private AudioSource speaker5_RearRight;
    [SerializeField] private AudioSource speaker6_SideLeft;
    [SerializeField] private AudioSource speaker7_SideRight;

    [Header("Select which speaker to play")]
    [SerializeField] private SpeakerPosition speakerToPlay;

    public void PlaySelected()
    {
        AudioSource selectedSource = GetSelectedSource();

        if (selectedSource == null)
            return;

        if (selectedSource.isPlaying)
        {
            selectedSource.Stop();
        }
        else
        {
            StopAll();
            selectedSource.Play();
        }
    }

    private void StopAll()
    {
        speaker0_FrontLeft?.Stop();
        speaker1_FrontRight?.Stop();
        speaker2_Center?.Stop();
        speaker4_RearLeft?.Stop();
        speaker5_RearRight?.Stop();
        speaker6_SideLeft?.Stop();
        speaker7_SideRight?.Stop();
    }

    private AudioSource GetSelectedSource()
    {
        return speakerToPlay switch
        {
            SpeakerPosition.Speaker_0_FrontLeft => speaker0_FrontLeft,
            SpeakerPosition.Speaker_1_FrontRight => speaker1_FrontRight,
            SpeakerPosition.Speaker_2_Center => speaker2_Center,
            SpeakerPosition.Speaker_4_RearLeft => speaker4_RearLeft,
            SpeakerPosition.Speaker_5_RearRight => speaker5_RearRight,
            SpeakerPosition.Speaker_6_SideLeft => speaker6_SideLeft,
            SpeakerPosition.Speaker_7_SideRight => speaker7_SideRight,
            _ => null,
        };
    }

#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(SoundController))]
    private class SoundControllerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            SoundController controller = (SoundController)target;

            if (GUILayout.Button("▶ Play/Stop Selected Speaker"))
            {
                controller.PlaySelected();
            }

            if (GUILayout.Button("⏹ Stop All"))
            {
                controller.SendMessage("StopAll", SendMessageOptions.DontRequireReceiver);
            }
        }
    }
#endif
}
