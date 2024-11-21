using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class FSM<EnumState, EnumFlag> where EnumState : Enum where EnumFlag : Enum
{
    private const int UNNASSIGNED_TRASNSITION = -1;
    public int currentState = 0;
    private Dictionary<int, State> behaviours;
    private Dictionary<int, Func<object[]>> behaviourOnTickParameters;
    private Dictionary<int, Func<object[]>> behaviourOnEnterParameters;
    private Dictionary<int, Func<object[]>> behaviourOnExitParameters;
    private (int destinationState, Action onTransitions)[,] transitions;

    private ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };

    private BehaviourActions GetCurrentStateOnEnterBehaviours => behaviours[currentState]
        .GetEnterBehaviours(behaviourOnEnterParameters[currentState]?.Invoke());

    private BehaviourActions GetCurrentStateOnExitBehaviours => behaviours[currentState]
        .GetExitBehaviours(behaviourOnExitParameters[currentState]?.Invoke());

    private BehaviourActions GetCurrentStateOnTickBehaviours => behaviours[currentState]
        .GetTickBehaviours(behaviourOnTickParameters[currentState]?.Invoke());

    public FSM()
    {
        int states = Enum.GetValues(typeof(EnumState)).Length;
        int flags = Enum.GetValues(typeof(EnumFlag)).Length;
        behaviours = new Dictionary<int, State>();
        transitions = new (int destinationState, Action onTransitions)[states, flags];

        for (int i = 0; i < states; i++)
        {
            for (int j = 0; j < flags; j++)
            {
                transitions[i, j] = (UNNASSIGNED_TRASNSITION, null);
            }
        }

        behaviourOnTickParameters = new Dictionary<int, Func<object[]>>();
        behaviourOnEnterParameters = new Dictionary<int, Func<object[]>>();
        behaviourOnExitParameters = new Dictionary<int, Func<object[]>>();
    }

    public void Transition(Enum flag)
    {
        if (transitions[currentState, Convert.ToInt32(flag)].destinationState != UNNASSIGNED_TRASNSITION)
        {
            ExecuteBehaviour(GetCurrentStateOnExitBehaviours);
            transitions[currentState, Convert.ToInt32(flag)].onTransitions?.Invoke();
            currentState = transitions[currentState, Convert.ToInt32(flag)].destinationState;
            ExecuteBehaviour(GetCurrentStateOnEnterBehaviours);
        }
    }

    public void AddBehaviour<T>(EnumState state, Func<object[]> onTickParametes = null,
        Func<object[]> onEnterParametes = null,
        Func<object[]> onExitParametes = null) where T : State, new()
    {
        int stateIndex = Convert.ToInt32(state);
        if (!behaviours.ContainsKey(stateIndex))
        {
            State newBehaviour = new T();
            behaviours.Add(stateIndex, newBehaviour);
            behaviourOnEnterParameters.Add(stateIndex, onEnterParametes);
            behaviourOnTickParameters.Add(stateIndex, onTickParametes);
            behaviourOnExitParameters.Add(stateIndex, onExitParametes);
            newBehaviour.OnFlag += Transition;
        }
    }

    public void SetTransition(EnumState originState, EnumFlag flag, EnumState destinationState,
        Action onTransitions = null)
    {
        transitions[Convert.ToInt32(originState), Convert.ToInt32(flag)] =
            (Convert.ToInt32(destinationState), onTransitions);
    }

    public void ForceState(EnumState state)
    {
        currentState = Convert.ToInt32(state);
        ExecuteBehaviour(GetCurrentStateOnEnterBehaviours);
    }

    public void Tick()
    {
        if (behaviours.ContainsKey(currentState))
        {
            ExecuteBehaviour(GetCurrentStateOnTickBehaviours);
        }
    }

    public void ExecuteBehaviour(BehaviourActions behaviourAction)
    {
        if (behaviourAction.Equals(default(BehaviourActions)))
        {
            return;
        }

        int executionOrder = 0;


        while ((behaviourAction.mainThreadBehaviours != null && behaviourAction.mainThreadBehaviours.Count > 0) ||
               (behaviourAction.multithreadBehaviours != null && behaviourAction.multithreadBehaviours.Count > 0))
        {
            Task multithreadableBehaviours = new Task(() =>
            {
                if (behaviourAction.multithreadBehaviours != null &&
                    behaviourAction.multithreadBehaviours.ContainsKey(executionOrder))
                {
                    Parallel.ForEach(behaviourAction.multithreadBehaviours[executionOrder], parallelOptions,
                        (behaviour) => { behaviour?.Invoke(); });
                    behaviourAction.multithreadBehaviours.TryRemove(executionOrder, out _);
                }
            });
            multithreadableBehaviours.Start();

            if (behaviourAction.mainThreadBehaviours != null &&
                behaviourAction.mainThreadBehaviours.ContainsKey(executionOrder))
            {
                foreach (Action behaviour in behaviourAction.mainThreadBehaviours[executionOrder])
                {
                    behaviour?.Invoke();
                }

                behaviourAction.mainThreadBehaviours.Remove(executionOrder);
            }

            multithreadableBehaviours.Wait();
            executionOrder++;
        }

        behaviourAction.transitionBehaviour?.Invoke();
    }
}