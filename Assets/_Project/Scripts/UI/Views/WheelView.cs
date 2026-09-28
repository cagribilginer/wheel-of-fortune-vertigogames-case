using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The wheel hub: rotor, indicator, spin button and the eight fixed slots. Only <see cref="Rotor"/> gets a
    /// rotation tween, and every animated part lives on its own dedicated transform, never a shared or laid-out one.
    /// </summary>
    public sealed class WheelView : UIViewBase
    {
        /// <summary>Hole centres as a fraction of the wheel's width, measured off the bronze/silver/golden base art.</summary>
        private const float SLOT_RING_RADIUS = 0.2955f;

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
        private RectTransform _root;

        #region References
        public RectTransform Root
        {
            get
            {
                if (!_root) _root = (RectTransform)transform;
                return _root;
            }
        }

        public RectTransform Rotor
        {
            get { return _ui_transform_wheel_rotor; }
        }
        public RectTransform Indicator
        {
            get { return _ui_transform_wheel_indicator; }
        }
        public RectTransform SpinButtonRect
        {
            get { return (RectTransform)_ui_button_wheel_spin.transform; }
        }
        public IReadOnlyList<WheelSlotView> Slots
        {
            get { return _slots; }
        }

        public event Action SpinClicked;
        #endregion

        #region Binding
        protected override void CacheReferences()
        {
            Bind(ref _ui_image_wheel_glow, "ui_image_wheel_glow");
            Bind(ref _ui_transform_wheel_rotor, "ui_transform_wheel_rotor");
            Bind(ref _ui_image_wheel_base_value, "ui_image_wheel_base_value");
            Bind(ref _ui_group_wheel_slots, "ui_group_wheel_slots");
            Bind(ref _ui_transform_wheel_indicator, "ui_transform_wheel_indicator");
            Bind(ref _ui_image_wheel_indicator_value, "ui_image_wheel_indicator_value");
            Bind(ref _ui_button_wheel_spin, "ui_button_wheel_spin");

            _slots = _ui_group_wheel_slots
                ? _ui_group_wheel_slots.GetComponentsInChildren<WheelSlotView>(includeInactive: true)
                : Array.Empty<WheelSlotView>();
        }

        private void OnEnable()
        {
            _ui_button_wheel_spin.onClick.AddListener(RaiseSpinClicked);
        }
        private void OnDisable()
        {
            _ui_button_wheel_spin.onClick.RemoveListener(RaiseSpinClicked);
        }
        private void RaiseSpinClicked()
        {
            SpinClicked?.Invoke();
        }
        #endregion

        #region Presentation
        public void SetTheme(Sprite baseSprite, Sprite indicatorSprite, Color accent, Color glow)
        {
            _ui_image_wheel_base_value.sprite = baseSprite;
            _ui_image_wheel_indicator_value.sprite = indicatorSprite;
            _ui_image_wheel_glow.color = glow;
            _ui_button_wheel_spin.image.color = accent;
        }

        public void SetSpinInteractable(bool interactable)
        {
            _ui_button_wheel_spin.interactable = interactable;
        }

        /// <summary>
        /// Places the slots on the polar ring of the base art's holes: slot 0 under the indicator at
        /// 12 o'clock, the rest clockwise at equal angles. The one layout both play mode and the editor
        /// menu below use, so an edit-mode preview can't drift from what the game shows.
        /// </summary>
        public void LayoutSlots(float wheelSize)
        {
            float radius = SLOT_RING_RADIUS * wheelSize;
            float slotAngle = 360f / _slots.Length;

            for (int i = 0; i < _slots.Length; i++)
            {
                float angleDeg = i * slotAngle;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                RectTransform slot = _slots[i].Rect;
                slot.anchoredPosition = new Vector2(radius * Mathf.Sin(angleRad), radius * Mathf.Cos(angleRad));

                // Cancels the slot's own position angle so its local "up" points radially outward — the
                // bottom of the icon/text faces the hub, and stays correct through any later rotor spin.
                slot.localEulerAngles = new Vector3(0f, 0f, -angleDeg);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Vertigo/Layout Wheel Slots")]
        private void LayoutSlotsInEditor()
        {
            CacheReferences();

            if (!_ui_transform_wheel_rotor || _slots.Length == 0)
            {
                Debug.LogWarning("[Vertigo] WheelSlotLayout: rotor or slots not found. Run OnValidate first.", this);
                return;
            }

            float wheelSize = _ui_transform_wheel_rotor.rect.width;
            LayoutSlots(wheelSize);
            for (int i = 0; i < _slots.Length; i++) UnityEditor.EditorUtility.SetDirty(_slots[i].Rect);

            Debug.Log($"[Vertigo] Laid out {_slots.Length} wheel slots for a {wheelSize:F0} wheel.", this);
        }
#endif
        #endregion
    }
}
