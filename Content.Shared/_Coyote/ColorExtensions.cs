namespace Content.Shared._Coyote;
public static class ColorExtensions
{
    /// <summary>
    /// takes a string input and returns a color based on a consistent random seed using the input as a seed.
    /// when the same input is given, the same color will be returned.
    /// </summary>
    /// <param name="input"></param>
    /// <returns>A color</returns>
    public static Color ConsistentRandomSeededColorFromString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            input = "bingles"; // Return transparent color for empty or null input
        }
        // Use a deterministic hash function (FNV-1a) to generate a stable seed. 
        // TODO: Maybe another day once we have time for testing.
        // int seed = GetDeterministicHashCode(input);
        int seed = input.GetHashCode();
        System.Random random = new(seed);

        // Generate random RGB values
        byte r = (byte)random.Next(0, 256);
        byte g = (byte)random.Next(0, 256);
        byte b = (byte)random.Next(0, 256);

        return new Color(r, g, b, 255); // A is set to 255 (opaque)
    }

    /// <summary>
    /// FNV-1a hash algorithm for deterministic, stable hashing across runs.
    /// </summary>
    private static int GetDeterministicHashCode(string str)
    {
        unchecked
        {
            const int fnvPrime = 16777619;
            int hash = (int)2166136261;

            foreach (char c in str)
            {
                hash ^= c;
                hash *= fnvPrime;
            }

            return hash;
        }
    }

    /// <summary>
    /// The backbround is 44, 44, 46, 255
    /// If a color is darker than the background color, or just barely brighter than it, it will be adjusted, violently
    /// Color's vars are between 0 and 1, and must be clamped to that range.
    /// This can't go wrong in any way
    /// </summary>
    public static Color PreventColorFromBeingTooCloseToTheBackgroundColor(Color theColor)
    {
        // Background color components are in 0-1 range (44/255 ≈ 0.172, 46/255 ≈ 0.180)
        const float bgR = 44f / 255f;
        const float bgG = 44f / 255f;
        const float bgB = 46f / 255f;
        
        // Calculate the brightness using standard luminance formula
        var brightness = theColor.R * 0.299f + theColor.G * 0.587f + theColor.B * 0.114f;
        var backgroundBrightness = bgR * 0.299f + bgG * 0.587f + bgB * 0.114f;
        
        const float threshold = 0.15f;
        
        // If the color brightness is too close to the background, adjust it
        if (brightness >= backgroundBrightness - threshold && brightness <= backgroundBrightness + threshold)
        {
            // If darker than background, brighten it significantly
            if (brightness < backgroundBrightness)
            {
                theColor.R = Math.Clamp(theColor.R + 0.4f, 0f, 1f);
                theColor.G = Math.Clamp(theColor.G + 0.4f, 0f, 1f);
                theColor.B = Math.Clamp(theColor.B + 0.4f, 0f, 1f);
            }
            else
            {
                // If similar or slightly brighter, brighten it more to ensure contrast
                theColor.R = Math.Clamp(theColor.R + 0.3f, 0f, 1f);
                theColor.G = Math.Clamp(theColor.G + 0.3f, 0f, 1f);
                theColor.B = Math.Clamp(theColor.B + 0.3f, 0f, 1f);
            }
        }
        
        return theColor;
    }

}
