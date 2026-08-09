using Content.Shared.Stacks;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._WF.Cargo.Components;

/// <summary>
/// Additional currency when sold in appropiate target. Based of NFs
/// </summary>
[RegisterComponent]
public sealed partial class WFBonusCurrencyComponent : Component
{
    /// <summary>
    ///     The stack prototype to spawn when the item is sold.
    /// </summary>
    [DataField(required: true)] public ProtoId<StackPrototype> Currency;

    /// <summary>
    ///     The amount of entities to spawn.
    /// </summary>
    [DataField] public int Amount = 1;

}
