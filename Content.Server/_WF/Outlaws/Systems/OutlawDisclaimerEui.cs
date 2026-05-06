using Content.Server.EUI;
using Content.Shared._WF.Outlaws;
using Content.Shared.Eui;

namespace Content.Server._WF.Outlaws.Systems;

public sealed class OutlawDisclaimerEui : BaseEui
{
    private readonly bool _isWanted;

    public OutlawDisclaimerEui(bool isWanted)
    {
        _isWanted = isWanted;
    }

    public override EuiStateBase GetNewState()
    {
        return new OutlawDisclaimerEuiState(_isWanted);
    }

    public override void Opened()
    {
        StateDirty();
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is OutlawDisclaimerEuiMsg.Close)
            Close();
    }
}
