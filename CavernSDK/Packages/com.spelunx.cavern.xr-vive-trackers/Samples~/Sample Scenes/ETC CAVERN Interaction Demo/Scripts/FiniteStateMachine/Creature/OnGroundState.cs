using UnityEngine;

namespace Spelunx.XR.Vive.Samples.MainSample{
[CreateAssetMenu(fileName = "OnGround", menuName = "FiniteStateMachine/Creature/OnGround")]
public class OnGroundState : State
{
    private CreatureBehavior creature;
    public override string GetName() { return "OnGround"; }

    public override void OnEnter(FiniteStateMachine fsm, GameObject target) 
    {
        Debug.Log("-----Enter Ground state-----");
        creature = target.GetComponent<CreatureBehavior>();
        creature.PlayWhining();
    }

    public override void OnUpdate(FiniteStateMachine fsm, GameObject target) 
    {
        // wait for player crouch, then changeState(MirroringState)
        TrackingArea tracking = creature.GetViveTrackerObject().GetComponent<TrackingArea>();
        if (tracking.Crouching())
        {
            creature.PlayCorrect();
            fsm.ChangeState("Mirroring");
        }
    }
}
}