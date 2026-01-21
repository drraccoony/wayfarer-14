using Content.Server._CS.Body.Systems;
using Content.Server._CS.Weapons.Ranged.Components;
using Content.Shared._CS.Weapons.Ranged.Components;
using Content.Server.DeviceLinking.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;
using System.Numerics;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared._CS.Weapons.Ranged.Systems;

namespace Content.Server._CS.Weapons.Ranged.Systems;

public sealed class SizeManipulatorSystem : SharedSizeManipulatorSystem
{
    [Dependency] private readonly SizeManipulationSystem _sizeManipulation = default!;
    [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
    [Dependency] private readonly GunSystem _gunSystem = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("size_manipulator");

        SubscribeLocalEvent<SizeManipulatorComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<BulletSizeManipulatorComponent, ProjectileHitEvent>(OnProjectileHit);

        SubscribeLocalEvent<FireOnSignalComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<FireOnSignalComponent, ComponentInit>(OnInit);
    }

    private void OnSignalReceived(EntityUid uid, FireOnSignalComponent component, ref SignalReceivedEvent args)
    {
        if (!TryComp<GunComponent>(uid, out var gun) || !TryComp<SizeManipulatorComponent>(uid, out var sizeManip))
            return;

        // Determine which mode to use based on which port received the signal
        SizeManipulatorMode? modeToUse = null;

        if (args.Port == component.GrowPort)
            modeToUse = SizeManipulatorMode.Grow;
        else if (args.Port == component.ShrinkPort)
            modeToUse = SizeManipulatorMode.Shrink;

        if (modeToUse == null)
            return;

        // Set the mode before firing
        sizeManip.Mode = modeToUse.Value;
        Dirty(uid, sizeManip);

        // Update the projectile prototype based on the mode
        if (TryComp<ProjectileBatteryAmmoProviderComponent>(uid, out var projectileProvider))
        {
            projectileProvider.Prototype = modeToUse == SizeManipulatorMode.Grow
                ? sizeManip.GrowPrototype
                : sizeManip.ShrinkPrototype;
            Dirty(uid, projectileProvider);
        }

        // Fire the gun - rotate 90 degrees counter-clockwise from the gun's default direction
        // Guns shoot down by default, so rotating 90 degrees makes them shoot in the visual direction
        var dir = gun.DefaultDirection;
        dir = new Vector2(-dir.Y, dir.X); // 90 degrees counter-clockwise rotation
        _gunSystem.AttemptShoot(uid, uid, gun, new EntityCoordinates(uid, dir));
    }

    private void OnInit(EntityUid uid, FireOnSignalComponent component, ComponentInit args)
    {
        _deviceLink.EnsureSinkPorts(uid, component.GrowPort, component.ShrinkPort);
    }

    private void OnAmmoShot(EntityUid uid, SizeManipulatorComponent component, AmmoShotEvent args)
    {
        // Update all fired projectiles with the safety state from the gun
        foreach (var projectile in args.FiredProjectiles)
        {
            if (TryComp<BulletSizeManipulatorComponent>(projectile, out var bullet))
            {
                bullet.SafetyDisabled = component.SafetyDisabled;
                Dirty(projectile, bullet);
            }
        }
    }

    private void OnProjectileHit(EntityUid uid, BulletSizeManipulatorComponent component, ref ProjectileHitEvent args)
    {
        var hitEntity = args.Target;

        if (!Exists(hitEntity))
        {
            _sawmill.Debug("SizeManipulator: Hit entity doesn't exist");
            return;
        }

        _sawmill.Debug($"SizeManipulator: Projectile {ToPrettyString(uid)} hit entity {ToPrettyString(hitEntity)}, applying size change mode: {component.Mode}, safety disabled: {component.SafetyDisabled}");

        // Apply size change to the hit entity, passing the safety state
        _sizeManipulation.TryChangeSize(hitEntity, component.Mode, args.Shooter, component.SafetyDisabled);
    }
}
