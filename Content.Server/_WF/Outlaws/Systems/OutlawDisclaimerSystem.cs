using Content.Server.EUI;
using Content.Shared.GameTicking;

namespace Content.Server._WF.Outlaws.Systems;

/// <summary>
///     Added to Outlaw and Wanted Outlaw entities to show a disclaimer popup on spawn.
///     Set <see cref="IsWanted"/> to true for the Wanted Outlaw extended disclaimer.
/// </summary>
[RegisterComponent]
public sealed partial class OutlawDisclaimerComponent : Component
{
    [DataField]
    public bool IsWanted;
}

public sealed class OutlawDisclaimerSystem : EntitySystem
{
    [Dependency] private readonly EuiManager _euiManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<OutlawDisclaimerComponent, PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(EntityUid uid, OutlawDisclaimerComponent component, PlayerSpawnCompleteEvent args)
    {
        if (args.Player.AttachedEntity != uid)
            return;

        _euiManager.OpenEui(new OutlawDisclaimerEui(component.IsWanted), args.Player);
    }
}
