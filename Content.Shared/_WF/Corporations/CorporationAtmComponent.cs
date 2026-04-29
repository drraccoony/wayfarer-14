using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.Corporations;

[NetSerializable, Serializable]
public enum CorporationAtmUiKey : byte
{
    Key
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CorporationAtmComponent : Component
{
    [DataField]
    public SoundSpecifier ErrorSound = new SoundPathSpecifier("/Audio/Effects/Cargo/buzz_sigh.ogg");

    [DataField]
    public SoundSpecifier ConfirmSound = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");
}

[Serializable, NetSerializable]
public sealed class CorporationAtmUiState : BoundUserInterfaceState
{
    /// <summary>Corporation name, or null if player has no corporation.</summary>
    public string? CorporationName;
    /// <summary>Corporation ID, or -1 if none.</summary>
    public int CorporationId;
    /// <summary>Current balance in spesos.</summary>
    public int Balance;
    /// <summary>Whether the player can withdraw (Manager or Leader).</summary>
    public bool CanWithdraw;
    /// <summary>Player's own bank balance.</summary>
    public int PlayerBalance;
    /// <summary>Error/status message loc key, or empty string.</summary>
    public string StatusMessage;

    public CorporationAtmUiState(string? corporationName, int corporationId, int balance, bool canWithdraw, int playerBalance, string statusMessage)
    {
        CorporationName = corporationName;
        CorporationId = corporationId;
        Balance = balance;
        CanWithdraw = canWithdraw;
        PlayerBalance = playerBalance;
        StatusMessage = statusMessage;
    }
}

[Serializable, NetSerializable]
public sealed class CorporationAtmDepositMessage : BoundUserInterfaceMessage
{
    public int Amount;
    public CorporationAtmDepositMessage(int amount) => Amount = amount;
}

[Serializable, NetSerializable]
public sealed class CorporationAtmWithdrawMessage : BoundUserInterfaceMessage
{
    public int Amount;
    public CorporationAtmWithdrawMessage(int amount) => Amount = amount;
}
