using System;

[Serializable]
public class BuildingConfiguration : LevelElementConfiguration<Building, BuildingConfiguration>
{
    public override void Apply(Building target)
    {
        return;
    }

    public override BuildingConfiguration From(Building source)
    {
        return this;
    }
}