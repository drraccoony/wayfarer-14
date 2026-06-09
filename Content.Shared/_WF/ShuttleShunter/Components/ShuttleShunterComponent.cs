using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._WF.ShuttleShunter.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ShuttleShunterComponent : Component
{
    /// <summary>
    /// Maximum range in tiles to detect a nearby player shuttle.
    /// </summary>
    [DataField]
    public float Range = 20f;

    /// <summary>
    /// Speed in m/s applied to the shuttle on shunt.
    /// </summary>
    [DataField]
    public float ShuntForce = 5f;

    [Serializable, NetSerializable]
    public enum ShuttleShunterUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ShuttleShunterBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string ShuttleName;
        public readonly string ShuttleOwner;
        public readonly string AppraisalValue;
        public readonly NetEntity ShuttleUid;

        public ShuttleShunterBoundUserInterfaceState(string shuttleName, string shuttleOwner, string appraisalValue, NetEntity shuttleUid)
        {
            ShuttleName = shuttleName;
            ShuttleOwner = shuttleOwner;
            AppraisalValue = appraisalValue;
            ShuttleUid = shuttleUid;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ShuttleShunterShuntMessage : BoundUserInterfaceMessage;
}
