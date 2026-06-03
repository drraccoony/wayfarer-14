using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Utility;

namespace Content.Client._Floof.Vore;

public sealed class CustomVorePresetState
{
    public List<CustomVorePreset> Presets { get; set; } = new();
    public int SelectedPresetIndex { get; set; }
}

public sealed class CustomVorePreset
{
    public string Nickname { get; set; } = string.Empty;
    public string CustomAttemptText { get; set; } = string.Empty;
    public string CustomEmote { get; set; } = string.Empty;
    public string CustomDigestionEmote { get; set; } = string.Empty;
    public bool PlayStomachSounds { get; set; }
    public bool AllowDigestion { get; set; }
}

public static class CustomVoreSettingsStore
{
    private const string FilePrefix = "/wayfarer_custom_vore_";

    public static CustomVorePresetState Load()
    {
        var resourceManager = IoCManager.Resolve<IResourceManager>();
        var path = GetPath();

        if (!resourceManager.UserData.TryReadAllText(path, out var raw) || string.IsNullOrWhiteSpace(raw))
            return CreateDefaultState();

        try
        {
            return Normalize(Deserialize(raw));
        }
        catch
        {
            return CreateDefaultState();
        }
    }

    public static void Save(CustomVorePresetState state)
    {
        var resourceManager = IoCManager.Resolve<IResourceManager>();
        var normalized = Normalize(state);

        using var writer = resourceManager.UserData.OpenWriteText(GetPath());
        writer.Write(Serialize(normalized));
    }

    public static string GetDisplayName(CustomVorePreset preset, int index)
    {
        var nickname = preset.Nickname.Trim();
        return string.IsNullOrWhiteSpace(nickname) ? $"Preset {index + 1}" : nickname;
    }

    private static CustomVorePresetState Normalize(CustomVorePresetState? state)
    {
        state ??= CreateDefaultState();

        if (state.Presets.Count == 0)
            state.Presets.Add(CreateDefaultPreset(1));

        for (var i = 0; i < state.Presets.Count; i++)
        {
            var preset = state.Presets[i];
            preset.Nickname = preset.Nickname.Trim();
            preset.CustomAttemptText = preset.CustomAttemptText.Trim();
            preset.CustomEmote = preset.CustomEmote.Trim();
            preset.CustomDigestionEmote = preset.CustomDigestionEmote.Trim();

            if (string.IsNullOrWhiteSpace(preset.Nickname))
                preset.Nickname = $"Preset {i + 1}";
        }

        if (state.SelectedPresetIndex < 0 || state.SelectedPresetIndex >= state.Presets.Count)
            state.SelectedPresetIndex = 0;

        return state;
    }

    private static CustomVorePresetState CreateDefaultState()
    {
        return new CustomVorePresetState
        {
            Presets = new List<CustomVorePreset> { CreateDefaultPreset(1) },
            SelectedPresetIndex = 0,
        };
    }

    private static CustomVorePreset CreateDefaultPreset(int index)
    {
        return new CustomVorePreset
        {
            Nickname = $"Preset {index}",
        };
    }

    private static ResPath GetPath()
    {
        var serverId = IoCManager.Resolve<IConfigurationManager>().GetCVar(CCVars.ServerId);
        if (string.IsNullOrWhiteSpace(serverId))
            serverId = "local";

        // Keep server IDs filename-safe without using blocked APIs in the content sandbox.
        var safeServerIdChars = new char[serverId.Length];
        for (var i = 0; i < serverId.Length; i++)
            safeServerIdChars[i] = IsSafeFileChar(serverId[i]) ? serverId[i] : '_';

        var safeServerId = new string(safeServerIdChars);
        return new ResPath($"{FilePrefix}{safeServerId}.json");
    }

    private static bool IsSafeFileChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_' || ch == '-';
    }

    private static string Serialize(CustomVorePresetState state)
    {
        var lines = new List<string>
        {
            "version\t1",
            $"selected\t{state.SelectedPresetIndex}",
            $"count\t{state.Presets.Count}",
        };

        foreach (var preset in state.Presets)
        {
            lines.Add(string.Join("\t",
                "preset",
                Escape(preset.Nickname),
                Escape(preset.CustomEmote),
                Escape(preset.CustomDigestionEmote),
                preset.PlayStomachSounds ? "1" : "0",
                preset.AllowDigestion ? "1" : "0",
                Escape(preset.CustomAttemptText)));
        }

        return string.Join("\n", lines);
    }

    private static CustomVorePresetState Deserialize(string raw)
    {
        var state = new CustomVorePresetState();
        var lines = raw.Replace("\r", string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            if (parts.Length == 0)
                continue;

            if (parts[0] == "selected" && parts.Length >= 2 && int.TryParse(parts[1], out var selected))
            {
                state.SelectedPresetIndex = selected;
                continue;
            }

            if (parts[0] != "preset" || parts.Length < 6)
                continue;

            state.Presets.Add(new CustomVorePreset
            {
                Nickname = Unescape(parts[1]),
                CustomEmote = Unescape(parts[2]),
                CustomDigestionEmote = Unescape(parts[3]),
                PlayStomachSounds = parts[4] == "1",
                AllowDigestion = parts[5] == "1",
                CustomAttemptText = parts.Length >= 7 ? Unescape(parts[6]) : string.Empty,
            });
        }

        return state;
    }

    private static string Escape(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("\t", "\\t")
            .Replace("\n", "\\n");
    }

    private static string Unescape(string text)
    {
        var chars = new List<char>(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\' || i + 1 >= text.Length)
            {
                chars.Add(text[i]);
                continue;
            }

            i++;
            chars.Add(text[i] switch
            {
                't' => '\t',
                'n' => '\n',
                '\\' => '\\',
                _ => text[i],
            });
        }

        return new string(chars.ToArray());
    }
}