using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace mosair.Services
{
    // The colour themes of Görünüm ▸ Tema. Each palette sets every colour token of App.axaml for the dark and the
    // light variant; the dark/light button stays as it is. The choice is the user's own (saved in
    // %APPDATA%\mosair\ui.json), never in a project file. Lapis = the colours written in App.axaml.
    public static class ThemeService
    {
        public sealed record Palette(string Id, string NameKey, Dictionary<string, string> Dark, Dictionary<string, string> Light);

        public const string DefaultId = "lapis";

        // Status colours mean the same in every palette (saved, warning, error, edit mode).
        private static readonly Dictionary<string, string> StatusDark = new()
        {
            ["FgWarn"] = "#E3A54F", ["Success"] = "#4CC27A", ["Danger"] = "#E53935", ["DangerText"] = "#F2665E", ["EditMode"] = "#FF7A29",
        };
        private static readonly Dictionary<string, string> StatusLight = new()
        {
            ["FgWarn"] = "#8A5106", ["Success"] = "#17703D", ["Danger"] = "#E53935", ["DangerText"] = "#B71C1C", ["EditMode"] = "#B23A0A",
        };

        // Token order: BgMain BgBar BgPanel BgInput BgCanvas BgCard BgHover BgPressed BrdrMain BrdrSec BrdrTer
        // FgPrimary FgSecondary FgMuted FgDisabled FgIcon FgMenu NavBg Brand BrandFill AccentFill AccentFillHover
        // AccentFillPressed OnAccent AccentText AccentSubtle AccentBorder.
        private static readonly string[] Keys =
        {
            "BgMain", "BgBar", "BgPanel", "BgInput", "BgCanvas", "BgCard", "BgHover", "BgPressed", "BrdrMain", "BrdrSec",
            "BrdrTer", "FgPrimary", "FgSecondary", "FgMuted", "FgDisabled", "FgIcon", "FgMenu", "NavBg", "Brand",
            "BrandFill", "AccentFill", "AccentFillHover", "AccentFillPressed", "OnAccent", "AccentText", "AccentSubtle",
            "AccentBorder",
        };

        private static Dictionary<string, string> Tokens(string values, bool dark)
        {
            var v = values.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (v.Length != Keys.Length) throw new ArgumentException("palette needs " + Keys.Length + " colours");
            var d = new Dictionary<string, string>(dark ? StatusDark : StatusLight);
            for (int i = 0; i < Keys.Length; i++) d[Keys[i]] = v[i];
            return d;
        }

        public static IReadOnlyList<Palette> Palettes { get; } = new[]
        {
            new Palette("lapis", "ThemeLapis",
                Tokens("#18191C #1D1E22 #222328 #16171A #2C2D31 #2A2B31 #33343B #3C3D45 #303137 #41424A #6B6C76 " +
                       "#ECECF0 #B4B5BE #9294A0 #62636C #B4B5BE #D6D7DD #CC18191C #6FAF6F #3F7A3F #2D6BD9 #3672DE " +
                       "#255DC0 #FFFFFF #6FA3FF #262D6BD9 #6FA3FF", true),
                Tokens("#F3F4F6 #E9EBEE #E3E5E9 #FFFFFF #E6E7EA #FBFBFC #D8DBE0 #CDD0D6 #D0D3D9 #BCC0C7 #7D8089 " +
                       "#1B1C20 #464953 #5C5F68 #8D9099 #464953 #2A2B31 #CCF3F4F6 #356B35 #3F7A3F #1F5FCC #2766D4 " +
                       "#1A50AD #FFFFFF #1D5BC4 #1F1F5FCC #1F5FCC", false)),
            new Palette("pastel", "ThemePastel",
                Tokens("#1C1E21 #212327 #25282C #191B1E #2A2C30 #2C2F34 #34383E #3D4148 #353840 #454950 #6E737B " +
                       "#E9ECEF #B9BFC6 #979DA5 #62676E #B9BFC6 #D8DCE0 #CC1C1E21 #C3B1E1 #6E5C93 #93BBAA #A0C6B6 " +
                       "#84AD9C #12201A #A9D3C2 #2693BBAA #A9D3C2", true),
                Tokens("#F5F4F1 #FCFBF9 #F9F8F5 #FFFFFF #EAE8E3 #FFFFFF #ECE9E3 #E1DED6 #E0DDD6 #CFCBC2 #8A867E " +
                       "#23262B #4D525A #6C717A #9EA2A8 #4D525A #2B2E33 #CCF5F4F1 #7D68A8 #6B5796 #4D7D6B #568874 " +
                       "#426C5C #FFFFFF #3F6E5C #1F4D7D6B #4D7D6B", false)),
            new Palette("grafit", "ThemeGraphite",
                Tokens("#141517 #18191B #1C1D20 #111214 #26272A #232428 #2C2E32 #35373C #2D2F33 #3D3F44 #6A6D74 " +
                       "#EDEEF0 #AEB1B7 #8B8F97 #5C5F66 #AEB1B7 #D5D7DB #CC141517 #4FC9B9 #0E7C71 #0E7C71 #11897D " +
                       "#0B6B61 #FFFFFF #4FC9B9 #264FC9B9 #4FC9B9", true),
                Tokens("#F4F5F6 #FFFFFF #FAFAFB #FFFFFF #E7E8EA #FFFFFF #ECEDEF #E0E2E5 #DCDEE1 #C6C9CE #7E828A " +
                       "#16181B #454951 #666B74 #9A9EA5 #454951 #202328 #CCF4F5F6 #0E7C71 #0E7C71 #0E7C71 #11897D " +
                       "#0B6B61 #FFFFFF #0B6B61 #1A0E7C71 #0E7C71", false)),
            new Palette("traverten", "ThemeTravertine",
                Tokens("#1B1A18 #201F1C #24221F #171614 #2A2825 #2C2A26 #35322D #3E3A34 #37342F #48443D #746E64 " +
                       "#EEEAE3 #BDB6AB #9B9488 #655F56 #BDB6AB #DCD7CF #CC1B1A18 #9DB08A #5C7048 #A9603C #B56A45 " +
                       "#944F30 #FFFFFF #E2A27C #26E2A27C #E2A27C", true),
                Tokens("#F4F1EB #FBF9F5 #F8F5F0 #FFFFFF #E7E2D9 #FFFFFF #EDE8DF #E2DCD1 #DED7CB #CBC3B5 #857D71 " +
                       "#24211D #524C44 #716A60 #A39C91 #524C44 #2C2924 #CCF4F1EB #5C7048 #5C7048 #9A5434 #A65D3C " +
                       "#85472B #FFFFFF #8A4A2D #1A9A5434 #9A5434", false)),
            new Palette("murekkep", "ThemeInk",
                Tokens("#15171D #1A1D23 #1E2129 #12141A #262A32 #252934 #2D3240 #363C4C #2F3441 #3F4556 #6B7289 " +
                       "#E8EAF2 #B0B5C7 #8E94A8 #5D6375 #B0B5C7 #D3D7E4 #CC15171D #F1B98A #B86A2E #8592F2 #929EF5 " +
                       "#7381E6 #11152B #A7B1FA #268592F2 #A7B1FA", true),
                Tokens("#F3F4F8 #FFFFFF #F9FAFC #FFFFFF #E6E8EF #FFFFFF #EBEDF3 #DFE2EA #DADDE7 #C4C8D5 #7C8296 " +
                       "#191C26 #474C5E #666C80 #9CA1B1 #474C5E #22252F #CCF3F4F8 #B86A2E #B86A2E #4F5FD1 #5A6AD9 " +
                       "#4352C0 #FFFFFF #4352C0 #1A4F5FD1 #4F5FD1", false)),
        };

        public static string CurrentId { get; private set; } = DefaultId;
        public static bool IsLight { get; private set; }

        public static event Action? Changed;

        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "mosair", "ui.json");

        private sealed class Saved
        {
            public string? Theme { get; set; }
            public bool Light { get; set; }
        }

        // At start-up: the saved palette and dark/light (Lapis, dark when nothing is saved or the file is broken).
        public static void LoadSaved()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var s = JsonSerializer.Deserialize<Saved>(File.ReadAllText(SettingsPath));
                    if (s != null)
                    {
                        IsLight = s.Light;
                        if (Palettes.Any(p => p.Id == s.Theme)) CurrentId = s.Theme!;
                    }
                }
            }
            catch { /* a broken settings file just means the defaults */ }
            Apply(CurrentId, IsLight, save: false);
        }

        public static void SetPalette(string id) => Apply(id, IsLight, save: true);
        public static void SetLight(bool light) => Apply(CurrentId, light, save: true);

        private static void Apply(string id, bool light, bool save)
        {
            var app = Application.Current;
            if (app == null) return;
            var palette = Palettes.FirstOrDefault(p => p.Id == id) ?? Palettes[0];
            CurrentId = palette.Id;
            IsLight = light;

            // Replacing the values in App.axaml's theme dictionaries updates every DynamicResource at once.
            Fill(app.Resources, ThemeVariant.Dark, palette.Dark);
            Fill(app.Resources, ThemeVariant.Light, palette.Light);
            var fluent = app.Styles.OfType<FluentTheme>().FirstOrDefault();
            if (fluent != null)
            {
                if (fluent.Palettes.TryGetValue(ThemeVariant.Dark, out var d)) d.Accent = Color.Parse(palette.Dark["AccentFill"]);
                if (fluent.Palettes.TryGetValue(ThemeVariant.Light, out var l)) l.Accent = Color.Parse(palette.Light["AccentFill"]);
            }
            app.RequestedThemeVariant = light ? ThemeVariant.Light : ThemeVariant.Dark;

            if (save)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
                    File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Saved { Theme = CurrentId, Light = light }));
                }
                catch { /* the theme still changes for this session */ }
            }
            Changed?.Invoke();
        }

        private static void Fill(IResourceDictionary resources, ThemeVariant variant, Dictionary<string, string> tokens)
        {
            if (!resources.ThemeDictionaries.TryGetValue(variant, out var provider) || provider is not IResourceDictionary dict) return;
            foreach (var (key, hex) in tokens) dict[key] = Color.Parse(hex);
        }
    }
}
