using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CS.Weapons.Ranged.Components;

/// <summary>
/// Component for guns that can toggle between growing and shrinking targets.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SizeManipulatorComponent : Component
{
    /// <summary>
    /// Current mode of the size manipulator (grow or shrink)
    /// </summary>
    [DataField, AutoNetworkedField]
    public SizeManipulatorMode Mode = SizeManipulatorMode.Grow;

    /// <summary>
    /// The grow hitscan prototype ID
    /// </summary>
    [DataField(required: true)]
    public string GrowPrototype = string.Empty;

    /// <summary>
    /// The shrink hitscan prototype ID
    /// </summary>
    [DataField(required: true)]
    public string ShrinkPrototype = string.Empty;

    /// <summary>
    /// Whether the safety limiter has been disabled via hacking.
    /// When disabled, doubles the max size limit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SafetyDisabled = false;
}

/// <summary>
/// Component for the projectiles fired by size manipulator guns.
/// Stores which mode (grow/shrink) this projectile should apply.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BulletSizeManipulatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public SizeManipulatorMode Mode = SizeManipulatorMode.Grow;

    /// <summary>
    /// Whether this projectile was fired from a gun with disabled safety.
    /// If true, allows double the normal max size limit.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool SafetyDisabled = false;
}

[Serializable, NetSerializable]
public enum SizeManipulatorMode : byte
{
    Grow,
    Shrink
}

/// <summary>
/// Status light keys for the size manipulator wires
/// </summary>
[Serializable, NetSerializable]
public enum SizeManipulatorWireStatus
{
    Safety
}
