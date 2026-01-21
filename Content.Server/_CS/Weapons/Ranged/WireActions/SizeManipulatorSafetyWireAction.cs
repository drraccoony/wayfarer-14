using Content.Server.Wires;
using Content.Shared._CS.Weapons.Ranged.Components;
using Content.Shared.Wires;

namespace Content.Server._CS.Weapons.Ranged.WireActions;

/// <summary>
/// Wire action that controls the safety limiter on the size manipulator.
/// When cut, disables the safety limiter and doubles the max size limit.
/// </summary>
public sealed partial class SizeManipulatorSafetyWireAction : ComponentWireAction<SizeManipulatorComponent>
{
    public override string Name { get; set; } = "wire-name-sizemanipulator-safety";
    public override Color Color { get; set; } = Color.Red;
    public override object StatusKey { get; } = SizeManipulatorWireStatus.Safety;

    public override StatusLightState? GetLightState(Wire wire, SizeManipulatorComponent component)
    {
        return component.SafetyDisabled ? StatusLightState.Off : StatusLightState.On;
    }

    public override bool Cut(EntityUid user, Wire wire, SizeManipulatorComponent component)
    {
        component.SafetyDisabled = true;
        EntityManager.Dirty(wire.Owner, component);
        return true;
    }

    public override bool Mend(EntityUid user, Wire wire, SizeManipulatorComponent component)
    {
        component.SafetyDisabled = false;
        EntityManager.Dirty(wire.Owner, component);
        return true;
    }

    public override void Pulse(EntityUid user, Wire wire, SizeManipulatorComponent component)
    {
        // Pulsing temporarily disables safety for a moment, but this is just a wire pulse
        // so we won't implement a temporary effect - cutting is the main interaction
    }

    public override void Update(Wire wire)
    {
    }
}
