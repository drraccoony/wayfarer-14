namespace Content.Shared._CS.Weapons.Ranged.Events;

/// <summary>
/// Raised to determine what ammo or hitscan prototype should be used for a shot.
/// Allows systems to override the default ammo selection.
/// </summary>
[ByRefEvent]
public struct GetAmmoToUseEvent
{
    public EntityUid Gun;
    public string? Hitscan;
    public EntityUid? Projectile;
}
