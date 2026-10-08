using Robust.Shared.Prototypes;

namespace Content.Server._WF.Nutrition;

[RegisterComponent]
public sealed partial class BoilableFoodComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public float CookingTime = 15f;

    [DataField]
    public Dictionary<string, string> ReagentConversions = new();
}