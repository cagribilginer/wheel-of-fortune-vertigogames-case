using System;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// A collapsible cheat bar for manual testing. It only raises intent events; <c>DebugPresenter</c> owns what they
    /// do. It switches itself off outside the editor and development builds and starts hidden, toggled with
    /// <see cref="TOGGLE_KEY"/>.
    /// </summary>
    public sealed class DebugOverlayView : UIViewBase
    {
        // Tap-only landscape game — nothing else reads the keyboard, so a plain letter is safe here.
        private const KeyCode TOGGLE_KEY = KeyCode.D;

        [SerializeField] private Button _buttonDebugToggle;
        [SerializeField] private RectTransform _panelDebugBody;
        [SerializeField] private Button _buttonDebugZone5;
        [SerializeField] private Button _buttonDebugZone30;
        [SerializeField] private Button _buttonDebugBomb;
        [SerializeField] private Button _buttonDebugGold;
        [SerializeField] private Button _buttonDebugItems;

        public event Action JumpToZone5Clicked;
        public event Action JumpToZone30Clicked;
        public event Action TriggerBombClicked;
        public event Action GrantGoldClicked;
        public event Action GrantItemsClicked;

        private bool _isAvailable;
        private bool _isShown;
        private bool _isExpanded;

        #region Wiring
        protected override void CacheReferences()
        {
            Bind(ref _buttonDebugToggle, "ui_button_debug_toggle");
            Bind(ref _panelDebugBody, "ui_panel_debug_body");
            Bind(ref _buttonDebugZone5, "ui_button_debug_zone5");
            Bind(ref _buttonDebugZone30, "ui_button_debug_zone30");
            Bind(ref _buttonDebugBomb, "ui_button_debug_bomb");
            Bind(ref _buttonDebugGold, "ui_button_debug_gold");
            Bind(ref _buttonDebugItems, "ui_button_debug_items");
        }

        protected override void Awake()
        {
            base.Awake();

            _isAvailable = Application.isEditor || Debug.isDebugBuild;
            if (!_isAvailable)
            {
                gameObject.SetActive(false);
                return;
            }

            // Hidden until the hotkey asks for it, so a fresh recording is clean.
            SetShown(false);
        }

        private void Update()
        {
            if (_isAvailable && Input.GetKeyDown(TOGGLE_KEY))
                SetShown(!_isShown);
        }

        private void OnEnable()
        {
            _buttonDebugToggle.onClick.AddListener(ToggleBody);
            _buttonDebugZone5.onClick.AddListener(RaiseZone5);
            _buttonDebugZone30.onClick.AddListener(RaiseZone30);
            _buttonDebugBomb.onClick.AddListener(RaiseBomb);
            _buttonDebugGold.onClick.AddListener(RaiseGold);
            _buttonDebugItems.onClick.AddListener(RaiseItems);
        }

        private void OnDisable()
        {
            _buttonDebugToggle.onClick.RemoveListener(ToggleBody);
            _buttonDebugZone5.onClick.RemoveListener(RaiseZone5);
            _buttonDebugZone30.onClick.RemoveListener(RaiseZone30);
            _buttonDebugBomb.onClick.RemoveListener(RaiseBomb);
            _buttonDebugGold.onClick.RemoveListener(RaiseGold);
            _buttonDebugItems.onClick.RemoveListener(RaiseItems);
        }
        #endregion

        #region Visibility
        // Whole-overlay visibility, driven by the hotkey. The root stays active either way.
        private void SetShown(bool shown)
        {
            _isShown = shown;
            if (_buttonDebugToggle) _buttonDebugToggle.gameObject.SetActive(shown);
            if (!shown) SetExpanded(false);
        }

        private void ToggleBody()
        {
            SetExpanded(!_isExpanded);
        }

        private void SetExpanded(bool expanded)
        {
            _isExpanded = expanded;
            if (_panelDebugBody) _panelDebugBody.gameObject.SetActive(expanded);
        }
        #endregion

        #region Events
        private void RaiseZone5()
        {
            if (JumpToZone5Clicked != null) JumpToZone5Clicked();
        }
        private void RaiseZone30()
        {
            if (JumpToZone30Clicked != null) JumpToZone30Clicked();
        }
        private void RaiseBomb()
        {
            if (TriggerBombClicked != null) TriggerBombClicked();
        }
        private void RaiseGold()
        {
            if (GrantGoldClicked != null) GrantGoldClicked();
        }
        private void RaiseItems()
        {
            if (GrantItemsClicked != null) GrantItemsClicked();
        }
        #endregion
    }
}
