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
}
