using Content.Shared._WF.RouletteTable.BUI;
using Content.Shared._WF.RouletteTable.Events;
using Robust.Client.UserInterface;

namespace Content.Client._WF.RouletteTable;

public sealed class RouletteTableBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private RouletteTableWindow? _window;

    public RouletteTableBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) { }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RouletteTableWindow>();
        _window.Title = Loc.GetString("roulette-table-title");
        _window.OnPlaceBet += (betType, betValue, amount) =>
            SendMessage(new RoulettePlaceBetMessage(betType, betValue, amount));
        _window.OnSpin += () => SendMessage(new RouletteSpinMessage());
        _window.OnClearMyBets += () => SendMessage(new RouletteClearMyBetsMessage());
        _window.OnCashOut += () => SendMessage(new RouletteCashOutMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not RouletteTableBUIState castState)
            return;
        _window?.UpdateState(castState);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _window?.Dispose();
    }
}
