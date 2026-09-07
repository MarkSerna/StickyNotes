using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using StickyNotes.Core.Enums;
using Windows.UI;

namespace StickyNotes.App.Helpers;

public record NotePalette(
    SolidColorBrush HeaderBrush,
    SolidColorBrush BodyBrush,
    SolidColorBrush BorderBrush,
    SolidColorBrush ForegroundBrush);

public static class ColorHelper
{
    public static NotePalette GetPalette(NoteColor color) => color switch
    {
        NoteColor.Yellow => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 255, 244, 117)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 255, 248, 153)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
            ForegroundBrush: new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))),

        NoteColor.Green => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 204, 255, 144)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 226, 255, 179)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
            ForegroundBrush: new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))),

        NoteColor.Pink => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 253, 207, 232)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 255, 223, 239)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
            ForegroundBrush: new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))),

        NoteColor.Purple => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 215, 174, 251)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 228, 199, 255)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
            ForegroundBrush: new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))),

        NoteColor.Blue => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 203, 240, 248)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 225, 247, 252)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)),
            ForegroundBrush: new SolidColorBrush(Color.FromArgb(255, 0, 0, 0))),

        NoteColor.Gray => new NotePalette(
            HeaderBrush: new SolidColorBrush(Color.FromArgb(255, 60, 64, 67)),
            BodyBrush:   new SolidColorBrush(Color.FromArgb(255, 40, 42, 45)),
            BorderBrush: new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)),
            ForegroundBrush: new SolidColorBrush(Color.FromArgb(255, 255, 255, 255))),

        _ => GetPalette(NoteColor.Yellow)
    };
}
