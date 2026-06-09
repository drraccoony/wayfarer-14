using Robust.Client.UserInterface;
using static Content.Shared._WF.ShuttleShunter.Components.ShuttleShunterComponent;

namespace Content.Client._WF.ShuttleShunter.UI;

public sealed class ShuttleShunterBoundUserInterface : BoundUserInterface
{
    private ShuttleShunterWindow? _window;

    public ShuttleShunterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ShuttleShunterWindow>();
        _window.OnShunt += OnShunt;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is ShuttleShunterBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }

    private void OnShunt()
    {
        SendMessage(new ShuttleShunterShuntMessage());
    }
}
