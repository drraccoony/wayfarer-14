using Content.Shared.FixedPoint;

namespace Content.Server._WF.Nutrition;

[RegisterComponent]
public sealed partial class CookingPotComponent : Component
{
    [DataField]
    public string Solution = "food";

    [DataField]
    public float BoilingTemperature = 373.15f;

    [DataField]
    public float BoilingTime = 5f;

    [DataField]
    public FixedPoint2 MinimumWater = FixedPoint2.New(10);

    public float BoilingProgress;

    public readonly Dictionary<EntityUid, float> CookingProgress = new();
}