using System.Collections.Generic;

public class RadiusComponent : ECSComponent
{
    public float radius;

    public RadiusComponent( float radius)
    {
        this.radius = radius;
    }
}