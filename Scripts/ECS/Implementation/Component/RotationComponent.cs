public class RotationComponent : ECSComponent
{
    public float X;
    public float Y;
    public float Z;
    public float w;

    public RotationComponent(float X, float Y, float Z, float w)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
        this.w = w;
    }
}

public abstract class Vector3Component : ECSComponent
{
    public float X;
    public float Y;
    public float Z;

    public Vector3Component(float X, float Y, float Z)
    {
        this.X = X;
        this.Y = Y;
        this.Z = Z;
    }
}
public abstract class FloatComponent : ECSComponent
{
    public float X;

    public FloatComponent(float X)
    {
        this.X = X;
    }
}
public sealed class SpeedComponent : FloatComponent
{
    public SpeedComponent(float X) : base(X)
    {
    }
}
public sealed class AlignmentComponent : Vector3Component
{
    public AlignmentComponent(float X, float Y, float Z) : base(X, Y, Z)
    {
    }
}

public sealed class CohesionComponent : Vector3Component
{
    public CohesionComponent(float X, float Y, float Z) : base(X, Y, Z)
    {
    }
}

public sealed class SeparationComponent : Vector3Component
{
    public SeparationComponent(float X, float Y, float Z) : base(X, Y, Z)
    {
    }
}
public sealed class DirectionComponent : Vector3Component
{
    public DirectionComponent(float X, float Y, float Z) : base(X, Y, Z)
    {
    }
}public sealed class ObjectiveComponent : Vector3Component
{
    public ObjectiveComponent(float X, float Y, float Z) : base(X, Y, Z)
    {
    }
}
public sealed class FowardComponent : Vector3Component
{
    public FowardComponent(float X, float Y, float Z) : base(X, Y, Z)
    {
    }
}