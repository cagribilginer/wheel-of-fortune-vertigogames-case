using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The horizontal zone strip shell: it owns the scroll rect and content transform, the presenter owns pooling and
    /// the scroll tween. The viewport is a bare RectMask2D with no Image, and the milestone badges are re-labelled
    /// by the presenter on every zone change.
    /// </summary>
    public sealed class ZoneMapView : UIViewBase
    {
        [SerializeField] private ScrollRect _ui_scroll_zonemap;
        [SerializeField] private RectTransform _ui_content_zonemap;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_milestone_super_value;
        [SerializeField] private TextMeshProUGUI _ui_text_zonemap_milestone_safe_value;
        [SerializeField] private Button _ui_card_zonemap_milestone_super;
        [SerializeField] private Button _ui_card_zonemap_milestone_safe;

        public ScrollRect Scroll
        {
            get { return _ui_scroll_zonemap; }
        }
        public RectTransform Content
        {
            get { return _ui_content_zonemap; }
        }

        /// <summary>Raised when the player taps a milestone badge — opens the preview teaser.</summary>
        public event Action SafeMilestoneClicked;
        public event Action SuperMilestoneClicked;

        protected override void CacheReferences()
        {
            Bind(ref _ui_scroll_zonemap, "ui_scroll_zonemap");
            Bind(ref _ui_content_zonemap, "ui_content_zonemap");
            Bind(ref _ui_text_zonemap_milestone_super_value, "ui_text_zonemap_milestone_super_value");
            Bind(ref _ui_text_zonemap_milestone_safe_value, "ui_text_zonemap_milestone_safe_value");
            Bind(ref _ui_card_zonemap_milestone_super, "ui_card_zonemap_milestone_super");
            Bind(ref _ui_card_zonemap_milestone_safe, "ui_card_zonemap_milestone_safe");
        }

        private void OnEnable()
        {
            _ui_card_zonemap_milestone_safe.onClick.AddListener(RaiseSafe);
            _ui_card_zonemap_milestone_super.onClick.AddListener(RaiseSuper);
        }

        private void OnDisable()
        {
            _ui_card_zonemap_milestone_safe.onClick.RemoveListener(RaiseSafe);
            _ui_card_zonemap_milestone_super.onClick.RemoveListener(RaiseSuper);
        }

        private void RaiseSafe()
        {
            SafeMilestoneClicked?.Invoke();
        }
        private void RaiseSuper()
        {
            SuperMilestoneClicked?.Invoke();
        }

        /// <summary>
        /// <paramref name="nextSafeZone"/> / <paramref name="nextSuperZone"/> are absolute zone numbers, not
        /// intervals — the next safe/super zone strictly ahead of where the player is now.
        /// </summary>
        public void SetMilestoneTargets(int nextSafeZone, int nextSuperZone)
        {
            _ui_text_zonemap_milestone_super_value.SetText("SUPER ZONE {0}", nextSuperZone);
            _ui_text_zonemap_milestone_safe_value.SetText("SAFE ZONE {0}", nextSafeZone);
        }
    }
}
