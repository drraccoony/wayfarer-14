using Content.Shared.FloofStation;
using Robust.Shared.Player;

namespace Content.Client._Floof.Vore;

public sealed class CustomVoreSystem : EntitySystem
{
    private CustomVoreWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<OpenCustomVoreWindowEvent>(OnOpenCustomVoreWindow);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(_ => SyncPresetsToServer());
        SyncPresetsToServer();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _window?.Dispose();
        _window = null;
    }

    private void OnOpenCustomVoreWindow(OpenCustomVoreWindowEvent msg)
    {
        SyncPresetsToServer();

        _window?.Close();

        _window = new CustomVoreWindow();
        if (TryGetEntity(msg.Target, out var target))
            _window.SetTargetName(Name(target.Value));
        else
            _window.SetTargetName(Loc.GetString("vore-custom-window-unknown-target"));

        _window.SetDigestAllowed(msg.CanAllowDigestion);
        _window.OnGo += submit => RaiseNetworkEvent(new SubmitCustomVoreEvent(msg.Target, submit.CustomEmote, submit.CustomDigestionEmote, submit.PlayStomachSounds, submit.AllowDigestion));
        _window.OpenCentered();
    }

    public void SyncPresetsToServer()
    {
        var state = CustomVoreSettingsStore.Load();
        var presets = new List<CustomVorePresetData>(state.Presets.Count);

        foreach (var preset in state.Presets)
        {
            presets.Add(new CustomVorePresetData(
                preset.Nickname,
                preset.CustomAttemptText,
                preset.CustomEmote,
                preset.CustomDigestionEmote,
                preset.PlayStomachSounds,
                preset.AllowDigestion));
        }

        RaiseNetworkEvent(new UpdateCustomVorePresetsEvent(presets));
    }
}