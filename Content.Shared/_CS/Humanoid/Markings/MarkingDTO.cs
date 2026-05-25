using System.Linq;
using System.Text.Json.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.Markings
{
    public sealed class MarkingDTO
    {
        public List<string> MarkingColors { get; set; } = new() { Color.White.ToHex() };

        public MarkingDTO()
        {
        }

        /// <summary>
        ///     ID of the marking prototype.
        /// </summary>
        public string? MarkingId { get; set; }

        /// <summary>
        ///     If this marking is currently visible.
        public bool? Visible { get; set; }

        /// <summary>
        ///     If this marking is can be toggled on or off by the user.
        /// </summary>
        public string? CustomName { get; set; }

        /// <summary>
        ///     If this marking is can be toggled on or off by the user.
        /// </summary>
        public bool? CanToggleVisible { get; set; }

        /// <summary>
        ///     If this marking is can be toggled on or off by the other players.
        /// </summary>
        public bool? OtherCanToggleVisible { get; set; }

        /// <summary>
        ///     Verb to use when putting on
        /// </summary>
        public string? PutOnVerb { get; set; }

        /// <summary>
        ///     Verb to use when taking off
        /// </summary>
        public string? TakeOffVerb { get; set; }

        /// <summary>
        ///     Verb to use when putting on (2nd person)
        /// </summary>
        public string? PutOnVerb2p { get; set; }

        /// <summary>
        ///     Verb to use when taking off (2nd person)
        /// </summary>
        public string? TakeOffVerb2p { get; set; }
    }
}
