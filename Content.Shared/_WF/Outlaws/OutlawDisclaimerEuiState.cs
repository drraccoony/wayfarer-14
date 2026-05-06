using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.Outlaws;

[Serializable, NetSerializable]
public sealed class OutlawDisclaimerEuiState(bool isWanted) : EuiStateBase
{
    /// <summary>
    ///     When true, shows the extended Wanted Outlaw disclaimer text.
    ///     When false, shows the basic Outlaw disclaimer text.
    /// </summary>
    public bool IsWanted { get; } = isWanted;
}

public static class OutlawDisclaimerEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class Close : EuiMessageBase;
}
