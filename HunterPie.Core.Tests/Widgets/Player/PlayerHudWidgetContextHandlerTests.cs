using HunterPie.Core.Client.Configuration.Overlay;
using HunterPie.Core.Game;
using HunterPie.Core.Game.Entity.Game;
using HunterPie.Core.Game.Entity.Party;
using HunterPie.Core.Game.Entity.Player;
using HunterPie.Core.Game.Entity.Player.Classes;
using HunterPie.Core.Game.Entity.Player.Vitals;
using HunterPie.Core.Game.Enums;
using HunterPie.Core.Game.Events;
using HunterPie.Core.Game.Services;
using HunterPie.UI.Overlay.Widgets.Player;
using HunterPie.UI.Overlay.Widgets.Player.ViewModels;
using Moq;
using NUnit.Framework;
using System;

namespace HunterPie.Core.Tests.Widgets.Player;

[TestFixture]
public class PlayerHudWidgetContextHandlerTests
{
    private Mock<IContext> _mockContext = null!;
    private Mock<IGame> _mockGame = null!;
    private Mock<IPlayer> _mockPlayer = null!;
    private Mock<IHealthComponent> _mockHealth = null!;
    private Mock<IStaminaComponent> _mockStamina = null!;
    private Mock<IWeapon> _mockWeapon = null!;
    private Mock<IAbnormalityCategorizationService> _mockCategorizationService = null!;
    private Mock<IParty> _mockParty = null!;
    private PlayerHudViewModel _viewModel = null!;
    private PlayerHudWidgetContextHandler _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHealth = new Mock<IHealthComponent>();
        _mockHealth.Setup(h => h.Current).Returns(100.0);
        _mockHealth.Setup(h => h.Max).Returns(150.0);
        _mockHealth.Setup(h => h.MaxPossibleHealth).Returns(200.0);
        _mockHealth.Setup(h => h.RecoverableHealth).Returns(10.0);
        _mockHealth.Setup(h => h.Heal).Returns(0.0);

        _mockStamina = new Mock<IStaminaComponent>();
        _mockStamina.Setup(s => s.Current).Returns(50.0);
        _mockStamina.Setup(s => s.Max).Returns(80.0);
        _mockStamina.Setup(s => s.MaxPossibleStamina).Returns(100.0);
        _mockStamina.Setup(s => s.MaxRecoverableStamina).Returns(75.0);

        _mockWeapon = new Mock<IWeapon>();
        _mockWeapon.Setup(w => w.Id).Returns(Weapon.Bow);

        _mockParty = new Mock<IParty>();

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Health).Returns(_mockHealth.Object);
        _mockPlayer.Setup(p => p.Stamina).Returns(_mockStamina.Object);
        _mockPlayer.Setup(p => p.Weapon).Returns(_mockWeapon.Object);
        _mockPlayer.Setup(p => p.Party).Returns(_mockParty.Object);
        _mockPlayer.Setup(p => p.Name).Returns("Hunter");
        _mockPlayer.Setup(p => p.MasterRank).Returns(999);
        _mockPlayer.Setup(p => p.HighRank).Returns(500);
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(false);

        _mockCategorizationService = new Mock<IAbnormalityCategorizationService>();

        _mockGame = new Mock<IGame>();
        _mockGame.Setup(g => g.Player).Returns(_mockPlayer.Object);
        _mockGame.Setup(g => g.AbnormalityCategorizationService).Returns(_mockCategorizationService.Object);

        _mockContext = new Mock<IContext>();
        _mockContext.Setup(c => c.Game).Returns(_mockGame.Object);

        _viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _sut = new PlayerHudWidgetContextHandler(_mockContext.Object, _viewModel);
    }

    // --- Constructor / UpdateData ---

    [Test]
    public void Constructor_SetsHealthPropertiesFromPlayer()
    {
        Assert.That(_viewModel.Health, Is.EqualTo(100.0).Within(0.001));
        Assert.That(_viewModel.MaxHealth, Is.EqualTo(150.0).Within(0.001));
        Assert.That(_viewModel.MaxExtendableHealth, Is.EqualTo(200.0).Within(0.001));
        Assert.That(_viewModel.RecoverableHealth, Is.EqualTo(10.0).Within(0.001));
    }

    [Test]
    public void Constructor_SetsStaminaPropertiesFromPlayer()
    {
        Assert.That(_viewModel.Stamina, Is.EqualTo(50.0).Within(0.001));
        Assert.That(_viewModel.MaxStamina, Is.EqualTo(80.0).Within(0.001));
        Assert.That(_viewModel.MaxPossibleStamina, Is.EqualTo(100.0).Within(0.001));
        Assert.That(_viewModel.MaxRecoverableStamina, Is.EqualTo(75.0).Within(0.001));
    }

    [Test]
    public void Constructor_SetsNameLevelAndWeaponFromPlayer()
    {
        Assert.That(_viewModel.Name, Is.EqualTo("Hunter"));
        Assert.That(_viewModel.Level, Is.EqualTo(999));
        Assert.That(_viewModel.Weapon, Is.EqualTo(Weapon.Bow));
    }

    [Test]
    public void Constructor_WithMeleeWeapon_SetsSharpnessViewModelFromWeapon()
    {
        var weaponMock = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 180, level: Sharpness.Blue);
        _mockPlayer.Setup(p => p.Weapon).Returns(weaponMock.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _ = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);

        Assert.That(viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(150));  // 200 - 50
        Assert.That(viewModel.SharpnessViewModel.Sharpness, Is.EqualTo(130));     // 180 - 50
        Assert.That(viewModel.SharpnessViewModel.SharpnessLevel, Is.EqualTo(Sharpness.Blue));
    }

    [Test]
    public void Constructor_WithNonMeleeWeapon_DoesNotSetSharpnessViewModel()
    {
        Assert.That(_viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(0));
        Assert.That(_viewModel.SharpnessViewModel.Sharpness, Is.EqualTo(0));
    }

    // --- OnPlayerLogin ---

    [Test]
    public void OnPlayerLogin_WhenRaised_UpdatesNameAndLevel()
    {
        _mockPlayer.Setup(p => p.Name).Returns("NewHunter");
        _mockPlayer.Setup(p => p.MasterRank).Returns(1500);

        _mockPlayer.Raise(p => p.OnLogin += null, EventArgs.Empty);

        Assert.That(_viewModel.Name, Is.EqualTo("NewHunter"));
        Assert.That(_viewModel.Level, Is.EqualTo(1500));
    }

    // --- OnPlayerLevelChange ---

    [Test]
    public void OnPlayerLevelChange_WhenRaised_UpdatesLevelFromPlayer()
    {
        _mockPlayer.Setup(p => p.MasterRank).Returns(1337);
        var args = new LevelChangeEventArgs(_mockPlayer.Object);

        _mockPlayer.Raise(p => p.OnLevelChange += null, args);

        Assert.That(_viewModel.Level, Is.EqualTo(1337));
    }

    // --- OnStageChange ---

    [Test]
    public void OnStageChange_WhenPlayerInHuntingZone_SetsInHuntingZoneTrue()
    {
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(true);

        _mockPlayer.Raise(p => p.OnStageUpdate += null, EventArgs.Empty);

        Assert.That(_viewModel.InHuntingZone, Is.True);
    }

    [Test]
    public void OnStageChange_WhenPlayerNotInHuntingZone_SetsInHuntingZoneFalse()
    {
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(false);
        _viewModel.InHuntingZone = true; // set to true first so we can detect the change

        _mockPlayer.Raise(p => p.OnStageUpdate += null, EventArgs.Empty);

        Assert.That(_viewModel.InHuntingZone, Is.False);
    }

    // --- OnPlayerHealthChange ---

    [Test]
    public void OnPlayerHealthChange_WhenRaised_UpdatesAllHealthProperties()
    {
        var healthForEvent = new Mock<IHealthComponent>();
        healthForEvent.Setup(h => h.Current).Returns(75.0);
        healthForEvent.Setup(h => h.Max).Returns(210.0);
        healthForEvent.Setup(h => h.MaxPossibleHealth).Returns(250.0);
        healthForEvent.Setup(h => h.RecoverableHealth).Returns(5.0);
        healthForEvent.Setup(h => h.Heal).Returns(0.0);
        var args = new HealthChangeEventArgs(healthForEvent.Object);

        _mockHealth.Raise(h => h.OnHealthChange += null, (object?)null, args);

        Assert.That(_viewModel.Health, Is.EqualTo(75.0).Within(0.001));
        Assert.That(_viewModel.MaxHealth, Is.EqualTo(210.0).Within(0.001));
        Assert.That(_viewModel.MaxExtendableHealth, Is.EqualTo(250.0).Within(0.001));
        Assert.That(_viewModel.RecoverableHealth, Is.EqualTo(5.0).Within(0.001));
    }

    // --- OnPlayerStaminaChange ---

    [Test]
    public void OnPlayerStaminaChange_WhenRaised_UpdatesAllStaminaProperties()
    {
        var staminaForEvent = new Mock<IStaminaComponent>();
        staminaForEvent.Setup(s => s.Current).Returns(60.0);
        staminaForEvent.Setup(s => s.Max).Returns(120.0);
        staminaForEvent.Setup(s => s.MaxPossibleStamina).Returns(150.0);
        staminaForEvent.Setup(s => s.MaxRecoverableStamina).Returns(110.0);
        var args = new StaminaChangeEventArgs(staminaForEvent.Object);

        _mockStamina.Raise(s => s.OnStaminaChange += null, args);

        Assert.That(_viewModel.Stamina, Is.EqualTo(60.0).Within(0.001));
        Assert.That(_viewModel.MaxStamina, Is.EqualTo(120.0).Within(0.001));
        Assert.That(_viewModel.MaxPossibleStamina, Is.EqualTo(150.0).Within(0.001));
        Assert.That(_viewModel.MaxRecoverableStamina, Is.EqualTo(110.0).Within(0.001));
    }

    // --- OnHeal ---

    [Test]
    public void OnHeal_WhenRaised_UpdatesHealProperty()
    {
        var healthForEvent = new Mock<IHealthComponent>();
        healthForEvent.Setup(h => h.Heal).Returns(30.0);
        healthForEvent.Setup(h => h.Current).Returns(0.0);
        healthForEvent.Setup(h => h.Max).Returns(0.0);
        healthForEvent.Setup(h => h.MaxPossibleHealth).Returns(0.0);
        healthForEvent.Setup(h => h.RecoverableHealth).Returns(0.0);
        var args = new HealthChangeEventArgs(healthForEvent.Object);

        _mockHealth.Raise(h => h.OnHeal += null, (object?)null, args);

        Assert.That(_viewModel.Heal, Is.EqualTo(30.0).Within(0.001));
    }

    // --- OnPlayerWeaponChange ---

    [Test]
    public void OnPlayerWeaponChange_BothNonMelee_SetsNewWeaponId()
    {
        var oldWeapon = new Mock<IWeapon>();
        oldWeapon.Setup(w => w.Id).Returns(Weapon.Bow);
        var newWeapon = new Mock<IWeapon>();
        newWeapon.Setup(w => w.Id).Returns(Weapon.LightBowgun);
        var args = new WeaponChangeEventArgs(oldWeapon.Object, newWeapon.Object);

        _mockPlayer.Raise(p => p.OnWeaponChange += null, args);

        Assert.That(_viewModel.Weapon, Is.EqualTo(Weapon.LightBowgun));
    }

    [Test]
    public void OnPlayerWeaponChange_OldMeleeNewNonMelee_UnsubscribesSharpnessAndSetsWeapon()
    {
        var weaponMock = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 150, level: Sharpness.Green);
        _mockPlayer.Setup(p => p.Weapon).Returns(weaponMock.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _ = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);

        var newNonMelee = new Mock<IWeapon>();
        newNonMelee.Setup(w => w.Id).Returns(Weapon.HeavyBowgun);
        _mockPlayer.Raise(p => p.OnWeaponChange += null, new WeaponChangeEventArgs(weaponMock.Object, newNonMelee.Object));

        Assert.That(viewModel.Weapon, Is.EqualTo(Weapon.HeavyBowgun));

        // Sharpness event after unsubscription should not affect ViewModel
        var sharpArgs = MakeSharpnessArgs(maxSharpness: 999, threshold: 0, currentSharpness: 999, level: Sharpness.Purple);
        weaponMock.As<IMeleeWeapon>().Raise(m => m.OnSharpnessChange += null, sharpArgs);

        Assert.That(viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(150)); // 200-50, unchanged
    }

    [Test]
    public void OnPlayerWeaponChange_OldNonMeleeNewMelee_SubscribesNewSharpnessAndSetsWeapon()
    {
        var newMeleeWeapon = BuildMeleeWeaponMock(maxSharpness: 300, threshold: 100, currentSharpness: 250, level: Sharpness.White);
        var oldNonMelee = new Mock<IWeapon>();
        oldNonMelee.Setup(w => w.Id).Returns(Weapon.Bow);

        _mockPlayer.Raise(p => p.OnWeaponChange += null, new WeaponChangeEventArgs(oldNonMelee.Object, newMeleeWeapon.Object));

        Assert.That(_viewModel.Weapon, Is.EqualTo(Weapon.Longsword));

        // Sharpness event on new weapon should update ViewModel
        var sharpArgs = MakeSharpnessArgs(maxSharpness: 300, threshold: 100, currentSharpness: 250, level: Sharpness.White);
        newMeleeWeapon.As<IMeleeWeapon>().Raise(m => m.OnSharpnessChange += null, sharpArgs);

        Assert.That(_viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(200)); // 300-100
        Assert.That(_viewModel.SharpnessViewModel.Sharpness, Is.EqualTo(150));    // 250-100
    }

    [Test]
    public void OnPlayerWeaponChange_BothMelee_ResubscribesToNewWeapon()
    {
        var oldMelee = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 150, level: Sharpness.Green);
        _mockPlayer.Setup(p => p.Weapon).Returns(oldMelee.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _ = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);

        var newMelee = BuildMeleeWeaponMock(maxSharpness: 400, threshold: 100, currentSharpness: 380, level: Sharpness.Purple);
        _mockPlayer.Raise(p => p.OnWeaponChange += null, new WeaponChangeEventArgs(oldMelee.Object, newMelee.Object));

        var sharpArgs = MakeSharpnessArgs(maxSharpness: 400, threshold: 100, currentSharpness: 350, level: Sharpness.Purple);
        newMelee.As<IMeleeWeapon>().Raise(m => m.OnSharpnessChange += null, sharpArgs);

        Assert.That(viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(300)); // 400-100
        Assert.That(viewModel.SharpnessViewModel.Sharpness, Is.EqualTo(250));    // 350-100
    }

    // --- OnSharpnessChange ---

    [Test]
    public void OnSharpnessChange_WithMeleeWeapon_UpdatesMaxSharpnessAndCurrent()
    {
        var weaponMock = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 170, level: Sharpness.Green);
        _mockPlayer.Setup(p => p.Weapon).Returns(weaponMock.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _ = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);

        var sharpArgs = MakeSharpnessArgs(maxSharpness: 210, threshold: 50, currentSharpness: 190, level: Sharpness.Green);
        weaponMock.As<IMeleeWeapon>().Raise(m => m.OnSharpnessChange += null, sharpArgs);

        Assert.That(viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(160)); // 210-50
        Assert.That(viewModel.SharpnessViewModel.Sharpness, Is.EqualTo(140));    // 190-50
    }

    [Test]
    public void OnSharpnessChange_DoesNotUpdateSharpnessLevel()
    {
        var weaponMock = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 170, level: Sharpness.Blue);
        _mockPlayer.Setup(p => p.Weapon).Returns(weaponMock.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _ = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);
        var levelBefore = viewModel.SharpnessViewModel.SharpnessLevel;

        var sharpArgs = MakeSharpnessArgs(maxSharpness: 210, threshold: 50, currentSharpness: 190, level: Sharpness.White);
        weaponMock.As<IMeleeWeapon>().Raise(m => m.OnSharpnessChange += null, sharpArgs);

        // OnSharpnessChange does NOT update SharpnessLevel — only OnSharpnessLevelChange does
        Assert.That(viewModel.SharpnessViewModel.SharpnessLevel, Is.EqualTo(levelBefore));
    }

    // --- OnSharpnessLevelChange ---

    [Test]
    public void OnSharpnessLevelChange_WithMeleeWeapon_UpdatesLevelMaxAndCurrent()
    {
        var weaponMock = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 170, level: Sharpness.Blue);
        _mockPlayer.Setup(p => p.Weapon).Returns(weaponMock.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        _ = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);

        var sharpArgs = MakeSharpnessArgs(maxSharpness: 300, threshold: 100, currentSharpness: 260, level: Sharpness.White);
        weaponMock.As<IMeleeWeapon>().Raise(m => m.OnSharpnessLevelChange += null, sharpArgs);

        Assert.That(viewModel.SharpnessViewModel.SharpnessLevel, Is.EqualTo(Sharpness.White));
        Assert.That(viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(200)); // 300-100
        Assert.That(viewModel.SharpnessViewModel.Sharpness, Is.EqualTo(160));    // 260-100
    }

    // --- UnhookEvents ---

    [Test]
    public void UnhookEvents_AfterCall_LoginEventDoesNotUpdateViewModel()
    {
        _sut.UnhookEvents();
        _mockPlayer.Setup(p => p.Name).Returns("ShouldNotAppear");
        _mockPlayer.Setup(p => p.MasterRank).Returns(9999);

        _mockPlayer.Raise(p => p.OnLogin += null, EventArgs.Empty);

        Assert.That(_viewModel.Name, Is.EqualTo("Hunter"));   // unchanged
        Assert.That(_viewModel.Level, Is.EqualTo(999));        // unchanged
    }

    [Test]
    public void UnhookEvents_AfterCall_HealthChangeDoesNotUpdateViewModel()
    {
        _sut.UnhookEvents();
        var healthForEvent = new Mock<IHealthComponent>();
        healthForEvent.Setup(h => h.Current).Returns(999.0);
        healthForEvent.Setup(h => h.Max).Returns(999.0);
        healthForEvent.Setup(h => h.MaxPossibleHealth).Returns(999.0);
        healthForEvent.Setup(h => h.RecoverableHealth).Returns(999.0);
        healthForEvent.Setup(h => h.Heal).Returns(0.0);

        _mockHealth.Raise(h => h.OnHealthChange += null, new HealthChangeEventArgs(healthForEvent.Object));

        Assert.That(_viewModel.Health, Is.EqualTo(100.0).Within(0.001)); // unchanged
    }

    [Test]
    public void UnhookEvents_WithMeleeWeapon_UnsubscribesSharpnessEvents()
    {
        var weaponMock = BuildMeleeWeaponMock(maxSharpness: 200, threshold: 50, currentSharpness: 150, level: Sharpness.Green);
        _mockPlayer.Setup(p => p.Weapon).Returns(weaponMock.Object);

        var viewModel = new PlayerHudViewModel(new PlayerHudWidgetConfig());
        var handler = new PlayerHudWidgetContextHandler(_mockContext.Object, viewModel);
        handler.UnhookEvents();

        var sharpArgs = MakeSharpnessArgs(maxSharpness: 999, threshold: 0, currentSharpness: 999, level: Sharpness.Purple);
        weaponMock.As<IMeleeWeapon>().Raise(m => m.OnSharpnessChange += null, sharpArgs);

        Assert.That(viewModel.SharpnessViewModel.MaxSharpness, Is.EqualTo(150)); // 200-50, unchanged
    }

    // --- OnPlayerAbnormalityStart ---

    [Test]
    public void OnPlayerAbnormalityStart_WithNoneCategory_DoesNotAddToActiveAbnormalities()
    {
        var abnormality = new Mock<IAbnormality>();
        _mockCategorizationService.Setup(s => s.Categorize(abnormality.Object)).Returns(AbnormalityCategory.None);

        _mockPlayer.Raise(p => p.OnAbnormalityStart += null, (object?)null, abnormality.Object);

        Assert.That(_viewModel.ActiveAbnormalities.Count, Is.EqualTo(0));
    }

    [Test]
    public void OnPlayerAbnormalityStart_WithValidCategory_AddsToActiveAbnormalities()
    {
        var abnormality = new Mock<IAbnormality>();
        _mockCategorizationService.Setup(s => s.Categorize(abnormality.Object)).Returns(AbnormalityCategory.Fire);

        _mockPlayer.Raise(p => p.OnAbnormalityStart += null, (object?)null, abnormality.Object);

        Assert.That(_viewModel.ActiveAbnormalities.Contains(AbnormalityCategory.Fire), Is.True);
    }

    // --- OnPlayerAbnormalityEnd ---

    [Test]
    public void OnPlayerAbnormalityEnd_WithNoneCategory_LeavesActiveAbnormalitiesUnchanged()
    {
        _viewModel.ActiveAbnormalities.Add(AbnormalityCategory.Poison);
        var abnormality = new Mock<IAbnormality>();
        _mockCategorizationService.Setup(s => s.Categorize(abnormality.Object)).Returns(AbnormalityCategory.None);

        _mockPlayer.Raise(p => p.OnAbnormalityEnd += null, (object?)null, abnormality.Object);

        Assert.That(_viewModel.ActiveAbnormalities.Contains(AbnormalityCategory.Poison), Is.True);
    }

    [Test]
    public void OnPlayerAbnormalityEnd_WhenCategoryNotPresent_DoesNotRemoveOtherEntries()
    {
        _viewModel.ActiveAbnormalities.Add(AbnormalityCategory.Poison);
        var abnormality = new Mock<IAbnormality>();
        _mockCategorizationService.Setup(s => s.Categorize(abnormality.Object)).Returns(AbnormalityCategory.Fire);

        _mockPlayer.Raise(p => p.OnAbnormalityEnd += null, (object?)null, abnormality.Object);

        Assert.That(_viewModel.ActiveAbnormalities.Contains(AbnormalityCategory.Poison), Is.True);
    }

    [Test]
    public void OnPlayerAbnormalityEnd_WhenCategoryPresent_RemovesFromActiveAbnormalities()
    {
        _viewModel.ActiveAbnormalities.Add(AbnormalityCategory.Fire);
        var abnormality = new Mock<IAbnormality>();
        _mockCategorizationService.Setup(s => s.Categorize(abnormality.Object)).Returns(AbnormalityCategory.Fire);

        _mockPlayer.Raise(p => p.OnAbnormalityEnd += null, (object?)null, abnormality.Object);

        Assert.That(_viewModel.ActiveAbnormalities.Contains(AbnormalityCategory.Fire), Is.False);
    }

    // --- Private helpers ---

    /// <summary>
    /// Creates a mock weapon that implements both IWeapon and IMeleeWeapon.
    /// The _mockPlayer.Weapon is NOT updated here; callers must set it up if needed.
    /// </summary>
    private static Mock<IWeapon> BuildMeleeWeaponMock(int maxSharpness, int threshold, int currentSharpness, Sharpness level)
    {
        var weaponMock = new Mock<IWeapon>();
        weaponMock.Setup(w => w.Id).Returns(Weapon.Longsword);
        weaponMock.As<IMeleeWeapon>().Setup(m => m.MaxSharpness).Returns(maxSharpness);
        weaponMock.As<IMeleeWeapon>().Setup(m => m.Threshold).Returns(threshold);
        weaponMock.As<IMeleeWeapon>().Setup(m => m.CurrentSharpness).Returns(currentSharpness);
        weaponMock.As<IMeleeWeapon>().Setup(m => m.Sharpness).Returns(level);
        weaponMock.As<IMeleeWeapon>().Setup(m => m.SharpnessThresholds).Returns(Array.Empty<int>());
        return weaponMock;
    }

    private static SharpnessEventArgs MakeSharpnessArgs(int maxSharpness, int threshold, int currentSharpness, Sharpness level)
    {
        var weapon = new Mock<IMeleeWeapon>();
        weapon.Setup(m => m.MaxSharpness).Returns(maxSharpness);
        weapon.Setup(m => m.Threshold).Returns(threshold);
        weapon.Setup(m => m.CurrentSharpness).Returns(currentSharpness);
        weapon.Setup(m => m.Sharpness).Returns(level);
        weapon.Setup(m => m.SharpnessThresholds).Returns(Array.Empty<int>());
        return new SharpnessEventArgs(weapon.Object);
    }
}
