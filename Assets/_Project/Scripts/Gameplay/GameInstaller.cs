using DG.Tweening;
using UnityEngine;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Core.States;
using Vertigo.Wheel.Core.Zones;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.Data.Services;
using Vertigo.Wheel.Gameplay.Presenters;
using Vertigo.Wheel.UI.Views;
using Vertigo.Wheel.UI.Views.Popups;

namespace Vertigo.Wheel.Gameplay
{
    /// <summary>
    /// The composition root: wires Core services, the authored ScriptableObject configs, and the scene's
    /// Views into one running <see cref="GameStateMachine"/> with explicit <c>new</c> — no DI container, no
    /// singletons, no <c>FindObjectOfType</c>.
    /// <para>
    /// Every field below is populated once, by <c>MainSceneBuilder</c> via <see cref="Configure"/> at
    /// scene-build time — the same "never drag a reference by hand" rule every <see cref="UIViewBase"/>
    /// follows through its own name-based auto-wiring.
    /// </para>
    /// </summary>
    public sealed class GameInstaller : MonoBehaviour
    {
        [SerializeField] private HeaderView _header;
        [SerializeField] private WheelView _wheel;
        [SerializeField] private ZoneMapView _zoneMap;
        [SerializeField] private BankView _bank;
        [SerializeField] private ActionBarView _actionBar;
        [SerializeField] private BombPopupView _bombPopup;
        [SerializeField] private CollectPopupView _collectPopup;
        [SerializeField] private GiveUpConfirmPopupView _giveUpPopup;
        [SerializeField] private MilestonePreviewPopupView _milestonePopup;
        [SerializeField] private VfxView _vfx;
        [SerializeField] private DebugOverlayView _debugOverlay;
        [SerializeField] private ZoneMapTileView _zoneMapTilePrefab;
        [SerializeField] private BankEntryView _bankEntryPrefab;
        [SerializeField] private Transform _flightLayer;
        [SerializeField] private Sprite _bombSlotIcon;

        /// <summary>
        /// Exposed for the one Play Mode smoke test that proves this composition root actually reaches
        /// <c>IdleState</c> in a real scene — nothing at runtime reads it. Everything else about the state
        /// machine's construction stays a local in <see cref="Awake"/>, same as before.
        /// </summary>
        public GameStateMachine Machine { get; private set; }

        // The header presenter has no per-zone collaborator now that the zone number moved to the zone
        // strip — it only mirrors the wallet onto the header. The composition root holds it so its
        // subscription lives exactly as long as the scene does.
        private HeaderPresenter _headerPresenter;

        // Held for the same reason as the header presenter: it owns the milestone badge -> preview-popup
        // subscription and must outlive this method.
        private MilestonePreviewPresenter _milestonePreviewPresenter;

        /// <summary>Called once by the editor scene-build step; never touched by hand.</summary>
        public void Configure(
            HeaderView header, WheelView wheel, ZoneMapView zoneMap, BankView bank, ActionBarView actionBar,
            BombPopupView bombPopup, CollectPopupView collectPopup, GiveUpConfirmPopupView giveUpPopup,
            MilestonePreviewPopupView milestonePopup, VfxView vfx, DebugOverlayView debugOverlay,
            ZoneMapTileView zoneMapTilePrefab, BankEntryView bankEntryPrefab, Transform flightLayer,
            Sprite bombSlotIcon)
        {
            _header = header;
            _wheel = wheel;
            _zoneMap = zoneMap;
            _bank = bank;
            _actionBar = actionBar;
            _bombPopup = bombPopup;
            _collectPopup = collectPopup;
            _giveUpPopup = giveUpPopup;
            _milestonePopup = milestonePopup;
            _vfx = vfx;
            _debugOverlay = debugOverlay;
            _zoneMapTilePrefab = zoneMapTilePrefab;
            _bankEntryPrefab = bankEntryPrefab;
            _flightLayer = flightLayer;
            _bombSlotIcon = bombSlotIcon;
        }

        private void Awake()
        {
            // Sized against the busiest moment (a spin's tick punches plus a bomb's shake) so the first
            // real spin never pays for a capacity resize on the frame the player is watching.
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly)
                   .SetCapacity(tweenersCapacity: 120, sequencesCapacity: 40);

            var catalog = Resources.Load<RewardCatalog>("Configs/Settings/RewardCatalog");
            var spinConfig = Resources.Load<WheelSpinConfig>("Configs/Settings/WheelSpin_Default");
            var progression = Resources.Load<ZoneProgressionConfig>("Configs/Settings/ZoneProgression_Default");
            var continueConfig = Resources.Load<ContinueConfig>("Configs/Settings/Continue_Default");
            var bronzeTheme = Resources.Load<WheelThemeConfig>("Configs/Themes/Theme_Bronze");
            var silverTheme = Resources.Load<WheelThemeConfig>("Configs/Themes/Theme_Silver");
            var goldenTheme = Resources.Load<WheelThemeConfig>("Configs/Themes/Theme_Golden");

            IZoneClassifier classifier = progression.CreateClassifier();
            // The wheel factory gets its own RNG so a zone's slices are dealt onto different wedges each
            // time; the resolver's RNG stays separate so the two concerns can't perturb each other.
            var wheelFactory = new ZoneWheelFactory(
                classifier, progression, progression.Scaling, new UnityRandomProvider());
            var spinService = new SpinService(new WeightedSliceResolver(new UnityRandomProvider()));
            var wallet = new GoldWallet(new PlayerPrefsSaveService());
            var continueService = new ContinueService(wallet, continueConfig.ToSettings());
            var runModel = new RunModel(classifier, wallet, new RewardId("Reward_Gold"));

            var audioLibrary = Resources.Load<AudioLibrary>("Configs/Settings/AudioLibrary");
            IAudioService audioService = new AudioService(new PlayerPrefsSaveService(), transform);
            AudioHub.Initialize(audioService, audioLibrary);
            var audioPresenter = new AudioPresenter(audioService, audioLibrary);

            _headerPresenter = new HeaderPresenter(_header, wallet);

            // Skipped when the scene predates the milestone popup — a rebuild adds it.
            if (_milestonePopup != null)
                _milestonePreviewPresenter = new MilestonePreviewPresenter(_zoneMap, _milestonePopup);
            var wheelPresenter = new WheelPresenter(_wheel, spinConfig, catalog, _bombSlotIcon, audioService);
            var zoneMapPresenter = new ZoneMapPresenter(_zoneMap, _zoneMapTilePrefab, classifier);
            var bankPresenter = new BankPresenter(
                _bank, _bankEntryPrefab, catalog, runModel.Bank, _flightLayer, audioPresenter);
            var actionBarPresenter = new ActionBarPresenter(_actionBar);
            var popupPresenter = new PopupPresenter(
                _bombPopup, _collectPopup, _giveUpPopup, _bankEntryPrefab, catalog, audioPresenter);
            var vfxPresenter = new VfxPresenter(_vfx);

            var presentation = new ScreenPresentation(
                _header, wheelPresenter, zoneMapPresenter, bankPresenter, actionBarPresenter, popupPresenter,
                vfxPresenter, audioPresenter, bronzeTheme, silverTheme, goldenTheme);

            var context = new GameContext(runModel, wheelFactory, spinService, continueService, presentation);
            GameStateMachine machine = GameFlow.Build(context);
            Machine = machine;

            wheelPresenter.WireInput(machine);
            actionBarPresenter.WireInput(machine);
            popupPresenter.WireInput(machine);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_debugOverlay != null)
                new DebugPresenter(runModel, machine, wallet, catalog, bankPresenter).WireInput(_debugOverlay);
#endif

            GameFlow.Start(machine);
        }
    }
}
