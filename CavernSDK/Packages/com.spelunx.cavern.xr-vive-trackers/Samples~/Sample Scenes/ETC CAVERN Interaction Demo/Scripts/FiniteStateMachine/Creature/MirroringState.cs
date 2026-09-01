using UnityEngine;

namespace Spelunx.XR.Vive.Samples.MainSample{
[CreateAssetMenu(fileName = "Mirroring", menuName = "FiniteStateMachine/Creature/Mirroring")]
public class MirroringState : State
{
    [SerializeField, Tooltip("Zone where mirroring won't happen to prevent rapid changing")] private float deadZoneRadius = 0.5f;
    [SerializeField, Tooltip("time (seconds) to stay near big creature")] private float timeToStay = 4.0f;
    [SerializeField, Tooltip("hugging countdown VFX prefab")] private GameObject countdownVFXPrefab;

    private float timer;
    private CreatureBehavior creature;
    private GameObject countdownVFXObj;
    public override string GetName() { return "Mirroring"; }

    public override void OnEnter(FiniteStateMachine fsm, GameObject target) 
    {
        Debug.Log("-----Enter Mirroring state-----");
        creature = target.GetComponent<CreatureBehavior>();
        creature.PlayMirroring();
        timer = 0f;
    }

    public override void OnUpdate(FiniteStateMachine fsm, GameObject target) 
    {
        UpdateLocation(creature);

        // check if stayed by river area more than timeToStay seconds, if yes, changeState(HuggingState)
        if (creature.NearBigCreature())
        {
            timer += Time.deltaTime;

            // play particles
            if (countdownVFXObj == null)
            {
                countdownVFXObj = Instantiate(countdownVFXPrefab);
            }

            Debug.Log($"near big creature with {timer}s");
            if (timer >= timeToStay)
            {
                creature.PlayCorrect();
                Destroy(countdownVFXObj);
                fsm.ChangeState("Hugging");
            }
        }
        else
        {
            timer = 0f;
            // stop particles
            Destroy(countdownVFXObj);
        }
    } 

    private void UpdateLocation(CreatureBehavior creature)
    {
        CavernSetup cavernSetup = creature.GetCavernSetup();
        Transform playerTracker = creature.GetViveTrackerObject().transform;
       
        Vector3 center = cavernSetup.transform.position;
        Vector3 cavernFlatPos = new Vector3(center.x, 0, center.z);
        Vector3 newDirection = (new Vector3(playerTracker.position.x, 0, playerTracker.position.z) - cavernFlatPos).normalized;
        float newRadius = (2 * cavernSetup.CavernRadius - (Vector3.Distance(new Vector3(playerTracker.position.x, 0, playerTracker.position.z), cavernFlatPos)));
        
        if (newRadius > deadZoneRadius)
        {
            Vector3 newVector = newDirection * newRadius;
            Vector3 mirroredPosition = center + new Vector3(newVector.x, playerTracker.position.y, newVector.z);
            creature.transform.position = mirroredPosition;
        }
        // mirroring rotation
        Vector3 mirrorNormal = (creature.transform.position - center).normalized; // Normal of the mirror plane
        Vector3 reflectedForward = Vector3.Reflect(playerTracker.forward, mirrorNormal);
        creature.transform.rotation = Quaternion.LookRotation(reflectedForward, Vector3.up);
    }
}
}