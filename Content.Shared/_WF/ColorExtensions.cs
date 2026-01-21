using System.Numerics;

namespace Content.Shared._WF;
public static class ColorExtensions
{
    /// <summary>
    /// Generates a random color based on an input string as a seed
    /// </summary>
    /// <param name="input">The input string to use as a seed</param>
    /// <param name="minimumLightness">An optional minimum lightness to maintain contrast, 0-255</param>
    /// <returns>A random color with the given minimum lightness. Always the same color for the same input string.</returns>
    public static Color ConsistentRandomSeededColorFromString(string input, int minimumLightness = 0)
    {
        // Use a consistent hash function to generate a seed from the input string
        var seed = input.GetHashCode();
        System.Random random = new(seed);

        // Generate random HSL values
        var h = random.Next(0, 256) / 255.0F;
        var s = random.Next(0, 256) / 255.0F;
        var l = random.Next(minimumLightness, 256) / 255.0F;

        return Color.FromHsl(new Vector4(h, s, l, 1.0F));
    }
}
