using System.Linq;

namespace Content.Shared.Humanoid.Markings
{
    public sealed partial class Marking
    {
        public Marking(string markingId,
            List<Color> markingColors, MarkingCategories category) : this(markingId, markingColors.Count, category)
        {
            MarkingId = markingId;
            _markingColors = markingColors;
        }
        public Marking(Marking marking,
            List<Color> markingColors) : this(marking)
        {
            _markingColors = markingColors;
        }

        public Marking(Marking marking, int colorCount) : this(marking)
        {
            List<Color> colors = new();
            for (int i = 0; i < colorCount; i++)
                colors.Add(Color.White);
            _markingColors = colors;
        }

        public Marking(Marking marking,
            IReadOnlyList<Color> markingColors)
            : this(marking)
        {
            _markingColors = new(markingColors);
        }

        /// <summary>
        /// Creates a new marking from metadata, setting defaults based on category
        /// </summary>
        /// <param name="markingId"></param>
        /// <param name="colorCount"></param>
        /// <param name="category"></param>

        public Marking(MarkingDTO? other)
        {
            if (other == null) return;
            MarkingId = other.MarkingId ?? MarkingId;
            _markingColors = new(other.MarkingColors.Select(x => Color.FromHex(x)) ?? _markingColors);
            ShowAtStart = other.Visible ?? ShowAtStart;
            CustomName = other.CustomName ?? CustomName;
            CanToggleVisible = other.CanToggleVisible ?? CanToggleVisible;
            OtherCanToggleVisible = other.OtherCanToggleVisible ?? OtherCanToggleVisible;
            PutOnVerb = other.PutOnVerb ?? PutOnVerb;
            PutOnVerb2p = other.PutOnVerb2p ?? PutOnVerb2p;
            TakeOffVerb = other.TakeOffVerb ?? TakeOffVerb;
            TakeOffVerb2p = other.TakeOffVerb2p ?? TakeOffVerb2p;
        }
        public MarkingDTO ToDTO()
        {
            return new MarkingDTO()
            {
                MarkingId = MarkingId,
                CanToggleVisible = CanToggleVisible,
                CustomName = CustomName,
                MarkingColors = _markingColors.Select(x => x.ToHex()).ToList(),
                Visible = ShowAtStart,
                OtherCanToggleVisible = OtherCanToggleVisible,
                PutOnVerb = PutOnVerb,
                PutOnVerb2p = PutOnVerb2p,
                TakeOffVerb = TakeOffVerb,
                TakeOffVerb2p = TakeOffVerb2p
            };
        }

        /// <summary>
        ///     If this marking is can be toggled on or off by the user.
        /// </summary>
        [DataField("customName")]
        public string? CustomName = null;

        /// <summary>
        ///     If this marking is should start enabled.
        /// </summary>
        [DataField("showAtStart")]
        public bool ShowAtStart = true;

        /// <summary>
        ///     If this marking is can be toggled on or off by the user.
        /// </summary>
        [DataField("canToggleVisible")]
        public bool CanToggleVisible = false;

        /// <summary>
        ///     If this marking is can be toggled on or off by the other players.
        /// </summary>
        [DataField("otherCanToggleVisible")]
        public bool OtherCanToggleVisible = false;

        /// <summary>
        ///     Verb to use when putting on
        /// </summary>
        [DataField("putOnVerb")]
        public string PutOnVerb = "put on";

        /// <summary>
        ///     Verb to use when taking off
        /// </summary>
        [DataField("takeOffVerb")]
        public string TakeOffVerb = "take off";

        /// <summary>
        ///     Verb to use when putting on (2nd person)
        /// </summary>
        [DataField("putOnVerb2p")]
        public string PutOnVerb2p = "puts on";

        /// <summary>
        ///     Verb to use when taking off (2nd person)
        /// </summary>
        [DataField("takeOffVerb2p")]
        public string TakeOffVerb2p = "takes off";
    }
}
