using System;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// A collapsible cheat bar for fast manual testing: jump zones, force the bomb defeat screen, top up
    /// gold, stuff the bank. It raises intent events only — <c>DebugPresenter</c> owns what they do.
    /// <para>
    /// Present in every build of the scene but inert outside the editor and development builds: the whole
    /// object switches itself off in <see cref="Awake"/> when neither applies, so a shipped build never
    /// shows it and never wires it.
    /// </para>
    /// <para>
    /// Even in the editor / a dev build it starts <em>hidden</em> so it never sits in a gameplay recording.
    /// Press <see cref="ToggleKey"/> to reveal the DEBUG bar; press it again to hide the whole thing. The
    /// root GameObject stays active while hidden so this component keeps polling for that key.
    /// </para>
    /// </summary>
    public sealed class DebugOverlayView : UIViewBase
    {
        // Tap-only landscape game — nothing else reads the keyboard, so a plain letter is safe here.
        private const KeyCode ToggleKey = KeyCode.D;

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

        private bool _available;
        private bool _shown;
        private bool _expanded;

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

        private void Awake()
        {
            _available = Application.isEditor || Debug.isDebugBuild;
            if (!_available)
            {
                gameObject.SetActive(false);
                return;
            }

            // Hidden until the hotkey asks for it, so a fresh recording is clean.
            SetShown(false);
        }

        private void Update()
        {
            if (_available && Input.GetKeyDown(ToggleKey))
                SetShown(!_shown);
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

        // Whole-overlay visibility, driven by the hotkey. The root stays active either way.
        private void SetShown(bool shown)
        {
            _shown = shown;
            if (_ui_button_debug_toggle != null) _ui_button_debug_toggle.gameObject.SetActive(shown);
            if (!shown) SetExpanded(false);
        }

        private void ToggleBody() => SetExpanded(!_expanded);

        private void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            if (_ui_panel_debug_body != null) _ui_panel_debug_body.gameObject.SetActive(expanded);
        }

        private void RaiseZone5() => JumpToZone5Clicked?.Invoke();
        private void RaiseZone30() => JumpToZone30Clicked?.Invoke();
        private void RaiseBomb() => TriggerBombClicked?.Invoke();
        private void RaiseGold() => GrantGoldClicked?.Invoke();
        private void RaiseItems() => GrantItemsClicked?.Invoke();
    }
}
