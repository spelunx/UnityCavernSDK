using System;
using System.Collections;
using UnityEngine;

namespace Spelunx.XR.Vive.Samples.Frogs{
public class GameManager : MonoBehaviour
{
    public ZoneData[] zones;
    public Zones trackerZones;

    [Serializable]
    public class ZoneData
    {
        public bool isActive = false;
        public bool touched = false;
        public float volume = 0;
        public AudioSource sound;
        public Material onMaterial;
        public Material offMaterial;
        public MeshRenderer frog;
        public Light light;
        public Color onColor;

        public IEnumerator FadeOutSound()
        {
            sound.volume = Mathf.MoveTowards(sound.volume, 0, Time.deltaTime);
            volume = sound.volume;
            if (sound.volume <= 0.001f)
            {
                sound.Stop();
            }
            yield return new WaitForEndOfFrame();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (ZoneData z in zones)
        {
            z.sound.volume = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Zones.ZonedTracker t in trackerZones.zonedTrackers)
        {
            if (t.zone != -1)
            {
                zones[t.zone].isActive = true;
                zones[t.zone].touched = true;
                zones[t.zone].volume = Mathf.Max(zones[t.zone].volume, t.distance);
            }
        }

        foreach (ZoneData z in zones)
        {
            if (!z.touched)
            {
                z.isActive = false;
                StartCoroutine(z.FadeOutSound());
            }
            if (z.isActive)
            {
                StopCoroutine(z.FadeOutSound());
                z.sound.volume = z.volume;
                if (!z.sound.isPlaying)
                {
                    z.sound.Play();
                }
                z.light.color = z.onColor;
                z.frog.material = z.onMaterial;
            }
            else
            {
                // z.light.color = new Color(71, 71, 71);
                z.light.color = Color.white;
                z.frog.material = z.offMaterial;
            }
            z.touched = false;
        }
    }
}
}