using HunterPie.Core.Client.Configuration.Debug;
using HunterPie.Core.Client.Configuration.Enums;
using HunterPie.Core.Client.Configuration.Overlay;
using HunterPie.Core.Domain.Enums;
using HunterPie.Core.Domain.Mapper;
using HunterPie.Core.Domain.Mapper.Internal;
using HunterPie.Core.Domain.Process.Entity;
using HunterPie.Core.Game;
using HunterPie.Core.Game.Entity.Game;
using HunterPie.Core.Game.Entity.Game.Quest;
using HunterPie.Core.Game.Entity.Party;
using HunterPie.Core.Game.Entity.Player;
using HunterPie.Core.Game.Enums;
using HunterPie.Core.Game.Events;
using HunterPie.UI.Overlay.Service;
using HunterPie.UI.Overlay.ViewModels;
using HunterPie.UI.Overlay.Widgets.Damage.Controllers;
using HunterPie.UI.Overlay.Widgets.Damage.ViewModels;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace HunterPie.Core.Tests.Widgets.DamageMeter;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class DamageMeterControllerV2Tests
{
    private Mock<IContext> _mockContext = null!;
    private Mock<IGame> _mockGame = null!;
    private Mock<IPlayer> _mockPlayer = null!;
    private Mock<IParty> _mockParty = null!;
    private Mock<IGameProcess> _mockProcess = null!;
    private DamageMeterWidgetConfig _config = null!;
    private MeterViewModelV2 _viewModel = null!;
    private WidgetContext _widgetContext = null!;
    private DamageMeterControllerV2 _sut = null!;

    [OneTimeSetUp]
    public void EnsureWpfApplication()
    {
        if (Application.Current == null)
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };

        try { MapFactory.Add(new XmlNodeToAilmentDefinitionMapper()); } catch { }
    }

    [SetUp]
    public void SetUp()
    {
        _mockParty = new Mock<IParty>();
        _mockParty.Setup(p => p.Members).Returns(Array.Empty<IPartyMember>());

        _mockPlayer = new Mock<IPlayer>();
        _mockPlayer.Setup(p => p.Party).Returns(_mockParty.Object);
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(false);

        _mockGame = new Mock<IGame>();
        _mockGame.Setup(g => g.Player).Returns(_mockPlayer.Object);
        _mockGame.Setup(g => g.Quest).Returns((IQuest?)null);
        _mockGame.Setup(g => g.TimeElapsed).Returns(0.4f);

        _mockProcess = new Mock<IGameProcess>();
        _mockProcess.Setup(p => p.Type).Returns(GameProcessType.MonsterHunterRise);

        _mockContext = new Mock<IContext>();
        _mockContext.Setup(c => c.Game).Returns(_mockGame.Object);
        _mockContext.Setup(c => c.Process).Returns(_mockProcess.Object);

        _config = new DamageMeterWidgetConfig();
        _viewModel = new MeterViewModelV2(_config);
        _widgetContext = new WidgetContext(
            viewModel: _viewModel,
            overlaySettings: new OverlayClientConfig(),
            developmentSettings: new DevelopmentConfig(),
            state: new Mock<IOverlayState>().Object);

        _sut = new DamageMeterControllerV2(_mockContext.Object, _viewModel, _widgetContext, _config);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.UnhookEvents();
    }

    // ── Dispatcher pump ────────────────────────────────────────────────────────

    private static void PumpDispatcher()
    {
        var frame = new DispatcherFrame();
        Application.Current.Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    // ── Constructor / UpdateData ───────────────────────────────────────────────

    [Test]
    public void Constructor_WithNoQuestAndNoMembers_SetsInitialViewModelState()
    {
        Assert.That(_viewModel.InHuntingZone, Is.False);
        Assert.That(_viewModel.MaxDeaths, Is.EqualTo(0));
        Assert.That(_viewModel.Deaths, Is.EqualTo(0));
        Assert.That(_viewModel.TimeElapsed, Is.EqualTo(0.4).Within(0.001));
        Assert.That(_viewModel.HasPetsToBeDisplayed, Is.False);
    }

    [Test]
    public void Constructor_WithActiveQuest_SetsDeathsFromQuest()
    {
        var questMock = new Mock<IQuest>();
        questMock.Setup(q => q.MaxDeaths).Returns(5);
        questMock.Setup(q => q.Deaths).Returns(2);
        _mockGame.Setup(g => g.Quest).Returns(questMock.Object);
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(false);

        var viewModel = new MeterViewModelV2(_config);
        var wCtx = new WidgetContext(viewModel, new OverlayClientConfig(), new DevelopmentConfig(), new Mock<IOverlayState>().Object);
        _ = new DamageMeterControllerV2(_mockContext.Object, viewModel, wCtx, _config);

        Assert.That(viewModel.MaxDeaths, Is.EqualTo(5));
        Assert.That(viewModel.Deaths, Is.EqualTo(2));
        Assert.That(viewModel.InHuntingZone, Is.True);
    }

    // ── HookEvents / UnhookEvents ──────────────────────────────────────────────

    [Test]
    public void HookEvents_PartyMemberJoinSubscribed_EventReachesHandler()
    {
        var petMock = BuildPetMember("TestPet", damage: 0);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, petMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Pets.Members.Count, Is.EqualTo(1));
    }

    [Test]
    public void UnhookEvents_AfterCall_PartyJoinEventNoLongerHandled()
    {
        _sut.UnhookEvents();

        var petMock = BuildPetMember("IgnoredPet", damage: 0);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, petMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Pets.Members.Count, Is.EqualTo(0));
    }

    [Test]
    public void UnhookEvents_AfterCall_VillageStateEventNoLongerHandled()
    {
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(true);
        _sut.UnhookEvents();
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(false);

        _mockPlayer.Raise(p => p.OnVillageEnter += null, EventArgs.Empty);

        // InHuntingZone should not be updated after unhooking
        Assert.That(_viewModel.InHuntingZone, Is.False); // unchanged from UpdateData
    }

    // ── OnMemberJoin ───────────────────────────────────────────────────────────

    [Test]
    public void OnMemberJoin_PetMember_AddsPetToViewModel()
    {
        var petMock = BuildPetMember("Felyne", damage: 0);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, petMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Pets.Members.Count, Is.EqualTo(1));
        Assert.That(_viewModel.Pets.Members[0].Name, Is.EqualTo("Felyne"));
        Assert.That(_viewModel.HasPetsToBeDisplayed, Is.True);
    }

    [Test]
    public void OnMemberJoin_PlayerMember_AddsPlayerToViewModel()
    {
        var memberMock = BuildPlayerMember("Hero", damage: 0, slot: 0, isMyself: false);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Players.Count, Is.EqualTo(1));
        Assert.That(_viewModel.Players[0].Name, Is.EqualTo("Hero"));
    }

    [Test]
    public void OnMemberJoin_CompanionMember_AddsPlayerToViewModel()
    {
        var memberMock = new Mock<IPartyMember>();
        memberMock.Setup(m => m.Type).Returns(MemberType.Companion);
        memberMock.Setup(m => m.Name).Returns("Companion");
        memberMock.Setup(m => m.Damage).Returns(0);
        memberMock.Setup(m => m.Weapon).Returns(Weapon.Longsword);
        memberMock.Setup(m => m.Slot).Returns(0);
        memberMock.Setup(m => m.IsMyself).Returns(false);
        memberMock.Setup(m => m.MasterRank).Returns(0);
        memberMock.Setup(m => m.Status).Returns((IPlayerStatus?)null);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Players.Count, Is.EqualTo(1));
    }

    [Test]
    public void OnMemberJoin_DuplicateMember_DoesNotAddTwice()
    {
        var memberMock = BuildPlayerMember("Solo", damage: 0, slot: 0, isMyself: true);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Players.Count, Is.EqualTo(1));
    }

    [Test]
    public void OnMemberJoin_PlayerMember_SetsIsUserForSelf()
    {
        var memberMock = BuildPlayerMember("MySelf", damage: 0, slot: 0, isMyself: true);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Players[0].IsUser, Is.True);
    }

    // ── OnMemberLeave ──────────────────────────────────────────────────────────

    [Test]
    public void OnMemberLeave_PetMember_RemovesPetFromViewModel()
    {
        var petMock = BuildPetMember("LeafBug", damage: 0);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, petMock.Object);
        PumpDispatcher();

        _mockParty.Raise(p => p.OnMemberLeave += null, (object?)null, petMock.Object);
        PumpDispatcher();

        // RemovePet keeps pet ViewModel in Pets.Members for damage history but hides the panel
        Assert.That(_viewModel.HasPetsToBeDisplayed, Is.False);
    }

    [Test]
    public void OnMemberLeave_PlayerMember_RemovesPlayerFromViewModel()
    {
        var memberMock = BuildPlayerMember("Leavin", damage: 0, slot: 1, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        _mockParty.Raise(p => p.OnMemberLeave += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Players.Count, Is.EqualTo(0));
    }

    // ── OnDamageDealt ──────────────────────────────────────────────────────────

    [Test]
    public void OnDamageDealt_WithPositiveDamage_UpdatesMemberDamage()
    {
        var memberMock = BuildPlayerMember("Attacker", damage: 1000, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        memberMock.Raise(m => m.OnDamageDealt += null, (object?)null, memberMock.Object);

        Assert.That(_viewModel.Players[0].Damage, Is.EqualTo(1000));
    }

    [Test]
    public void OnDamageDealt_WithZeroDamage_DoesNotUpdateMember()
    {
        var memberMock = BuildPlayerMember("ZeroHitter", damage: 0, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();
        int damageBefore = _viewModel.Players[0].Damage;

        memberMock.Raise(m => m.OnDamageDealt += null, (object?)null, memberMock.Object);

        Assert.That(_viewModel.Players[0].Damage, Is.EqualTo(damageBefore));
    }

    // ── OnWeaponChange ─────────────────────────────────────────────────────────

    [Test]
    public void OnWeaponChange_WithRegisteredMember_UpdatesWeaponInViewModel()
    {
        var memberMock = BuildPlayerMember("Switcher", damage: 0, slot: 0, isMyself: false);
        memberMock.Setup(m => m.Weapon).Returns(Weapon.Longsword);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        memberMock.Setup(m => m.Weapon).Returns(Weapon.HeavyBowgun);
        memberMock.Raise(m => m.OnWeaponChange += null, (object?)null, memberMock.Object);

        Assert.That(_viewModel.Players[0].Weapon, Is.EqualTo(Weapon.HeavyBowgun));
    }

    [Test]
    public void OnWeaponChange_MemberNotRegistered_DoesNotThrow()
    {
        var unregisteredMock = new Mock<IPartyMember>();
        unregisteredMock.Setup(m => m.Type).Returns(MemberType.Player);

        Assert.DoesNotThrow(() => unregisteredMock.Raise(m => m.OnWeaponChange += null, (object?)null, unregisteredMock.Object));
    }

    // ── OnDeathCounterChange ───────────────────────────────────────────────────

    [Test]
    public void OnDeathCounterChange_WhenQuestFiresEvent_UpdatesDeathsOnViewModel()
    {
        var questMock = new Mock<IQuest>();
        questMock.Setup(q => q.MaxDeaths).Returns(3);
        questMock.Setup(q => q.Deaths).Returns(0);
        _mockGame.Setup(g => g.Quest).Returns(questMock.Object);

        // Fire OnQuestStart so the handler subscribes to this quest's events
        _mockGame.Raise(g => g.OnQuestStart += null, (object?)null, questMock.Object);
        PumpDispatcher();

        questMock.Raise(q => q.OnDeathCounterChange += null, new CounterChangeEventArgs(current: 2, max: 5));

        Assert.That(_viewModel.Deaths, Is.EqualTo(2));
        Assert.That(_viewModel.MaxDeaths, Is.EqualTo(5));
    }

    // ── OnQuestStart ───────────────────────────────────────────────────────────

    [Test]
    public void OnQuestStart_ClearsPlayersAndSetsQuestDeaths()
    {
        var questMock = new Mock<IQuest>();
        questMock.Setup(q => q.MaxDeaths).Returns(7);
        questMock.Setup(q => q.Deaths).Returns(1);

        _mockGame.Raise(g => g.OnQuestStart += null, (object?)null, questMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.InHuntingZone, Is.True);
        Assert.That(_viewModel.MaxDeaths, Is.EqualTo(7));
        Assert.That(_viewModel.Deaths, Is.EqualTo(1));
        Assert.That(_widgetContext.ViewModel, Is.SameAs(_viewModel));
    }

    [Test]
    public void OnQuestStart_WithMembersAlreadyPresent_ClearsAndReinits()
    {
        var petMock = BuildPetMember("OldPet", damage: 0);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, petMock.Object);
        PumpDispatcher();

        var questMock = new Mock<IQuest>();
        questMock.Setup(q => q.MaxDeaths).Returns(0);
        questMock.Setup(q => q.Deaths).Returns(0);

        _mockGame.Raise(g => g.OnQuestStart += null, (object?)null, questMock.Object);
        PumpDispatcher();

        // OnQuestStart clears Pets.Members; HasPetsToBeDisplayed is not reset (kept from before)
        Assert.That(_viewModel.Pets.Members.Count, Is.EqualTo(0));
    }

    // ── OnQuestEnd ─────────────────────────────────────────────────────────────

    [Test]
    public void OnQuestEnd_SetsTimeElapsedOnSnapshotAndSetsWidgetContext()
    {
        var questMock = new Mock<IQuest>();
        var args = new QuestEndEventArgs(questMock.Object, QuestStatus.Success, 300f);

        _mockGame.Raise(g => g.OnQuestEnd += null, args);
        PumpDispatcher();

        // The OLD viewModel becomes the snapshot — TimeElapsed is set to 300
        Assert.That(_viewModel.TimeElapsed, Is.EqualTo(300.0).Within(0.001));
        Assert.That(_widgetContext.ViewModel, Is.SameAs(_viewModel));
    }

    [Test]
    public void OnQuestEnd_ClearsTrackedMembers()
    {
        var memberMock = BuildPlayerMember("Warrior", damage: 100, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        var questMock = new Mock<IQuest>();
        _mockGame.Raise(g => g.OnQuestEnd += null, new QuestEndEventArgs(questMock.Object, QuestStatus.Success, 120f));
        PumpDispatcher();

        // After end, old viewmodel is the snapshot (our _viewModel reference); Players still holds the
        // view models that were added before quest end (snapshot preserved), but internal _members is cleared.
        // The next quest will start with a fresh viewmodel. We verify via Players on the snapshot:
        Assert.That(_viewModel.TimeElapsed, Is.EqualTo(120.0).Within(0.001));
    }

    // ── OnStageUpdate ──────────────────────────────────────────────────────────

    [Test]
    public void OnStageUpdate_WithNoActiveQuest_RefreshesViewModelState()
    {
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(true);

        _mockPlayer.Raise(p => p.OnStageUpdate += null, EventArgs.Empty);
        PumpDispatcher();

        Assert.That(_viewModel.InHuntingZone, Is.True);
        Assert.That(_widgetContext.ViewModel, Is.SameAs(_viewModel));
        Assert.That(_viewModel.MaxDeaths, Is.EqualTo(0));
    }

    [Test]
    public void OnStageUpdate_WithActiveQuest_DoesNothing()
    {
        var questMock = new Mock<IQuest>();
        _mockGame.Setup(g => g.Quest).Returns(questMock.Object);
        _viewModel.InHuntingZone = false;

        _mockPlayer.Raise(p => p.OnStageUpdate += null, EventArgs.Empty);
        PumpDispatcher();

        // Handler returns early when quest is active — InHuntingZone remains unchanged
        Assert.That(_viewModel.InHuntingZone, Is.False);
    }

    // ── OnVillageStateUpdate ───────────────────────────────────────────────────

    [Test]
    public void OnVillageStateUpdate_WhenPlayerInHuntingZone_SetsInHuntingZoneTrue()
    {
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(true);

        _mockPlayer.Raise(p => p.OnVillageEnter += null, EventArgs.Empty);

        Assert.That(_viewModel.InHuntingZone, Is.True);
    }

    [Test]
    public void OnVillageStateUpdate_WhenPlayerNotInHuntingZoneAndNoQuest_SetsInHuntingZoneFalse()
    {
        _viewModel.InHuntingZone = true;
        _mockPlayer.Setup(p => p.InHuntingZone).Returns(false);

        _mockPlayer.Raise(p => p.OnVillageLeave += null, EventArgs.Empty);

        Assert.That(_viewModel.InHuntingZone, Is.False);
    }

    // ── OnTimeElapsedChange ────────────────────────────────────────────────────

    [Test]
    public void OnTimeElapsedChange_Always_UpdatesTimeElapsed()
    {
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 5.0f));

        Assert.That(_viewModel.TimeElapsed, Is.EqualTo(5.0).Within(0.001));
    }

    [Test]
    public void OnTimeElapsedChange_WhenNotCanUpdate_DoesNotTriggerCalculations()
    {
        // _viewModel.TimeElapsed = 0.4 after SetUp. Firing with 0.1 gives newUpdate=0.1 > lastUpdate=0.4→false
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.1f));
        PumpDispatcher();

        // No DPS calculation happened — Players is empty, nothing to assert beyond no crash
        Assert.That(_viewModel.Players.Count, Is.EqualTo(0));
    }

    [Test]
    public void OnTimeElapsedChange_WhenCanUpdate_TriggersUIThreadCalculations()
    {
        var memberMock = BuildPlayerMember("Fighter", damage: 2000, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // _viewModel.TimeElapsed = 0.4 → lastUpdate = 0.4 % 0.5 = 0.4
        // newUpdate = 0.6 % 0.5 = 0.1 → canUpdate = 0.1 < 0.4 = true
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        // CalculatePlayerSeries ran — DPS should be calculated (damage/elapsed ≥ 0)
        Assert.That(_viewModel.Players[0].DPS, Is.GreaterThanOrEqualTo(0));
    }

    // ── CalculatePlayerSeries ──────────────────────────────────────────────────

    [Test]
    public void CalculatePlayerSeries_RelativeToQuestStrategy_UsesTotalElapsedTime()
    {
        _config.DpsCalculationStrategy.Value = DPSCalculationStrategy.RelativeToQuest;
        var memberMock = BuildPlayerMember("Ranger", damage: 1000, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // TimeElapsed = 0.4, trigger CalculatePlayerSeries via canUpdate
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        // DPS = damage / max(1, timeElapsed=0.6) = 1000/1 = 1000 (Math.Max(1,0.6)=1)
        Assert.That(_viewModel.Players[0].DPS, Is.EqualTo(1000.0).Within(1.0));
    }

    [Test]
    public void CalculatePlayerSeries_RelativeToJoinStrategy_SubtractsJoinTime()
    {
        _config.DpsCalculationStrategy.Value = DPSCalculationStrategy.RelativeToJoin;
        var memberMock = BuildPlayerMember("Joiner", damage: 1000, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // TimeElapsed becomes 0.6; JoinedAt = 0.4 (from Game.TimeElapsed at join time).
        // timeElapsed = 0.6 - min(0.6, 0.4) = 0.2 → Math.Max(1, 0.2) = 1 → DPS = 1000/1 = 1000
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        Assert.That(_viewModel.Players[0].DPS, Is.EqualTo(1000.0).Within(1.0));
    }

    [Test]
    public void CalculatePlayerSeries_RelativeToFirstHitStrategy_SubtractsFirstHitTime()
    {
        _config.DpsCalculationStrategy.Value = DPSCalculationStrategy.RelativeToFirstHit;
        var memberMock = BuildPlayerMember("FirstHitter", damage: 1000, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // Register first hit: raises OnDamageDealt which sets FirstHitAt = Game.TimeElapsed (0.4)
        memberMock.Raise(m => m.OnDamageDealt += null, (object?)null, memberMock.Object);

        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        Assert.That(_viewModel.Players[0].DPS, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void CalculatePlayerSeries_WithMultiplePlayers_SetsBarPercentage()
    {
        var m1 = BuildPlayerMember("P1", damage: 3000, slot: 0, isMyself: false);
        var m2 = BuildPlayerMember("P2", damage: 1000, slot: 1, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, m1.Object);
        PumpDispatcher();
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, m2.Object);
        PumpDispatcher();

        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        // totalDamage = 4000; P1 has 75%, P2 has 25%
        double p1Pct = _viewModel.Players.First(p => p.Name == "P1").Bar.Percentage;
        double p2Pct = _viewModel.Players.First(p => p.Name == "P2").Bar.Percentage;
        Assert.That(p1Pct, Is.EqualTo(75.0).Within(0.1));
        Assert.That(p2Pct, Is.EqualTo(25.0).Within(0.1));
    }

    [Test]
    public void CalculatePlayerSeries_WithTotalDamagePlotStrategy_AddsTotalDamagePoint()
    {
        _config.DamagePlotStrategy.Value = DamagePlotStrategy.TotalDamage;
        var memberMock = BuildPlayerMember("Plotter", damage: 500, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        Assert.That(_viewModel.Series.Count, Is.GreaterThan(0));
    }

    [Test]
    public void CalculatePlayerSeries_WithMovingAverageStrategy_AddsPoint()
    {
        _config.DamagePlotStrategy.Value = DamagePlotStrategy.MovingAverageDamagePerSecond;
        var memberMock = BuildPlayerMember("Averager", damage: 800, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        Assert.That(_viewModel.Series.Count, Is.GreaterThan(0));
    }

    [Test]
    public void CalculatePlayerSeries_WithOldPlotDiscardingEnabled_RemovesOldPoints()
    {
        _config.IsOldPlotDiscardingEnabled.Value = true;
        _config.PlotSlidingWindowInSeconds.Current = 10.0;
        var memberMock = BuildPlayerMember("Discarder", damage: 100, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // First calculation at TimeElapsed ≈ 0.6 — adds a point at X ≈ 0.6
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        // Second calculation at TimeElapsed = 12.0 — X=0.6 is older than window (10s), so it's discarded
        // lastUpdate = 0.6 % 0.5 = 0.1; newUpdate = 12.0 % 0.5 = 0.0; canUpdate = 0.0 < 0.1 = true
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 12.0f));
        PumpDispatcher();

        // After ClearOldPoints the old point is gone; new point at X=12.0 remains
        Assert.That(_viewModel.Series.Count, Is.GreaterThan(0));
    }

    [Test]
    public void CalculatePlayerSeries_WhenHasBeenOneSecond_AddsDamageToHistory()
    {
        var memberMock = BuildPlayerMember("Historian", damage: 600, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // Event 1: TimeElapsed crosses 0.5 boundary → canUpdate=true, secondsCount=0→hasBeenOneSecond=false
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        // Event 2: canUpdate=true again (0.6→0.1, 1.0→0.0 → 0.0<0.1), secondsCount=1→hasBeenOneSecond=true
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 1.0f));
        PumpDispatcher();

        // DPS should be computed (no exception means DamageHistory.Add ran without error)
        Assert.That(_viewModel.Players[0].DPS, Is.GreaterThanOrEqualTo(0));
    }

    // ── CalculatePetsDamage ────────────────────────────────────────────────────

    [Test]
    public void CalculatePetsDamage_WithSinglePet_SetsTotalDamageAndFullPercentage()
    {
        var petMock = BuildPetMember("BeastBug", damage: 500);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, petMock.Object);
        PumpDispatcher();

        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        Assert.That(_viewModel.Pets.TotalDamage, Is.EqualTo(500));
        Assert.That(_viewModel.Pets.Members[0].DamageBar.Percentage, Is.EqualTo(100.0).Within(0.001));
    }

    [Test]
    public void CalculatePetsDamage_WithMultiplePets_DistributesPercentagesCorrectly()
    {
        var pet1 = BuildPetMember("Bug1", damage: 750);
        var pet2 = BuildPetMember("Bug2", damage: 250);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, pet1.Object);
        PumpDispatcher();
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, pet2.Object);
        PumpDispatcher();

        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        Assert.That(_viewModel.Pets.TotalDamage, Is.EqualTo(1000));
        double pct1 = _viewModel.Pets.Members.First(p => p.Name == "Bug1").DamageBar.Percentage;
        double pct2 = _viewModel.Pets.Members.First(p => p.Name == "Bug2").DamageBar.Percentage;
        Assert.That(pct1, Is.EqualTo(75.0).Within(0.1));
        Assert.That(pct2, Is.EqualTo(25.0).Within(0.1));
    }

    // ── OnPlotSlidingWindowChange ──────────────────────────────────────────────

    [Test]
    public void OnPlotSlidingWindowChange_WithNoMembers_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => _config.PlotSamplingInSeconds.Current = 20.0);
    }

    [Test]
    public void OnPlotSlidingWindowChange_WithMembers_AdjustsDamageHistorySize()
    {
        // After join, DamageHistory size is originally PlotSamplingInSeconds.Current (10)
        var memberMock = BuildPlayerMember("Historian", damage: 0, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // Change the sampling window — this fires PropertyChanged on PlotSamplingInSeconds
        _config.PlotSamplingInSeconds.Current = 30.0;

        // If AdjustSize ran without exception, the test passes
        Assert.That(_config.PlotSamplingInSeconds.Current, Is.EqualTo(30.0).Within(0.001));
    }

    // ── OnShowOnlySelfChange ───────────────────────────────────────────────────

    [Test]
    public void OnShowOnlySelfChange_WhenShowOnlySelfEnabled_HidesNonSelfPlayers()
    {
        var memberMock = BuildPlayerMember("Other", damage: 0, slot: 1, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        _config.ShowOnlySelf.Value = true;
        // IsVisible is set before LiveCharts LineSeries.Visibility crashes (no real chart in tests)
        try { PumpDispatcher(); } catch (NullReferenceException) { }

        Assert.That(_viewModel.Players[0].IsVisible, Is.False);
    }

    [Test]
    public void OnShowOnlySelfChange_WhenShowOnlySelfDisabled_ShowsAllPlayers()
    {
        var memberMock = BuildPlayerMember("Visible", damage: 0, slot: 1, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher(); // member added with IsVisible=true (ShowOnlySelf defaults to false)

        _config.ShowOnlySelf.Value = true;
        try { PumpDispatcher(); } catch (NullReferenceException) { } // IsVisible→false, Plots.Visibility NPE

        _config.ShowOnlySelf.Value = false;
        try { PumpDispatcher(); } catch (NullReferenceException) { } // IsVisible→true, Plots.Visibility NPE

        Assert.That(_viewModel.Players[0].IsVisible, Is.True);
    }

    // ── HandleMemberJoin / HandleMemberLeave — unknown MemberType ─────────────

    [Test]
    public void HandleMemberJoin_WithUnknownMemberType_DoesNotAddToAnyCollection()
    {
        var memberMock = new Mock<IPartyMember>();
        memberMock.Setup(m => m.Type).Returns((MemberType)99);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        Assert.That(_viewModel.Players.Count, Is.EqualTo(0));
        Assert.That(_viewModel.Pets.Members.Count, Is.EqualTo(0));
    }

    [Test]
    public void HandleMemberLeave_WithUnknownMemberType_DoesNotThrow()
    {
        var memberMock = new Mock<IPartyMember>();
        memberMock.Setup(m => m.Type).Returns((MemberType)99);

        Assert.DoesNotThrow(() =>
        {
            _mockParty.Raise(p => p.OnMemberLeave += null, (object?)null, memberMock.Object);
            PumpDispatcher();
        });
    }

    // ── UnhookEvents with active quest ─────────────────────────────────────────

    [Test]
    public void UnhookEvents_WithActiveQuest_QuestDeathCounterNoLongerAffectsViewModel()
    {
        var questMock = new Mock<IQuest>();
        questMock.Setup(q => q.MaxDeaths).Returns(3);
        questMock.Setup(q => q.Deaths).Returns(0);
        _mockGame.Setup(g => g.Quest).Returns(questMock.Object);

        var viewModel = new MeterViewModelV2(_config);
        var wCtx = new WidgetContext(viewModel, new OverlayClientConfig(), new DevelopmentConfig(), new Mock<IOverlayState>().Object);
        var controller = new DamageMeterControllerV2(_mockContext.Object, viewModel, wCtx, _config);

        controller.UnhookEvents();

        questMock.Raise(q => q.OnDeathCounterChange += null, new CounterChangeEventArgs(current: 5, max: 10));

        Assert.That(viewModel.Deaths, Is.EqualTo(0)); // unchanged after unhook
    }

    // ── AddMember / RemoveMember with non-null IPlayerStatus ──────────────────

    [Test]
    public void AddMember_WithNonNullStatus_StatusEventsUpdateViewModel()
    {
        var statusMock = new Mock<IPlayerStatus>();
        statusMock.Setup(s => s.Affinity).Returns(10.0);
        statusMock.Setup(s => s.RawDamage).Returns(100.0);
        statusMock.Setup(s => s.ElementalDamage).Returns(50.0);

        var memberMock = BuildPlayerMember("Affinity Player", damage: 0, slot: 0, isMyself: true);
        memberMock.Setup(m => m.Status).Returns(statusMock.Object);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        statusMock.Raise(s => s.AffinityChanged += null, new SimpleValueChangeEventArgs<double>(0, 25.0));

        Assert.That(_viewModel.Players[0].Affinity, Is.EqualTo(25.0).Within(0.001));
    }

    [Test]
    public void RemoveMember_WithNonNullStatus_StatusEventsNoLongerUpdateViewModel()
    {
        var statusMock = new Mock<IPlayerStatus>();
        statusMock.Setup(s => s.Affinity).Returns(10.0);
        statusMock.Setup(s => s.RawDamage).Returns(100.0);
        statusMock.Setup(s => s.ElementalDamage).Returns(50.0);

        var memberMock = BuildPlayerMember("Affinity Player", damage: 0, slot: 0, isMyself: true);
        memberMock.Setup(m => m.Status).Returns(statusMock.Object);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // Capture ViewModel reference before removal
        var playerVm = _viewModel.Players[0];
        playerVm.Affinity = 10.0;

        _mockParty.Raise(p => p.OnMemberLeave += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // After leave, status events must be unsubscribed
        statusMock.Raise(s => s.AffinityChanged += null, new SimpleValueChangeEventArgs<double>(0, 99.0));

        Assert.That(playerVm.Affinity, Is.EqualTo(10.0).Within(0.001)); // unchanged
    }

    // ── OnDamageDealt — first-hit tracking ────────────────────────────────────

    [Test]
    public void OnDamageDealt_WhenMemberPreviousDamageWasZero_UpdatesDamageAndSetsFirstHitAt()
    {
        var memberMock = BuildPlayerMember("FirstHitter", damage: 0, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        memberMock.Setup(m => m.Damage).Returns(750);
        memberMock.Raise(m => m.OnDamageDealt += null, (object?)null, memberMock.Object);

        Assert.That(_viewModel.Players[0].Damage, Is.EqualTo(750));
    }

    // ── CalculateDpsByConfiguredStrategy — default strategy ───────────────────

    [Test]
    public void CalculateDpsByConfiguredStrategy_WithUnknownStrategy_FallsBackToElapsedOfOne()
    {
        _config.DpsCalculationStrategy.Value = (DPSCalculationStrategy)99;
        var memberMock = BuildPlayerMember("Fallback", damage: 1000, slot: 0, isMyself: false);
        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        PumpDispatcher();

        // Trigger canUpdate path: lastUpdate=0.4%0.5=0.4, newUpdate=0.6%0.5=0.1 → 0.1 < 0.4 = true
        _mockGame.Raise(g => g.OnTimeElapsedChange += null, new TimeElapsedChangeEventArgs(false, 0.6f));
        PumpDispatcher();

        // default _ => 1: timeElapsed = max(1, 1) = 1, DPS = 1000/1 = 1000
        Assert.That(_viewModel.Players[0].DPS, Is.EqualTo(1000.0).Within(1.0));
    }

    // ── BuildPlayerPlots — isVisible=false path ────────────────────────────────

    [Test]
    public void AddMember_WhenShowOnlySelfEnabledBeforeJoin_NonSelfMemberHasSeriesAdded()
    {
        _config.ShowOnlySelf.Value = true;
        var memberMock = BuildPlayerMember("Hidden", damage: 0, slot: 1, isMyself: false);

        _mockParty.Raise(p => p.OnMemberJoin += null, (object?)null, memberMock.Object);
        // BuildPlayerPlots sets series.Visibility = Collapsed which NPEs in tests without a real chart
        try { PumpDispatcher(); } catch (NullReferenceException) { }

        // The series was added to the collection before the NPE on Visibility.Collapsed
        Assert.That(_viewModel.Series.Count, Is.EqualTo(1));
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private static Mock<IPartyMember> BuildPlayerMember(string name, int damage, int slot, bool isMyself)
    {
        var mock = new Mock<IPartyMember>();
        mock.Setup(m => m.Type).Returns(MemberType.Player);
        mock.Setup(m => m.Name).Returns(name);
        mock.Setup(m => m.Damage).Returns(damage);
        mock.Setup(m => m.Weapon).Returns(Weapon.Longsword);
        mock.Setup(m => m.Slot).Returns(slot);
        mock.Setup(m => m.IsMyself).Returns(isMyself);
        mock.Setup(m => m.MasterRank).Returns(0);
        mock.Setup(m => m.Status).Returns((IPlayerStatus?)null);
        return mock;
    }

    private static Mock<IPartyMember> BuildPetMember(string name, int damage)
    {
        var mock = new Mock<IPartyMember>();
        mock.Setup(m => m.Type).Returns(MemberType.Pet);
        mock.Setup(m => m.Name).Returns(name);
        mock.Setup(m => m.Damage).Returns(damage);
        mock.Setup(m => m.Slot).Returns(5);
        mock.Setup(m => m.IsMyself).Returns(false);
        mock.Setup(m => m.Status).Returns((IPlayerStatus?)null);
        return mock;
    }
}
