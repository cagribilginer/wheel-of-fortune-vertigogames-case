using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One of the eight fixed slots on the wheel rotor. Not pooled — a wheel always has exactly eight
    /// slices, so the group holds eight permanent instances positioned by the WheelSlotLayout tool.
    /// </summary>
    public sealed class WheelSlotView : UIViewBase
    {
        [SerializeField] private Image _ui_image_slot_icon_value;
        [SerializeField] private TextMeshProUGUI _ui_text_slot_amount_value;

        public RectTransform Rect => (RectTransform)transform;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_slot_icon_value, "ui_image_slot_icon_value");
            Bind(ref _ui_text_slot_amount_value, "ui_text_slot_amount_value");
        }

        /// <summary>Bomb slices carry no icon and no amount text; both are hidden rather than left stale.</summary>
        public void SetBomb(Sprite bombIcon)
        {
            gameObject.SetActive(true);
            _ui_image_slot_icon_value.sprite = bombIcon;
            _ui_image_slot_icon_value.enabled = bombIcon != null;
            _ui_text_slot_amount_value.gameObject.SetActive(false);
        }

        public void SetReward(Sprite icon, int amount)
        {
            gameObject.SetActive(true);
            _ui_image_slot_icon_value.sprite = icon;
            _ui_image_slot_icon_value.enabled = icon != null;
            _ui_text_slot_amount_value.gameObject.SetActive(true);
            _ui_text_slot_amount_value.SetText("x{0}", amount);
        }
    }
}
