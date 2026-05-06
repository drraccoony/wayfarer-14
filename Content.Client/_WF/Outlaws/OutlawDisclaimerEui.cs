using Content.Client._WF.Outlaws.UI;
using Content.Client.Eui;
using Content.Shared._WF.Outlaws;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._WF.Outlaws;

[UsedImplicitly]
public sealed class OutlawDisclaimerEui : BaseEui
{
    private readonly OutlawDisclaimerWindow _window;

    public OutlawDisclaimerEui()
    {
        _window = new OutlawDisclaimerWindow();
        _window.OnAcknowledge += () => SendMessage(new OutlawDisclaimerEuiMsg.Close());
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not OutlawDisclaimerEuiState s)
            return;

        _window.SetState(s.IsWanted);
    }

    public override void Opened()
    {
        _window.UserInterfaceManager.WindowRoot.AddChild(_window);
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.Wide);
    }

    public override void Closed()
    {
        _window.Orphan();
    }
}
