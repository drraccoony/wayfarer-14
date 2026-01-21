using System.Collections.Generic;
using Content.Client.Humanoid;
using Content.Shared._CS.Body.Components;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Content.Client._CS.Body.Systems;

/// <summary>
/// Handles visual scaling for entities affected by size manipulation.
/// </summary>
public sealed class SizeAffectedVisualsSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private readonly Dictionary<EntityUid, float> _baseScales = new();

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("size_manipulator");

        SubscribeLocalEvent<SizeAffectedComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<SizeAffectedComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<SizeAffectedComponent, ComponentShutdown>(OnComponentShutdown);
    }

    private void OnComponentStartup(EntityUid uid, SizeAffectedComponent component, ComponentStartup args)
    {
        // Store the original scale when the component is first added
        if (!TryComp<SpriteComponent>(uid, out var sprite))
        {
            return;
        }

        // Store the current scale as the base scale for this entity
        if (!_baseScales.ContainsKey(uid))
        {
            _baseScales[uid] = sprite.Scale.X;
            _sawmill.Debug($"SizeAffectedVisuals: Stored base scale {sprite.Scale.X} for {ToPrettyString(uid)}");
        }

        UpdateScale(uid, component, sprite);
    }

    private void OnHandleState(EntityUid uid, SizeAffectedComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        UpdateScale(uid, component, sprite);
    }

    private void OnComponentShutdown(EntityUid uid, SizeAffectedComponent component, ComponentShutdown args)
    {
        // Clean up stored base scale
        _baseScales.Remove(uid);
    }

    private void UpdateScale(EntityUid uid, SizeAffectedComponent component, SpriteComponent sprite)
    {
        // If this is a humanoid, use a special path to update through the humanoid appearance system
        if (TryComp<HumanoidAppearanceComponent>(uid, out var humanoid))
        {
            _sawmill.Debug($"SizeAffectedVisuals: Entity {ToPrettyString(uid)} is humanoid, delegating to HumanoidAppearanceSystem");

            // The HumanoidAppearanceSystem.UpdateSprite already checks for SizeAffectedComponent
            // We just need to trigger it by simulating what happens on state update
            // Get the private UpdateSprite method through reflection or use a public method
            // Actually, let's just manually update since we have access to the components

            // Calculate the final scale with size multiplier
            var height = humanoid.Height * component.ScaleMultiplier;
            var width = humanoid.Width * component.ScaleMultiplier;

            _sprite.SetScale(uid, new(width, height));
            _sawmill.Debug($"SizeAffectedVisuals: Updated humanoid scale to ({width}, {height}) with multiplier {component.ScaleMultiplier}");
            return;
        }

        // For non-humanoids, directly update the sprite scale
        var baseScale = _baseScales.GetValueOrDefault(uid, 1.0f);
        var scale = component.ScaleMultiplier * baseScale;
        var oldScale = sprite.Scale;
        _sprite.SetScale(uid, new(scale, scale));
        _sawmill.Debug($"SizeAffectedVisuals: Updated scale for {ToPrettyString(uid)} from {oldScale} to {sprite.Scale} (multiplier: {component.ScaleMultiplier}, base: {baseScale})");
    }
}
