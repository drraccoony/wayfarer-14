using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.FloofStation;

[Serializable, NetSerializable]
public sealed partial class VoreDoAfterEvent : SimpleDoAfterEvent
{
	public bool PlayStomachSounds;
	public bool AllowDigestion;
	public string? CustomDigestionEmote;
	public string? CustomVorePopupText;
}
