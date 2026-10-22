using MudBlazor;

namespace KubaPC.SharedClient.Components.Theme
{

    public static class KubaTheme
    {
        private static readonly string[] FONT_FAMILY = { "Geist", "system-ui", "-apple-system", "Segoe UI", "sans-serif" };

        public static readonly MudTheme Theme = new()
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#272251",
                Secondary = "#FFD618",
                Tertiary = "#B8B4D6",
                Background = "#F6F5F2",
                Surface = "#FFFFFF",
                AppbarBackground = "#F6F5F2",
                AppbarText = "#1C1A2E",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#1C1A2E",
                TextPrimary = "#1C1A2E",
                TextSecondary = "#5B5876",
                TextDisabled = "#9896AD",
                Success = "#187A4F",
                Warning = "#8A5200",
                Error = "#C2372B",
                Info = "#2F5BD9",
                LinesDefault = "rgba(39,34,81,0.08)",
                LinesInputs = "rgba(39,34,81,0.16)",
                Divider = "rgba(39,34,81,0.08)",
                TableLines = "rgba(39,34,81,0.08)",
                TableHover = "#FBFBF9",
                ActionDefault = "#5B5876",
                OverlayLight = "rgba(39,34,81,0.32)"
            },
            Typography = new Typography
            {
                Default = new DefaultTypography { FontFamily = FONT_FAMILY, FontSize = ".875rem", LineHeight = "1.5" },
                Button = new ButtonTypography { FontFamily = FONT_FAMILY, FontWeight = "500", TextTransform = "none", LetterSpacing = "0" },
                H4 = new H4Typography { FontFamily = FONT_FAMILY, FontSize = "1.5rem", FontWeight = "600", LetterSpacing = "-0.01em" },
                H6 = new H6Typography { FontFamily = FONT_FAMILY, FontSize = "1.125rem", FontWeight = "600" },
                Subtitle2 = new Subtitle2Typography { FontFamily = FONT_FAMILY, FontSize = ".75rem", FontWeight = "500" }
            },
            LayoutProperties = new LayoutProperties
            {
                DefaultBorderRadius = "12px",
                DrawerWidthLeft = "240px",
                DrawerMiniWidthLeft = "72px",
                DrawerWidthRight = "480px",
                AppbarHeight = "64px"
            }
        };
    }
}
