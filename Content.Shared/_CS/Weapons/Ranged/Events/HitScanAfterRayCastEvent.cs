using Robust.Shared.Physics;

namespace Content.Shared._CS.Weapons.Ranged.Events;

/// <summary>
/// Raised after a hitscan weapon performs a raycast
/// </summary>
[ByRefEvent]
public record struct HitScanAfterRayCastEvent(
    EntityUid User,
    RayCastResults? RayCastResults
);
