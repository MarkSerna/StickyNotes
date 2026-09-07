using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using StickyNotes.Core.Enums;
using Windows.UI;

namespace StickyNotes.App.Helpers;

public record NotePalette(
    SolidColorBrush HeaderBrush,
    SolidColorBrush BodyBrush,
    SolidColorBrush BorderBrush,
    SolidColorBrush ForegroundBrush,
    Color BodyColor,
    Color HeaderColor,
    Color BorderColor,
    Color TextColor);

public static class ColorHelper
{
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 6)
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            return Color.FromArgb(255, r, g, b);
        }
        if (hex.Length == 8)
        {
            byte a = Convert.ToByte(hex.Substring(0, 2), 16);
            byte r = Convert.ToByte(hex.Substring(2, 2), 16);
            byte g = Convert.ToByte(hex.Substring(4, 2), 16);
            byte b = Convert.ToByte(hex.Substring(6, 2), 16);
            return Color.FromArgb(a, r, g, b);
        }
        return Colors.Yellow;
    }

    public static NotePalette CreatePalette(string bgHex, string headerHex, string borderHex, string textHex)
    {
        var bodyColor = FromHex(bgHex);
        var headerColor = FromHex(headerHex);
        var borderColor = FromHex(borderHex);
        var textColor = FromHex(textHex);

        return new NotePalette(
            HeaderBrush: new SolidColorBrush(headerColor),
            BodyBrush: new SolidColorBrush(bodyColor),
            BorderBrush: new SolidColorBrush(borderColor),
            ForegroundBrush: new SolidColorBrush(textColor),
            BodyColor: bodyColor,
            HeaderColor: headerColor,
            BorderColor: borderColor,
            TextColor: textColor);
    }

    public static NotePalette GetPalette(NoteColor color) => color switch
    {
        NoteColor.Yellow => CreatePalette("#FFF385", "#FEE75C", "#F6D83B", "#2D2817"),
        NoteColor.Green  => CreatePalette("#D2F8B8", "#BAF096", "#A2E278", "#1A3311"),
        NoteColor.Pink   => CreatePalette("#FFCEE8", "#FCAFD9", "#F389C3", "#3B1528"),
        NoteColor.Purple => CreatePalette("#E7DCFF", "#D6C3FF", "#BF9FFF", "#261543"),
        NoteColor.Blue   => CreatePalette("#CEECFE", "#AFDDFC", "#8CCBF7", "#0F2B40"),
        NoteColor.Gray   => CreatePalette("#E9ECEF", "#DEE2E6", "#CED4DA", "#212529"),
        _ => CreatePalette("#FFF385", "#FEE75C", "#F6D83B", "#2D2817")
    };
}
