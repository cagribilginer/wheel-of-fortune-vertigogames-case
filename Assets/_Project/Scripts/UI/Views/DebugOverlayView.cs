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

        [SerializeField] private Button _ui_button_debug_toggle;
        [SerializeField] private RectTransform _ui_panel_debug_body;
        [SerializeField] private Button _ui_button_debug_zone5;
        [SerializeField] private Button _ui_button_debug_zone30;
        [SerializeField] private Button _ui_button_debug_bomb;
        [SerializeField] private Button _ui_button_debug_gold;
        [SerializeField] private Button _ui_button_debug_items;

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
            Bind(ref _ui_button_debug_toggle, "ui_button_debug_toggle");
            Bind(ref _ui_panel_debug_body, "ui_panel_debug_body");
            Bind(ref _ui_button_debug_zone5, "ui_button_debug_zone5");
            Bind(ref _ui_button_debug_zone30, "ui_button_debug_zone30");
            Bind(ref _ui_button_debug_bomb, "ui_button_debug_bomb");
            Bind(ref _ui_button_debug_gold, "ui_button_debug_gold");
            Bind(ref _ui_button_debug_items, "ui_button_debug_items");
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
            _ui_button_debug_toggle.onClick.AddListener(ToggleBody);
            _ui_button_debug_zone5.onClick.AddListener(RaiseZone5);
            _ui_button_debug_zone30.onClick.AddListener(RaiseZone30);
            _ui_button_debug_bomb.onClick.AddListener(RaiseBomb);
            _ui_button_debug_gold.onClick.AddListener(RaiseGold);
            _ui_button_debug_items.onClick.AddListener(RaiseItems);
        }

        private void OnDisable()
        {
            _ui_button_debug_toggle.onClick.RemoveListener(ToggleBody);
            _ui_button_debug_zone5.onClick.RemoveListener(RaiseZone5);
            _ui_button_debug_zone30.onClick.RemoveListener(RaiseZone30);
            _ui_button_debug_bomb.onClick.RemoveListener(RaiseBomb);
            _ui_button_debug_gold.onClick.RemoveListener(RaiseGold);
            _ui_button_debug_items.onClick.RemoveListener(RaiseItems);
        }
        #endregion

        #region Visibility
        // Whole-overlay visibility, driven by the hotkey. The root stays active either way.
        private void SetShown(bool shown)
        {
            _isShown = shown;
            if (_ui_button_debug_toggle) _ui_button_debug_toggle.gameObject.SetActive(shown);
            if (!shown) SetExpanded(false);
        }

        private void ToggleBody()
        {
            SetExpanded(!_isExpanded);
        }

        private void SetExpanded(bool expanded)
        {
            _isExpanded = expanded;
            if (_ui_panel_debug_body) _ui_panel_debug_body.gameObject.SetActive(expanded);
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
