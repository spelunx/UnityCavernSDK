using UnityEngine;
namespace Spelunx.XR.Vive.Samples.MainSample{
[CreateAssetMenu(fileName = "Hugging", menuName = "FiniteStateMachine/Creature/Hugging")]
public class HuggingState : State
{
    public override string GetName() { return "Hugging"; }

    public override void OnEnter(FiniteStateMachine fsm, GameObject target) 
    {
        Debug.Log("-----Enter Hugging state-----");
        
        CreatureBehavior creature = target.GetComponent<CreatureBehavior>();
        creature.PlayHugToTree(fsm);
    }
}
}