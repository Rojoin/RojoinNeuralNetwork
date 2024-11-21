using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

public struct BehaviourActions
{
    public Dictionary<int, List<Action>> mainThreadBehaviours;
    public ConcurrentDictionary<int, ConcurrentBag<Action>> multithreadBehaviours;
    public Action transitionBehaviour;

    public void AddMainThreadBehaviour(int executionOrder, Action behaviour)
    {
        if (mainThreadBehaviours == null)
        {
            mainThreadBehaviours = new Dictionary<int, List<Action>>();
        }

        if (!mainThreadBehaviours.ContainsKey(executionOrder))
        {
            mainThreadBehaviours.Add(executionOrder, new List<Action>());
        }

        mainThreadBehaviours[executionOrder].Add(behaviour);
    }

    public void AddMultiThreadBehaviour(int executionOrder, Action behaviour)
    {
        if (multithreadBehaviours == null)
        {
            multithreadBehaviours = new ConcurrentDictionary<int, ConcurrentBag<Action>>();
        }

        if (!multithreadBehaviours.ContainsKey(executionOrder))
        {
            multithreadBehaviours.TryAdd(executionOrder, new ConcurrentBag<Action>());
        }

        multithreadBehaviours[executionOrder].Add(behaviour);
    }

    public void SetTransitionBehavior(Action transitionBehaviour)
    {
        this.transitionBehaviour = transitionBehaviour;
    }
}

public abstract class State
{
    public Action<Enum> OnFlag;
    public abstract BehaviourActions GetTickBehaviours(params object[] parameters);
    public abstract BehaviourActions GetEnterBehaviours(params object[] parameters);
    public abstract BehaviourActions GetExitBehaviours(params object[] parameters);
}

