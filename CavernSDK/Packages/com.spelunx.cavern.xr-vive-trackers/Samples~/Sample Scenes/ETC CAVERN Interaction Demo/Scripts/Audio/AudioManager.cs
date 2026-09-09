using UnityEngine;
using UnityEngine.Audio;
namespace Spelunx.XR.Vive.Samples.MainSample{
public class AudioManager : MonoBehaviour
{
    [SerializeField, Tooltip("Creature AudioSource")] private AudioSource creatureAudioSource;
    [SerializeField, Tooltip("Big Creature AudioSource")] private AudioSource bigCreatureAudioSource;
    [SerializeField, Tooltip("UI AudioSource")] private AudioSource uIAudioSource;

    [Header("Audio Resource")]
    [SerializeField, Tooltip("Creature flying sound")] private AudioResource creatureFlyingSound;
    [SerializeField, Tooltip("Creature whining sound")] private AudioResource creatureWhiningSound;
    [SerializeField, Tooltip("Creature happy pickup sound")] private AudioResource creaturePickUpSound;
    [SerializeField, Tooltip("Creature hugging sound")] private AudioResource creatureHuggingSound;
    [SerializeField, Tooltip("Big creature hugging sound")] private AudioResource bigCreatureHuggingSound;
    [SerializeField, Tooltip("Correct sound")] private AudioResource correctSound;

    public class SpatialBlend
    {
        public const float ThreeD = 1.0f;
        public const float TwoD = 0.0f;
        public const float UI = 0.5f;
        public const float Balance = 0.75f;
    }

    private void Start()
    {
        creatureAudioSource.spatialBlend = SpatialBlend.ThreeD;
        creatureAudioSource.priority = 0;
        
        bigCreatureAudioSource.spatialBlend = SpatialBlend.Balance;
        bigCreatureAudioSource.priority = 0;
        bigCreatureAudioSource.pitch = 0.54f;
        
        uIAudioSource.spatialBlend = SpatialBlend.UI;
        uIAudioSource.priority = 0;
    }

    public void PlayCreatureFlyingSound()
    {
        PlaySound(creatureAudioSource, creatureFlyingSound, SpatialBlend.ThreeD, true);
    }

    public void PlayCreatureWhiningSound()
    {
        PlaySound(creatureAudioSource, creatureWhiningSound, SpatialBlend.Balance, true);
    }
    public void PlayCreatureHappyPickupSound()
    {
        PlaySound(creatureAudioSource, creaturePickUpSound, SpatialBlend.Balance, true);
    }

    public void PlayCreatureHuggingSound()
    {
        PlaySound(creatureAudioSource, creatureHuggingSound, SpatialBlend.Balance, false);
    }

    public void PlayBigCreatureHuggingSound()
    {
        PlaySound(bigCreatureAudioSource, bigCreatureHuggingSound, SpatialBlend.Balance, false);
    }

    public void PlayCorrectSound()
    {
        PlaySound(uIAudioSource, correctSound, SpatialBlend.UI, false);
    }

    private void PlaySound(AudioSource audioSource, AudioResource audioResource, float spatialBlend, bool loop)
    {
        audioSource.Stop();
        audioSource.resource = audioResource;
        audioSource.spatialBlend = spatialBlend;
        audioSource.loop = loop;
        audioSource.Play();
    }
}
}