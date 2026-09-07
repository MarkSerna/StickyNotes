using FluentAssertions;
using StickyNotes.Core.Enums;
using StickyNotes.Core.Models;
using Xunit;

namespace StickyNotes.Tests.Models;

public class NoteModelTests
{
    [Fact]
    public void DisplayTitle_WhenTitleIsPresent_ReturnsTrimmedTitle()
    {
        var note = new Note
        {
            Title = "  Mi Nota Importante  ",
            Content = "Primera línea de contenido"
        };

        note.DisplayTitle.Should().Be("Mi Nota Importante");
    }

    [Fact]
    public void DisplayTitle_WhenTitleIsEmpty_ReturnsFirstLineOfContent()
    {
        var note = new Note
        {
            Title = "",
            Content = "Comprar víveres en el supermercado\nSegunda línea de cosas"
        };

        note.DisplayTitle.Should().Be("Comprar víveres en el supermer...");
    }

    [Fact]
    public void DisplayTitle_WhenTitleAndContentAreEmpty_ReturnsDefaultPlaceholder()
    {
        var note = new Note
        {
            Title = "   ",
            Content = "   \n\r  "
        };

        note.DisplayTitle.Should().Be("Nota sin título");
    }

    [Fact]
    public void DisplayTitle_WhenContentFirstLineIsShort_ReturnsFullFirstLine()
    {
        var note = new Note
        {
            Title = null,
            Content = "Hola mundo\nSegunda línea"
        };

        note.DisplayTitle.Should().Be("Hola mundo");
    }

    [Fact]
    public void NoteDefaults_ShouldHaveExpectedGeometryAndStatus()
    {
        var note = new Note();

        note.PositionX.Should().Be(100.0);
        note.PositionY.Should().Be(100.0);
        note.Width.Should().Be(300.0);
        note.Height.Should().Be(260.0);
        note.Color.Should().Be(NoteColor.Yellow);
        note.IsAlwaysOnTop.Should().BeFalse();
        note.DeletedAt.Should().BeNull();
        note.SyncStatus.Should().Be(SyncStatus.PendingUpload);
    }

    [Theory]
    [InlineData(NoteColor.Yellow, "#FFF385")]
    [InlineData(NoteColor.Green, "#D2F8B8")]
    [InlineData(NoteColor.Pink, "#FFCEE8")]
    [InlineData(NoteColor.Purple, "#E7DCFF")]
    [InlineData(NoteColor.Blue, "#CEECFE")]
    [InlineData(NoteColor.Gray, "#E9ECEF")]
    public void ColorHex_ReturnsExpectedPastelHexCode(NoteColor color, string expectedHex)
    {
        var note = new Note { Color = color };
        note.ColorHex.Should().Be(expectedHex);
    }

    [Fact]
    public void PreviewText_WhenContentContainsRtf_StripsTagsAndReturnsCleanPlainText()
    {
        var rtf = @"{\rtf1\fbidis\ansi\ansicpg1252\deff0\nouicompat\deflang9226{\fonttbl{\f0\fnil Segoe UI;}}\fs20 ujghvohvouy\par}";
        var note = new Note { Content = rtf };

        note.PreviewText.Should().Be("ujghvohvouy");
    }

    [Fact]
    public void PropertyChanged_WhenTitleOrContentModified_RaisesEvent()
    {
        var note = new Note();
        var raisedProperties = new System.Collections.Generic.List<string?>();
        note.PropertyChanged += (s, e) => raisedProperties.Add(e.PropertyName);

        note.Title = "Nuevo Titulo";
        note.Content = "Nuevo Contenido";
        note.Color = NoteColor.Blue;

        raisedProperties.Should().Contain(nameof(Note.Title));
        raisedProperties.Should().Contain(nameof(Note.DisplayTitle));
        raisedProperties.Should().Contain(nameof(Note.Content));
        raisedProperties.Should().Contain(nameof(Note.PreviewText));
        raisedProperties.Should().Contain(nameof(Note.Color));
        raisedProperties.Should().Contain(nameof(Note.ColorHex));
    }
}
