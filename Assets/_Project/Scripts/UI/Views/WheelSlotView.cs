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

        /// <summary>
        /// Bomb slices carry no reward amount; the text stays active but goes blank rather than being
        /// deactivated, so a later <see cref="SetReward"/> on the same instance never has to remember to
        /// re-enable a GameObject some earlier call turned off.
        /// </summary>
        public void SetBomb(Sprite bombIcon)
        {
            gameObject.SetActive(true);
            SetIcon(bombIcon);
            _ui_text_slot_amount_value.gameObject.SetActive(true);
            _ui_text_slot_amount_value.color = Color.white;
            _ui_text_slot_amount_value.SetText(string.Empty);
        }

        public void SetReward(Sprite icon, int amount)
        {
            gameObject.SetActive(true);
            SetIcon(icon);
            _ui_text_slot_amount_value.gameObject.SetActive(true);
            _ui_text_slot_amount_value.color = Color.white;
            _ui_text_slot_amount_value.SetText("x{0}", amount);
        }

        /// <summary>
        /// A null sprite here means the caller (usually <c>RewardCatalog.IconFor</c>) failed to resolve one —
        /// that's the one condition that leaves a slot showing nothing but the wheel's own painted-in slot
        /// art behind it, i.e. exactly the "black hole" symptom. Logging it turns that into a named cause
        /// instead of a silent blank.
        /// </summary>
        private void SetIcon(Sprite icon)
        {
            if (icon == null)
                Debug.LogWarning($"[Vertigo] {name}: no icon sprite resolved; the slot will render blank.", this);

            _ui_image_slot_icon_value.sprite = icon;
            _ui_image_slot_icon_value.enabled = icon != null;
            _ui_image_slot_icon_value.preserveAspect = true;
            _ui_image_slot_icon_value.maskable = false; // never inside a mask — the wheel itself isn't clipped
            _ui_image_slot_icon_value.color = Color.white;
        }
    }
}
