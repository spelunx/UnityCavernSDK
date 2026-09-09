using UnityEngine;
using System.Collections;

namespace Spelunx.XR.Vive.Samples.MainSample{
public class CreatureBehavior : MonoBehaviour
{
    [SerializeField, Tooltip("Vive Tracker")] private GameObject player;
    [SerializeField, Tooltip("Cavern mirror")] private CavernSetup cavernSetup;
    [SerializeField, Tooltip("Audio Manager")] private AudioManager audioManager;
    [SerializeField, Tooltip("Big Creature Animator")] private Animator bigCreaAnim;
    [SerializeField, Tooltip("Hugging Particle")] private GameObject huggingParticle;

    private bool nearBigCreature = false;
    private Animator creatureAnimator;

    private void Start()
    {
        creatureAnimator = GetComponent<Animator>();
    }

    public GameObject GetViveTrackerObject()
    {
        return player;
    }

    public CavernSetup GetCavernSetup()
    {
        return cavernSetup;
    }


    public void PlayStartScene(FiniteStateMachine fsm)
    {
        StartCoroutine(PlayingIntroScene(fsm));
    }

    private IEnumerator PlayingIntroScene(FiniteStateMachine fsm)
    {
        yield return new WaitForSeconds(10f);
        audioManager.PlayCreatureFlyingSound();
        
        creatureAnimator.SetTrigger("Intro");
        yield return new WaitForSeconds(41f);
        
        yield return new WaitForSeconds(3f);
        fsm.ChangeState("OnGround");
    }

    public void PlayWhining()
    {
        creatureAnimator.SetTrigger("Default");
        audioManager.PlayCreatureWhiningSound();
    }

    public void PlayCorrect()
    {
        audioManager.PlayCorrectSound();
    }

    public void PlayMirroring()
    {
        creatureAnimator.applyRootMotion = true;
        audioManager.PlayCreatureFlyingSound();
    }

    public void PlayHugToTree(FiniteStateMachine fsm)
    {
        StartCoroutine(PlayingLastScene(fsm));
    }

    private IEnumerator PlayingLastScene(FiniteStateMachine fsm)
    {
        creatureAnimator.applyRootMotion = false;
        
        yield return new WaitForSeconds(1f);
        creatureAnimator.SetTrigger("ToHug");
        
        yield return new WaitForSeconds(2f);
        audioManager.PlayCreatureHuggingSound();
        
        yield return new WaitForSeconds(2f);
        huggingParticle.SetActive(true);
        
        yield return new WaitForSeconds(1f);
        audioManager.PlayBigCreatureHuggingSound();
        bigCreaAnim.SetTrigger("Hug");
        
        yield return new WaitForSeconds(5f);
        creatureAnimator.SetTrigger("BackToTree");
        
        yield return new WaitForSeconds(1f);
        huggingParticle.SetActive(false);
        
        yield return new WaitForSeconds(2f);
        bigCreaAnim.SetTrigger("Default");
        
        yield return new WaitForSeconds(4f);
        fsm.ChangeState("Start");
    }

    public bool NearBigCreature()
    {
        return nearBigCreature;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("MeetingArea"))
        {
            //Debug.Log("big creature enter");
            nearBigCreature = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("MeetingArea"))
        {
            //Debug.Log("big creature exit");
            nearBigCreature = false;
        }
    }
}
}