using System;
using System.Collections.Generic;

public class ECSEntity
{
    private class EntityID
    {
        private static uint LastEntityID = 0;
        internal static uint GetNew() => LastEntityID++;
    }

    private uint ID;
    private List<Type> componentsType;

    public ECSEntity()
    {
        ID = EntityID.GetNew();
        componentsType = new List<Type>();
    }

    public uint GetID() => ID;

    public void Dispose()
    {
        componentsType.Clear();
    }

    public void AddComponentType<ComponentType>() where ComponentType : ECSComponent
    {
        AddComponentType(typeof(ComponentType));
    }

    public void AddComponentType(Type ComponentType)
    {
        componentsType.Add(ComponentType);
    }

    public bool ContainsComponentType<ComponentType>() where ComponentType : ECSComponent
    {
        return ContainsComponentType(typeof(ComponentType));
    }

    public bool ContainsComponentType(Type ComponentType)
    {
        return componentsType.Contains(ComponentType);
    }
}
