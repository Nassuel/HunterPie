using HunterPie.Core.Settings.Types;
using HunterPie.UI.Overlay.Widgets.Damage.ViewModels;
using NUnit.Framework;

namespace HunterPie.Core.Tests.Widgets.DamageMeter;

[TestFixture]
public class DamageBarViewModelTests
{
    // --- Construction ---

    [Test]
    public void Constructor_ColorIsAssigned()
    {
        var color = new Color("#FFA74FFF");
        var sut = new DamageBarViewModel(color);

        Assert.That(sut.Color, Is.EqualTo(color));
    }

    [Test]
    public void Constructor_PercentageDefaultsToZero()
    {
        var sut = new DamageBarViewModel(new Color("#FFA74FFF"));

        Assert.That(sut.Percentage, Is.EqualTo(0.0).Within(0.001));
    }

    // --- Property round-trips ---

    [Test]
    public void Percentage_WhenSet_ReturnsSetValue()
    {
        var sut = new DamageBarViewModel(new Color("#FFA74FFF"))
        {
            Percentage = 0.75
        };

        Assert.That(sut.Percentage, Is.EqualTo(0.75).Within(0.001));
    }

    [Test]
    public void Color_WhenChanged_ReturnsNewColor()
    {
        var original = new Color("#FFA74FFF");
        var updated = new Color("#FF50C5B7");
        var sut = new DamageBarViewModel(original)
        {
            Color = updated
        };

        Assert.That(sut.Color, Is.EqualTo(updated));
    }

    // --- Boundary cases ---

    [Test]
    public void Percentage_WhenSetToZero_IsZero()
    {
        var sut = new DamageBarViewModel(new Color("#FFA74FFF"))
        {
            Percentage = 0.5
        };
        sut.Percentage = 0.0;

        Assert.That(sut.Percentage, Is.EqualTo(0.0).Within(0.001));
    }

    [Test]
    public void Percentage_WhenSetToOne_IsOne()
    {
        var sut = new DamageBarViewModel(new Color("#FFA74FFF"))
        {
            Percentage = 1.0
        };

        Assert.That(sut.Percentage, Is.EqualTo(1.0).Within(0.001));
    }
}