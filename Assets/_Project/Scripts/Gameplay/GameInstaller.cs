using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    /// The composition root: wires Core services, the ScriptableObject configs and the scene's views into one
    /// <see cref="GameStateMachine"/> with explicit <c>new</c>. No DI container, no singletons, no FindObjectOfType.
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

        // Held past Awake so OnDestroy can dispose them: kill persistent tweens and unwire every += made here.
        private MilestonePreviewPresenter _milestonePreviewPresenter;
        private WheelPresenter _wheelPresenter;
        private ActionBarPresenter _actionBarPresenter;
        private PopupPresenter _popupPresenter;
        private ScreenPresentation _presentation;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DebugPresenter _debugPresenter;
#endif

        private readonly List<AsyncOperationHandle> _configLoads = new List<AsyncOperationHandle>();

        #region Composition
        private void Awake()
        {
            // Sized for the busiest moment (tick punches plus a bomb shake) so no spin pays for a resize.
            DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.ErrorsOnly)
                   .SetCapacity(tweenersCapacity: 120, sequencesCapacity: 40);

            var catalog = LoadConfig<RewardCatalog>("Configs/Settings/RewardCatalog");
            var spinConfig = LoadConfig<WheelSpinConfig>("Configs/Settings/WheelSpin_Default");
            var progression = LoadConfig<ZoneProgressionConfig>("Configs/Settings/ZoneProgression_Default");
            var continueConfig = LoadConfig<ContinueConfig>("Configs/Settings/Continue_Default");
            var juice = LoadConfig<JuiceConfig>("Configs/Settings/Juice_Default");

            IZoneClassifier classifier = progression.CreateClassifier();
            // The factory gets its own RNG so wedge dealing and slot resolution cannot perturb each other.
            var wheelFactory = new ZoneWheelFactory(
                classifier, progression, progression.Scaling, new UnityRandomProvider());
            var spinService = new SpinService(new WeightedSliceResolver(new UnityRandomProvider()));
            RewardId goldRewardId = catalog.GoldCurrency;
            RewardId cashRewardId = catalog.CashCurrency;
            var wallet = new Wallet(new PlayerPrefsSaveService());
            var continueService = new ContinueService(wallet, goldRewardId, continueConfig.ToSettings());
            var runModel = new RunModel(classifier, wallet, goldRewardId, cashRewardId, catalog.CurrencyIds);

            var audioLibrary = LoadConfig<AudioLibrary>("Configs/Settings/AudioLibrary");
            IAudioService audioService = new AudioService(transform);
            AudioHub.Initialize(audioService, audioLibrary);
            var audioPresenter = new AudioPresenter(audioService, audioLibrary);

            _bombPopup.Configure(juice);
            _collectPopup.Configure(juice);
            _milestonePopup.Configure(juice);
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
            if (_debugOverlay)
            {
                _debugPresenter = new DebugPresenter(runModel, machine, wallet, goldRewardId, catalog, bankPresenter);
                _debugPresenter.WireInput(_debugOverlay);
            }
#endif

            GameFlow.Start(machine);
        }

        // Synchronous so Awake stays a plain method; a missing address fails here, by name.
        private T LoadConfig<T>(string address) where T : Object
        {
            AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
            T config = handle.WaitForCompletion();
            _configLoads.Add(handle);

            if (!config)
                throw new System.InvalidOperationException(
                    $"[Vertigo] Addressable config '{address}' did not load ({handle.Status}); check the Addressables groups.");

            return config;
        }
        #endregion

        #region Teardown
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

            // Released last, once nothing that was built from these configs is still being torn down.
            for (int i = 0; i < _configLoads.Count; i++)
                if (_configLoads[i].IsValid()) Addressables.Release(_configLoads[i]);
            _configLoads.Clear();
        }
        #endregion
    }
}
