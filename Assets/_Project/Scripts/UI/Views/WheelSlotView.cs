using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// One of the eight fixed slots on the wheel rotor. Not pooled — a wheel always has exactly eight
    /// slices, so the group holds eight permanent instances positioned by <see cref="WheelView.LayoutSlots"/>.
    /// </summary>
    public sealed class WheelSlotView : UIViewBase
    {
        // Offsets inside the wheel hole, in screen-upright space around the slot centre (slot-local would push content outward).
        private static readonly Vector2 ICON_AREA_SIZE = new(70f, 52f);
        private static readonly Vector2 ICON_CENTER = new(0f, 10f);
        private static readonly Vector2 TEXT_CENTER = new(0f, -32f);

        [SerializeField] private Image _ui_image_slot_icon_value;
        [SerializeField] private TextMeshProUGUI _ui_text_slot_amount_value;

        private RectTransform _rect;

        // The slot rotation the icon/text were last placed for. Zero-w is not a valid rotation, so the
        // first LateUpdate always places them.
        private Quaternion _placedForRotation = new(0f, 0f, 0f, 0f);

        public RectTransform Rect
        {
            get
            {
                if (!_rect) _rect = (RectTransform)transform;
                return _rect;
            }
        }

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_slot_icon_value, "ui_image_slot_icon_value");
            Bind(ref _ui_text_slot_amount_value, "ui_text_slot_amount_value");
        }

        /// <summary>
        /// The slot is rotated to its angle on the ring and the rotor spins it further, so every frame the
        /// icon and text are turned upright and their offsets are mapped back through the slot's rotation —
        /// the same content then sits identically in every hole, at any rotor angle.
        /// </summary>
        private void LateUpdate()
        {
            // Only the rotor's spin changes the slot's rotation (about Z), so an idle wheel skips all eight
            // slots instead of rewriting eight icons and labels — and dirtying the canvas — every frame.
            Quaternion slotRotation = Rect.rotation;
            if (slotRotation.z == _placedForRotation.z && slotRotation.w == _placedForRotation.w) return;
            _placedForRotation = slotRotation;

            Quaternion toSlotSpace = Quaternion.Inverse(slotRotation);
            RectTransform iconRect = _ui_image_slot_icon_value.rectTransform;
            RectTransform textRect = _ui_text_slot_amount_value.rectTransform;

            iconRect.rotation = Quaternion.identity;
            iconRect.localPosition = toSlotSpace * (Vector3)ICON_CENTER;

            textRect.rotation = Quaternion.identity;
            textRect.localPosition = toSlotSpace * (Vector3)TEXT_CENTER;
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
            AmountFormat.Apply(_ui_text_slot_amount_value, amount);
        }

        /// <summary>
        /// A null sprite means the caller (usually <c>RewardCatalog.IconFor</c>) failed to resolve an icon, leaving the
        /// "black hole" slot. Logging it names the cause instead of leaving a silent blank.
        /// </summary>
        private void SetIcon(Sprite icon)
        {
            if (!icon)
                Debug.LogWarning($"[Vertigo] {name}: no icon sprite resolved; the slot will render blank.", this);

            _ui_image_slot_icon_value.sprite = icon;
            _ui_image_slot_icon_value.enabled = icon;
            _ui_image_slot_icon_value.preserveAspect = true;
            _ui_image_slot_icon_value.maskable = false; // never inside a mask — the wheel itself isn't clipped
            _ui_image_slot_icon_value.color = Color.white;
            _ui_image_slot_icon_value.rectTransform.sizeDelta = ICON_AREA_SIZE;
        }
    }
}
