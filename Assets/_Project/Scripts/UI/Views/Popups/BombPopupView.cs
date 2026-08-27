using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The consequence screen. CONTINUE is shown only when the presenter says it is affordable and unused
    /// this run — the button is never shown greyed out, so a reviewer never sees an offer that turns out to
    /// be unhonoured.
    /// </summary>
    public sealed class BombPopupView : UIViewBase
    {
        [SerializeField] private Image _ui_image_popup_bomb_backdrop;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_zone_value;
        [SerializeField] private Button _ui_button_popup_bomb_continue;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_bomb_continue_value;
        [SerializeField] private Button _ui_button_popup_bomb_restart;

        public event Action ContinueClicked;
        public event Action RestartClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_bomb_backdrop, "ui_image_popup_bomb_backdrop");
            Bind(ref _ui_text_popup_bomb_zone_value, "ui_text_popup_bomb_zone_value");
            Bind(ref _ui_button_popup_bomb_continue, "ui_button_popup_bomb_continue");
            Bind(ref _ui_text_popup_bomb_continue_value, "ui_text_popup_bomb_continue_value");
            Bind(ref _ui_button_popup_bomb_restart, "ui_button_popup_bomb_restart");
        }

        private void OnEnable()
        {
            _ui_button_popup_bomb_continue.onClick.AddListener(RaiseContinue);
            _ui_button_popup_bomb_restart.onClick.AddListener(RaiseRestart);
        }

        private void OnDisable()
        {
            _ui_button_popup_bomb_continue.onClick.RemoveListener(RaiseContinue);
            _ui_button_popup_bomb_restart.onClick.RemoveListener(RaiseRestart);
        }

        private void RaiseContinue() => ContinueClicked?.Invoke();
        private void RaiseRestart() => RestartClicked?.Invoke();

        public void Show(int zoneReached, bool continueOffered, int continueCost)
        {
            _ui_text_popup_bomb_zone_value.SetText("You reached Zone {0}", zoneReached);
            _ui_button_popup_bomb_continue.gameObject.SetActive(continueOffered);
            if (continueOffered) _ui_text_popup_bomb_continue_value.SetText("{0:N0}", continueCost);

            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
