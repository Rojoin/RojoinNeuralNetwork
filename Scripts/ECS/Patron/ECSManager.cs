using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
[Flags]
public enum FlagsActions
{
    OnRotationComplete = 1
}

public static class ECSManager
{
    private static readonly ParallelOptions parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = 32 };

    private static ConcurrentDictionary<uint, ECSEntity> entities = null;
    private static ConcurrentDictionary<Type, ConcurrentDictionary<uint, ECSComponent>> components = null;
    private static ConcurrentDictionary<Type, ECSSystem> systems = null;
    private static ConcurrentDictionary<uint, FlagsActions> response = null;

    public static void Init()
    {
        entities = new ConcurrentDictionary<uint, ECSEntity>();
        components = new ConcurrentDictionary<Type, ConcurrentDictionary<uint, ECSComponent>>();
        systems = new ConcurrentDictionary<Type, ECSSystem>();
        response = new ConcurrentDictionary<uint, FlagsActions>();

        foreach (Type classType in typeof(ECSSystem).Assembly.GetTypes())
        {
            if (typeof(ECSSystem).IsAssignableFrom(classType) && !classType.IsAbstract)
            {
                systems.TryAdd(classType, Activator.CreateInstance(classType) as ECSSystem);
            }
        }

        foreach (KeyValuePair<Type, ECSSystem> system in systems)
        {
            system.Value.Initialize();
        }

        foreach (Type classType in typeof(ECSComponent).Assembly.GetTypes())
        {
            if (typeof(ECSComponent).IsAssignableFrom(classType) && !classType.IsAbstract)
            {
                components.TryAdd(classType, new ConcurrentDictionary<uint, ECSComponent>());
            }
        }
    }

    public static void Tick(float deltaTime)
    {
        Parallel.ForEach(systems, parallelOptions, system =>
        {
            system.Value.Run(deltaTime);
        });
        
    }

    public static uint CreateEntity()
    {
        ECSEntity ecsEntity;
            ecsEntity = new ECSEntity();
            entities.TryAdd(ecsEntity.GetID(), ecsEntity);
        return ecsEntity.GetID();
    }

    public static void AddComponent<ComponentType>(uint entityID, ComponentType component) where ComponentType : ECSComponent
    {
            component.EntityOwnerID = entityID;
            entities[entityID].AddComponentType(typeof(ComponentType));
            components[typeof(ComponentType)].TryAdd(entityID, component);
    }

    public static bool ContainsComponent<ComponentType>(uint entityID) where ComponentType : ECSComponent
    {
        return entities[entityID].ContainsComponentType<ComponentType>();
    }

    public static IEnumerable<uint> GetEntitiesWithComponentTypes(params Type[] componentTypes)
    {
        ConcurrentBag<uint> matchs = new ConcurrentBag<uint>();
        Parallel.ForEach(entities, parallelOptions, entity =>
        {
            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (!entity.Value.ContainsComponentType(componentTypes[i]))
                    return;
            }
            matchs.Add(entity.Key);
        });
        return matchs;
    }

    public static ConcurrentDictionary<uint, ComponentType> GetComponents<ComponentType>() where ComponentType : ECSComponent
    {
        if (components.ContainsKey(typeof(ComponentType)))
        {
            ConcurrentDictionary<uint, ComponentType> comps = new ConcurrentDictionary<uint, ComponentType>();

            Parallel.ForEach(components[typeof(ComponentType)], parallelOptions, component => 
            { 
                comps.TryAdd(component.Key, component.Value as ComponentType);
            });

            return comps;
        }

        return null;
    }

    public static ComponentType GetComponent<ComponentType>(uint entityID) where ComponentType : ECSComponent 
    {
        return components[typeof(ComponentType)][entityID] as ComponentType;
    }
}
