using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CS.Body.Components;

/// <summary>
/// Component for items that revert players back to acceptable size thresholds
/// when they walk past within a certain range.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class SizeReverterComponent : Component
{
    /// <summary>
    /// The range in tiles that the size reverter affects
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 3f;

    /// <summary>
    /// Maximum acceptable size multiplier. If player is larger than this, they will be reverted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxAcceptableSize = 2.0f;

    /// <summary>
    /// Minimum acceptable size multiplier. If player is smaller than this, they will be reverted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinAcceptableSize = 0.5f;

    /// <summary>
    /// Target size to revert to when player is too large
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RevertToLarge = 1.8f;

    /// <summary>
    /// Target size to revert to when player is too small
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RevertToSmall = 0.6f;

    /// <summary>
    /// How often to check for nearby players (in seconds)
    /// </summary>
    [DataField]
    public float UpdateInterval = 0.5f;

    /// <summary>
    /// Delay in seconds before the device can be unwrenched/unanchored
    /// </summary>
    [DataField]
    public TimeSpan UnanchorDelay = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Next time to check for players
    /// </summary>
    [DataField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Whether the size reverter is currently active (requires anchoring)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsActive = false;
}
