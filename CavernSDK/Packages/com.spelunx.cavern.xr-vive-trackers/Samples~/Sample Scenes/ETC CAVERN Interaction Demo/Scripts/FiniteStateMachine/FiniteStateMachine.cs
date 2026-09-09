using System.Collections.Generic;
using UnityEngine;

namespace Spelunx.XR.Vive.Samples.MainSample{
public abstract class State : ScriptableObject
{
    public abstract string GetName();
    public virtual void OnEnter(FiniteStateMachine fsm, GameObject target) { }
    public virtual void OnUpdate(FiniteStateMachine fsm, GameObject target) { }
    public virtual void OnLateUpdate(FiniteStateMachine fsm, GameObject target) { }
    public virtual void OnExit(FiniteStateMachine fsm, GameObject target) { }
}

public class FiniteStateMachine : MonoBehaviour
{
    [SerializeField] private State[] states = new State[0];
    [SerializeField] private string initialState = "";

    private Dictionary<string, State> runtimeStates = new Dictionary<string, State>();
    private string currentState = "";
    private string nextState = "";

    public void Awake()
    {
        // Make a copy of each state so that each instance of FiniteStateMachine's data is independent.
        nextState = initialState;
        for (int i = 0; i < states.Length; ++i)
        {
            State state = Instantiate(states[i]);
            runtimeStates.Add(state.GetName(), Instantiate(state));
        }
    }

    public string GetInitialState() { return initialState; }
    public string GetCurrentState() { return currentState; }
    public bool HasState(string stateName) { return runtimeStates.ContainsKey(stateName); }
    public void ChangeState(string nextState) { this.nextState = nextState; }

    private void Update()
    {
        // Transit state.
        if (nextState != currentState)
        {
            // Exit current state.
            if (runtimeStates.ContainsKey(currentState))
            {
                runtimeStates[currentState].OnExit(this, gameObject);
            }

            // Enter next state.
            currentState = nextState;
            if (runtimeStates.ContainsKey(currentState))
            {
                runtimeStates[currentState].OnEnter(this, gameObject);
            }
        }

        // Update current state.
        if (runtimeStates.ContainsKey(currentState))
        {
            runtimeStates[currentState].OnUpdate(this, gameObject);
        }
    }

    private void LateUpdate()
    {
        // LateUpdate current state.
        if (runtimeStates.ContainsKey(currentState))
        {
            runtimeStates[currentState].OnLateUpdate(this, gameObject);
        }
    }
}
}