using Content.Shared.DeviceLinking;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._CS.Weapons.Ranged.Components;

/// <summary>
/// Fires the weapon when signal is received.
/// Supports separate ports for grow and shrink modes.
/// </summary>
[RegisterComponent]
public sealed partial class FireOnSignalComponent : Component
{
    [DataField]
    public ProtoId<SinkPortPrototype> GrowPort = "GrowTrigger";

    [DataField]
    public ProtoId<SinkPortPrototype> ShrinkPort = "ShrinkTrigger";
}
