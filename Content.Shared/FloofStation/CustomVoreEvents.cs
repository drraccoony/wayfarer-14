using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.FloofStation;

[Serializable, NetSerializable]
public sealed class CustomVorePresetData
{
    public string Nickname;
    public string CustomAttemptText;
    public string CustomEmote;
    public string CustomDigestionEmote;
    public bool PlayStomachSounds;
    public bool AllowDigestion;

    public CustomVorePresetData(string nickname, string customAttemptText, string customEmote, string customDigestionEmote, bool playStomachSounds, bool allowDigestion)
    {
        Nickname = nickname;
        CustomAttemptText = customAttemptText;
        CustomEmote = customEmote;
        CustomDigestionEmote = customDigestionEmote;
        PlayStomachSounds = playStomachSounds;
        AllowDigestion = allowDigestion;
    }
}

[Serializable, NetSerializable]
public sealed class OpenCustomVoreWindowEvent : EntityEventArgs
{
    public NetEntity Target;
    public bool CanAllowDigestion;

    public OpenCustomVoreWindowEvent(NetEntity target, bool canAllowDigestion)
    {
        Target = target;
        CanAllowDigestion = canAllowDigestion;
    }
}

[Serializable, NetSerializable]
public sealed class SubmitCustomVoreEvent : EntityEventArgs
{
    public NetEntity Target;
    public string CustomEmote;
    public string CustomDigestionEmote;
    public bool PlayStomachSounds;
    public bool AllowDigestion;

    public SubmitCustomVoreEvent(NetEntity target, string customEmote, string customDigestionEmote, bool playStomachSounds, bool allowDigestion)
    {
        Target = target;
        CustomEmote = customEmote;
        CustomDigestionEmote = customDigestionEmote;
        PlayStomachSounds = playStomachSounds;
        AllowDigestion = allowDigestion;
    }
}

[Serializable, NetSerializable]
public sealed class UpdateCustomVorePresetsEvent : EntityEventArgs
{
    public List<CustomVorePresetData> Presets;

    public UpdateCustomVorePresetsEvent(List<CustomVorePresetData> presets)
    {
        Presets = presets;
    }
}