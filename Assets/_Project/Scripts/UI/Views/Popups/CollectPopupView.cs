using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The cash-out summary. The reward list itself is pooled <see cref="BankEntryView"/> instances driven
    /// by the presenter (Day 4) into <see cref="Content"/> — the same prefab and grid layout as the bank
    /// panel, so this popup adds no new list-rendering code of its own.
    /// </summary>
    public sealed class CollectPopupView : PopupViewBase
    {
        [SerializeField] private Image _ui_image_popup_collect_backdrop;
        [SerializeField] private RectTransform _ui_transform_popup_collect_anim;
        [SerializeField] private Image _ui_image_popup_collect_chest_value;
        [SerializeField] private TextMeshProUGUI _ui_text_popup_collect_zone_value;
        [SerializeField] private RectTransform _ui_content_popup_collect_list;
        [SerializeField] private Button _ui_button_popup_collect_confirm;

        public RectTransform Content => _ui_content_popup_collect_list;

        public event Action ConfirmClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_popup_collect_backdrop, "ui_image_popup_collect_backdrop");
            Bind(ref _ui_transform_popup_collect_anim, "ui_transform_popup_collect_anim");
            Bind(ref _ui_image_popup_collect_chest_value, "ui_image_popup_collect_chest_value");
            Bind(ref _ui_text_popup_collect_zone_value, "ui_text_popup_collect_zone_value");
            Bind(ref _ui_content_popup_collect_list, "ui_content_popup_collect_list");
            Bind(ref _ui_button_popup_collect_confirm, "ui_button_popup_collect_confirm");
        }

        private void OnEnable() => _ui_button_popup_collect_confirm.onClick.AddListener(RaiseConfirm);
        private void OnDisable() => _ui_button_popup_collect_confirm.onClick.RemoveListener(RaiseConfirm);
        private void RaiseConfirm() => ConfirmClicked?.Invoke();

        public void SetChest(Sprite chest) => _ui_image_popup_collect_chest_value.sprite = chest;

        public void Show(int zonesCleared)
        {
            _ui_text_popup_collect_zone_value.SetText("Cleared {0} zones", zonesCleared);
            PlayOpen(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
        }

        public void Hide() => PlayClose(_ui_image_popup_collect_backdrop, _ui_transform_popup_collect_anim);
    }
}
