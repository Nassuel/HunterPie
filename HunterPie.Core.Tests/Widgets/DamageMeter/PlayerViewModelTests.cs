using HunterPie.Core.Client.Configuration.Overlay;
using HunterPie.Core.Game.Enums;
using HunterPie.UI.Overlay.Widgets.Damage.ViewModels;
using NUnit.Framework;

namespace HunterPie.Core.Tests.Widgets.DamageMeter;

[TestFixture]
public class PlayerViewModelTests
{
    private DamageMeterWidgetConfig _config = null!;
    private PlayerViewModel _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _config = new DamageMeterWidgetConfig();
        _sut = new PlayerViewModel(_config);
    }

    // --- Construction ---

    [Test]
    public void Constructor_ConfigIsAssigned()
    {
        Assert.That(_sut.Config, Is.SameAs(_config));
    }

    [Test]
    public void Constructor_DamageDefaultsToZero()
    {
        Assert.That(_sut.Damage, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_DpsDefaultsToZero()
    {
        Assert.That(_sut.DPS, Is.EqualTo(0.0).Within(0.001));
    }

    [Test]
    public void Constructor_IsUserDefaultsToFalse()
    {
        Assert.That(_sut.IsUser, Is.False);
    }

    [Test]
    public void Constructor_IsIncreasingDefaultsToFalse()
    {
        Assert.That(_sut.IsIncreasing, Is.False);
    }

    [Test]
    public void Constructor_IsVisibleDefaultsToFalse()
    {
        Assert.That(_sut.IsVisible, Is.False);
    }

    [Test]
    public void Constructor_AffinityDefaultsToNull()
    {
        Assert.That(_sut.Affinity, Is.Null);
    }

    [Test]
    public void Constructor_RawDamageDefaultsToNull()
    {
        Assert.That(_sut.RawDamage, Is.Null);
    }

    [Test]
    public void Constructor_ElementalDamageDefaultsToNull()
    {
        Assert.That(_sut.ElementalDamage, Is.Null);
    }

    // --- Property round-trips ---

    [Test]
    public void Name_WhenSet_ReturnsSetValue()
    {
        _sut.Name = "Nassuel";

        Assert.That(_sut.Name, Is.EqualTo("Nassuel"));
    }

    [Test]
    public void Damage_WhenSet_ReturnsSetValue()
    {
        _sut.Damage = 4200;

        Assert.That(_sut.Damage, Is.EqualTo(4200));
    }

    [Test]
    public void DPS_WhenSet_ReturnsSetValue()
    {
        _sut.DPS = 99.5;

        Assert.That(_sut.DPS, Is.EqualTo(99.5).Within(0.001));
    }

    [Test]
    public void IsUser_WhenSetToTrue_ReturnsTrue()
    {
        _sut.IsUser = true;

        Assert.That(_sut.IsUser, Is.True);
    }

    [Test]
    public void IsVisible_WhenSetToTrue_ReturnsTrue()
    {
        _sut.IsVisible = true;

        Assert.That(_sut.IsVisible, Is.True);
    }

    [Test]
    public void Weapon_WhenSet_ReturnsSetValue()
    {
        _sut.Weapon = Weapon.Longsword;

        Assert.That(_sut.Weapon, Is.EqualTo(Weapon.Longsword));
    }

    [Test]
    public void MasterRank_WhenSet_ReturnsSetValue()
    {
        _sut.MasterRank = 999;

        Assert.That(_sut.MasterRank, Is.EqualTo(999));
    }

    // --- Nullable properties ---

    [Test]
    public void Affinity_WhenSet_ReturnsSetValue()
    {
        _sut.Affinity = 0.75;

        Assert.That(_sut.Affinity, Is.EqualTo(0.75).Within(0.001));
    }

    [Test]
    public void Affinity_WhenSetBackToNull_IsNull()
    {
        _sut.Affinity = 0.5;
        _sut.Affinity = null;

        Assert.That(_sut.Affinity, Is.Null);
    }

    [Test]
    public void RawDamage_WhenSet_ReturnsSetValue()
    {
        _sut.RawDamage = 1234.5;

        Assert.That(_sut.RawDamage, Is.EqualTo(1234.5).Within(0.001));
    }

    [Test]
    public void ElementalDamage_WhenSet_ReturnsSetValue()
    {
        _sut.ElementalDamage = 456.78;

        Assert.That(_sut.ElementalDamage, Is.EqualTo(456.78).Within(0.001));
    }

    // --- Boundary cases ---

    [Test]
    public void Damage_WhenSetToZero_IsZero()
    {
        _sut.Damage = 1000;
        _sut.Damage = 0;

        Assert.That(_sut.Damage, Is.EqualTo(0));
    }

    [Test]
    public void Damage_WhenSetToMaxInt_DoesNotThrow()
    {
        Assert.That(() => _sut.Damage = int.MaxValue, Throws.Nothing);
        Assert.That(_sut.Damage, Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void DPS_WhenSetToZero_IsZero()
    {
        _sut.DPS = 500.0;
        _sut.DPS = 0.0;

        Assert.That(_sut.DPS, Is.EqualTo(0.0).Within(0.001));
    }

    [Test]
    public void MasterRank_WhenSetToZero_IsZero()
    {
        _sut.MasterRank = 0;

        Assert.That(_sut.MasterRank, Is.EqualTo(0));
    }
}