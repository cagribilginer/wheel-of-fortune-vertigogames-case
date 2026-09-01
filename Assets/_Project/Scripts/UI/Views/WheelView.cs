using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The wheel hub: rotor, indicator, spin button and the eight fixed slots.
    /// <para>
    /// Only <see cref="Rotor"/> ever gets a rotation tween (Day 4) — never this view's own root and never a
    /// LayoutGroup-controlled node — which is the concrete reason every animated part of the tree lives on
    /// its own dedicated <c>_anim</c>/<c>_rotor</c>/<c>_indicator</c> transform instead of a shared one.
    /// </para>
    /// </summary>
    public sealed class WheelView : UIViewBase
    {
        [SerializeField] private Image _ui_image_wheel_glow;
        [SerializeField] private RectTransform _ui_transform_wheel_rotor;
        [SerializeField] private Image _ui_image_wheel_base_value;
        [SerializeField] private RectTransform _ui_group_wheel_slots;
        [SerializeField] private RectTransform _ui_transform_wheel_indicator;
        [SerializeField] private Image _ui_image_wheel_indicator_value;
        [SerializeField] private Button _ui_button_wheel_spin;
        [SerializeField] private WheelSlotView[] _slots = Array.Empty<WheelSlotView>();

        /// <summary>
        /// The panel root. Nothing else repositions it, so the zone-advance transition is free to slide it
        /// off-screen and back — unlike <see cref="Rotor"/>, which owns the spin rotation.
        /// </summary>
        public RectTransform Root => (RectTransform)transform;

        public RectTransform Rotor => _ui_transform_wheel_rotor;
        public RectTransform Indicator => _ui_transform_wheel_indicator;
        public RectTransform SpinButtonRect => (RectTransform)_ui_button_wheel_spin.transform;
        public IReadOnlyList<WheelSlotView> Slots => _slots;

        public event Action SpinClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_image_wheel_glow, "ui_image_wheel_glow");
            Bind(ref _ui_transform_wheel_rotor, "ui_transform_wheel_rotor");
            Bind(ref _ui_image_wheel_base_value, "ui_image_wheel_base_value");
            Bind(ref _ui_group_wheel_slots, "ui_group_wheel_slots");
            Bind(ref _ui_transform_wheel_indicator, "ui_transform_wheel_indicator");
            Bind(ref _ui_image_wheel_indicator_value, "ui_image_wheel_indicator_value");
            Bind(ref _ui_button_wheel_spin, "ui_button_wheel_spin");

            _slots = _ui_group_wheel_slots == null
                ? Array.Empty<WheelSlotView>()
                : _ui_group_wheel_slots.GetComponentsInChildren<WheelSlotView>(includeInactive: true);
        }

        private void OnEnable() => _ui_button_wheel_spin.onClick.AddListener(RaiseSpinClicked);
        private void OnDisable() => _ui_button_wheel_spin.onClick.RemoveListener(RaiseSpinClicked);
        private void RaiseSpinClicked() => SpinClicked?.Invoke();

        public void SetTheme(Sprite baseSprite, Sprite indicatorSprite, Color accent, Color glow)
        {
            _ui_image_wheel_base_value.sprite = baseSprite;
            _ui_image_wheel_indicator_value.sprite = indicatorSprite;
            _ui_image_wheel_glow.color = glow;
            _ui_button_wheel_spin.image.color = accent;
        }

        public void SetSpinInteractable(bool interactable) => _ui_button_wheel_spin.interactable = interactable;

#if UNITY_EDITOR
        /// <summary>
        /// Places the eight fixed slots on the polar ring implied by the cylinder artwork: slot 0 sits
        /// under the indicator at 12 o'clock, and slots run clockwise every 45 degrees. R is tuned against
        /// the actual base sprite rather than hard-coded, so re-running after an art swap stays correct.
        /// </summary>
        [ContextMenu("Vertigo/Layout Wheel Slots")]
        private void LayoutWheelSlots()
        {
            CacheReferences();

            if (_ui_transform_wheel_rotor == null || _slots.Length == 0)
            {
                Debug.LogWarning("[Vertigo] WheelSlotLayout: rotor or slots not found. Run OnValidate first.", this);
                return;
            }

            float wheelSize = _ui_transform_wheel_rotor.rect.width;
            // Keep in sync with WheelPresenter.LayoutSlots: 0.303 centres the slots in the bronze holes.
            float radius = 0.303f * wheelSize;
            float slotAngle = 360f / _slots.Length;

            for (int i = 0; i < _slots.Length; i++)
            {
                float angleRad = i * slotAngle * Mathf.Deg2Rad;
                float x = radius * Mathf.Sin(angleRad);
                float y = radius * Mathf.Cos(angleRad);

                RectTransform slotRect = _slots[i].Rect;
                slotRect.anchoredPosition = new Vector2(x, y);

                UnityEditor.EditorUtility.SetDirty(slotRect);
            }

            Debug.Log($"[Vertigo] Laid out {_slots.Length} wheel slots at R={radius:F1} for a {wheelSize:F0} wheel.", this);
        }
#endif
    }
}
