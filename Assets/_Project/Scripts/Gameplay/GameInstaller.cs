using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        [SerializeField] private WheelView _wheel;
        [SerializeField] private ZoneMapView _zoneMap;
        [SerializeField] private BankView _bank;
        [SerializeField] private ActionBarView _actionBar;
        [SerializeField] private BombPopupView _bombPopup;
        [SerializeField] private CollectPopupView _collectPopup;
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

        // Held past Awake so OnDestroy can dispose them — killing any persistent tween and unwiring every
        // += this composition root wired, rather than relying on the scene teardown to simply drop them.
        private MilestonePreviewPresenter _milestonePreviewPresenter;
        private WheelPresenter _wheelPresenter;
        private ActionBarPresenter _actionBarPresenter;
        private PopupPresenter _popupPresenter;
        private ScreenPresentation _presentation;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DebugPresenter _debugPresenter;
#endif

        /// <summary>Called once by the editor scene-build step; never touched by hand.</summary>
        public void Configure(
            WheelView wheel, ZoneMapView zoneMap, BankView bank, ActionBarView actionBar,
            BombPopupView bombPopup, CollectPopupView collectPopup,
            MilestonePreviewPopupView milestonePopup, VfxView vfx, DebugOverlayView debugOverlay,
            ZoneMapTileView zoneMapTilePrefab, BankEntryView bankEntryPrefab, Transform flightLayer,
            Sprite bombSlotIcon)
        {
            _wheel = wheel;
            _zoneMap = zoneMap;
            _bank = bank;
            _actionBar = actionBar;
            _bombPopup = bombPopup;
            _collectPopup = collectPopup;
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

            // .WaitForCompletion() keeps this synchronous like the Resources.Load it replaces, so Awake
            // stays a plain method and the Play Mode smoke test's boot-to-Idle budget is unaffected.
            var catalog = Addressables.LoadAssetAsync<RewardCatalog>("Configs/Settings/RewardCatalog").WaitForCompletion();
            var spinConfig = Addressables.LoadAssetAsync<WheelSpinConfig>("Configs/Settings/WheelSpin_Default").WaitForCompletion();
            var progression = Addressables.LoadAssetAsync<ZoneProgressionConfig>("Configs/Settings/ZoneProgression_Default").WaitForCompletion();
            var continueConfig = Addressables.LoadAssetAsync<ContinueConfig>("Configs/Settings/Continue_Default").WaitForCompletion();
            var juice = Addressables.LoadAssetAsync<JuiceConfig>("Configs/Settings/Juice_Default").WaitForCompletion();

            IZoneClassifier classifier = progression.CreateClassifier();
            // The wheel factory gets its own RNG so a zone's slices are dealt onto different wedges each
            // time; the resolver's RNG stays separate so the two concerns can't perturb each other.
            var wheelFactory = new ZoneWheelFactory(
                classifier, progression, progression.Scaling, new UnityRandomProvider());
            var spinService = new SpinService(new WeightedSliceResolver(new UnityRandomProvider()));
            var goldRewardId = new RewardId("Reward_Gold");
            var cashRewardId = new RewardId("Reward_Cash");
            var wallet = new Wallet(new PlayerPrefsSaveService());
            var continueService = new ContinueService(wallet, goldRewardId, continueConfig.ToSettings());
            var runModel = new RunModel(classifier, wallet, goldRewardId, cashRewardId);

            var audioLibrary = Addressables.LoadAssetAsync<AudioLibrary>("Configs/Settings/AudioLibrary").WaitForCompletion();
            IAudioService audioService = new AudioService(transform);
            AudioHub.Initialize(audioService, audioLibrary);
            var audioPresenter = new AudioPresenter(audioService, audioLibrary);

            // Skipped when the scene predates the milestone popup — a rebuild adds it.
            if (_milestonePopup != null)
                _milestonePreviewPresenter = new MilestonePreviewPresenter(_zoneMap, _milestonePopup);
            var wheelPresenter = new WheelPresenter(_wheel, spinConfig, catalog, _bombSlotIcon, audioService, juice);
            var zoneMapPresenter = new ZoneMapPresenter(_zoneMap, _zoneMapTilePrefab, classifier, juice);
            var bankPresenter = new BankPresenter(
                _bank, _bankEntryPrefab, catalog, runModel.Bank, _flightLayer, audioPresenter, juice);
            var actionBarPresenter = new ActionBarPresenter(_actionBar);
            var popupPresenter = new PopupPresenter(
                _bombPopup, _collectPopup, _bankEntryPrefab, catalog, audioPresenter);
            var vfxPresenter = new VfxPresenter(_vfx, juice);

            var presentation = new ScreenPresentation(
                wheelPresenter, zoneMapPresenter, bankPresenter, actionBarPresenter, popupPresenter,
                vfxPresenter, audioPresenter, progression, juice);

            var context = new GameContext(runModel, wheelFactory, spinService, continueService, presentation);
            GameStateMachine machine = GameFlow.Build(context);
            Machine = machine;

            wheelPresenter.WireInput(machine);
            actionBarPresenter.WireInput(machine);
            popupPresenter.WireInput(machine);

            _wheelPresenter = wheelPresenter;
            _actionBarPresenter = actionBarPresenter;
            _popupPresenter = popupPresenter;
            _presentation = presentation;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_debugOverlay != null)
            {
                _debugPresenter = new DebugPresenter(runModel, machine, wallet, goldRewardId, catalog, bankPresenter);
                _debugPresenter.WireInput(_debugOverlay);
            }
#endif

            GameFlow.Start(machine);
        }

        private void OnDestroy()
        {
            _milestonePreviewPresenter?.Dispose();
            _wheelPresenter?.Dispose();
            _actionBarPresenter?.Dispose();
            _popupPresenter?.Dispose();
            _presentation?.Dispose();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _debugPresenter?.Dispose();
#endif
        }
    }
}
