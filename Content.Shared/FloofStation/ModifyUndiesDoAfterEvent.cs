using Content.Shared.DoAfter;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization;

namespace Content.Shared.FloofStation;

[Serializable, NetSerializable]
public sealed partial class ModifyUndiesDoAfterEvent : DoAfterEvent
{
    [DataField]
    public Marking Marking = default!;

    [DataField]
    public string MarkingPrototypeName = string.Empty;

    [DataField]
    public bool IsVisible;

    public ModifyUndiesDoAfterEvent()
    {
    }

    public ModifyUndiesDoAfterEvent(Marking marking, string markingPrototypeName, bool isVisible)
    {
        Marking = marking;
        MarkingPrototypeName = markingPrototypeName;
        IsVisible = isVisible;
    }

    public override DoAfterEvent Clone()
    {
        return new ModifyUndiesDoAfterEvent(Marking, MarkingPrototypeName, IsVisible);
    }
}
