using System.Collections;
using UnityEngine;

namespace Spelunx.XR.Vive.Samples.MainSample{
[CreateAssetMenu(fileName = "Start", menuName = "FiniteStateMachine/Creature/Start")]
public class StartState : State
{
    public override string GetName() { return "Start"; }

    public override void OnEnter(FiniteStateMachine fsm, GameObject target) 
    {
        Debug.Log("-----Enter Start state-----");
        CreatureBehavior creature = target.GetComponent<CreatureBehavior>();
        creature.PlayStartScene(fsm);
    }
}
}