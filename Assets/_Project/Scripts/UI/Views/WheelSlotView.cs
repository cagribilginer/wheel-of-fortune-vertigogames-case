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
        // The authored centre-to-centre distance between icon and text (see MainSceneBuilder.
        // BuildWheelSlotPrefab: icon.y=6.1, text.y=-35.9), reapplied in world space every frame so the
        // text stays directly under the icon instead of sliding off to whichever direction the slot's own
        // rotation happens to point "down". Keep this in sync with that prefab's two Y offsets.
        private const float TEXT_OFFSET_BELOW_ICON = 42f;

        // The authored icon anchor (see MainSceneBuilder.BuildWheelSlotPrefab: icon.y=6.1) that every
        // reward's IconOffset is added on top of — box centring alone isn't enough once different icon
        // artwork has different visual weight (a wide weapon render vs. a tall bottle vs. a chest lid all
        // read as "centred" at different actual anchor points), so this is the per-reward correction hook.
        private static readonly Vector2 ICON_BASE_ANCHORED_POSITION = new Vector2(0f, 6.1f);

        [SerializeField] private Image _ui_image_slot_icon_value;
        [SerializeField] private TextMeshProUGUI _ui_text_slot_amount_value;

        public RectTransform Rect
        {
            get { return (RectTransform)transform; }
        }

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_slot_icon_value, "ui_image_slot_icon_value");
            Bind(ref _ui_text_slot_amount_value, "ui_text_slot_amount_value");
        }

        /// <summary>
        /// The slot is rotated to sit correctly on the polar ring (see <c>WheelPresenter.LayoutSlots</c>),
        /// which would otherwise carry the icon and text sideways/upside-down with it. Locking rotation
        /// alone isn't enough for the text — its authored offset would still swing to whichever direction
        /// the slot's "down" now points — so its position is re-anchored below the icon in world space too.
        /// </summary>
        private void LateUpdate()
        {
            RectTransform iconRect = _ui_image_slot_icon_value.rectTransform;
            RectTransform textRect = _ui_text_slot_amount_value.rectTransform;

            iconRect.rotation = Quaternion.identity;

            textRect.rotation = Quaternion.identity;
            textRect.position = iconRect.position + new Vector3(0f, -TEXT_OFFSET_BELOW_ICON, 0f);
        }

        /// <summary>
        /// Bomb slices carry no reward amount; the text stays active but goes blank rather than being
        /// deactivated, so a later <see cref="SetReward"/> on the same instance never has to remember to
        /// re-enable a GameObject some earlier call turned off.
        /// </summary>
        public void SetBomb(Sprite bombIcon)
        {
            gameObject.SetActive(true);
            SetIcon(bombIcon, 1f, Vector2.zero);
            _ui_text_slot_amount_value.gameObject.SetActive(true);
            _ui_text_slot_amount_value.color = Color.white;
            _ui_text_slot_amount_value.SetText(string.Empty);
        }

        public void SetReward(Sprite icon, int amount, float iconScale = 1f, Vector2 iconOffset = default)
        {
            gameObject.SetActive(true);
            SetIcon(icon, iconScale, iconOffset);
            _ui_text_slot_amount_value.gameObject.SetActive(true);
            _ui_text_slot_amount_value.color = Color.white;
            _ui_text_slot_amount_value.SetText("x{0}", amount);
        }

        /// <summary>
        /// A null sprite here means the caller (usually <c>RewardCatalog.IconFor</c>) failed to resolve one —
        /// that's the one condition that leaves a slot showing nothing but the wheel's own painted-in slot
        /// art behind it, i.e. exactly the "black hole" symptom. Logging it turns that into a named cause
        /// instead of a silent blank. <paramref name="iconScale"/> corrects for how much of the source
        /// sprite's own canvas the artwork fills, and <paramref name="offset"/> for where its visual weight
        /// sits within that canvas — see <see cref="Vertigo.Wheel.Data.Configs.RewardDefinition"/>.
        /// </summary>
        private void SetIcon(Sprite icon, float iconScale, Vector2 offset)
        {
            if (icon == null)
                Debug.LogWarning($"[Vertigo] {name}: no icon sprite resolved; the slot will render blank.", this);

            _ui_image_slot_icon_value.sprite = icon;
            _ui_image_slot_icon_value.enabled = icon != null;
            _ui_image_slot_icon_value.preserveAspect = true;
            _ui_image_slot_icon_value.maskable = false; // never inside a mask — the wheel itself isn't clipped
            _ui_image_slot_icon_value.color = Color.white;
            _ui_image_slot_icon_value.rectTransform.localScale = Vector3.one * iconScale;
            _ui_image_slot_icon_value.rectTransform.anchoredPosition = ICON_BASE_ANCHORED_POSITION + offset;
        }
    }
}
