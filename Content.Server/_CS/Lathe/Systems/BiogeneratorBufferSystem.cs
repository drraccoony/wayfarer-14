using Content.Server.Lathe;
using Content.Server.Power.EntitySystems;
using Content.Server._CS.Lathe.Components;
using Content.Shared.Lathe;
using Robust.Shared.Timing;

namespace Content.Server._CS.Lathe.Systems;

public sealed class BiogeneratorBufferSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly LatheSystem _lathe = default!;
    [Dependency] private readonly PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BiogeneratorBufferComponent, MapInitEvent>(OnMapInit);

        // Subscribe to LatheSystem events
        _lathe.OnGetMaterialAmount += OnGetMaterialAmount;
        _lathe.OnDeductMaterial += OnDeductMaterial;
        _lathe.OnGetBufferAmount += OnGetBufferAmount;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _lathe.OnGetMaterialAmount -= OnGetMaterialAmount;
        _lathe.OnDeductMaterial -= OnDeductMaterial;
        _lathe.OnGetBufferAmount -= OnGetBufferAmount;
    }

    private void OnMapInit(EntityUid uid, BiogeneratorBufferComponent component, MapInitEvent args)
    {
        component.NextRegen = _timing.CurTime + component.RegenInterval;
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<BiogeneratorBufferComponent, LatheComponent>();
        var curTime = _timing.CurTime;
        while (query.MoveNext(out var uid, out var buffer, out var lathe))
        {
            if (!buffer.Active || !_power.IsPowered(uid))
                continue;

            if (buffer.NextRegen <= curTime)
            {
                if (buffer.CurrentBuffer < buffer.MaxBuffer)
                {
                    var newBuffer = Math.Min(buffer.MaxBuffer, buffer.CurrentBuffer + buffer.RegenAmount);
                    buffer.CurrentBuffer = newBuffer;
                    _lathe.UpdateUserInterfaceState(uid, lathe);
                }
                buffer.NextRegen = curTime + buffer.RegenInterval;
            }
        }
    }

    private void OnGetMaterialAmount(EntityUid uid, LatheComponent lathe, string material, ref int amount)
    {
        if (material != "Biomass")
            return;
        if (!TryComp<BiogeneratorBufferComponent>(uid, out var buffer))
            return;

        amount += buffer.CurrentBuffer;
    }

    private void OnDeductMaterial(EntityUid uid, LatheComponent lathe, string material, ref int amount)
    {
        if (material != "Biomass")
            return;
        if (!TryComp<BiogeneratorBufferComponent>(uid, out var buffer))
            return;

        int taken = Math.Min(buffer.CurrentBuffer, amount);
        if (taken > 0)
        {
            buffer.CurrentBuffer -= taken;
            amount -= taken;
            _lathe.UpdateUserInterfaceState(uid, lathe);
        }
    }

    private void OnGetBufferAmount(EntityUid uid, LatheComponent lathe, ref int? bufferAmount)
    {
        if (TryComp<BiogeneratorBufferComponent>(uid, out var buffer))
            bufferAmount = buffer.CurrentBuffer;
    }
}
