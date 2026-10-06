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
        [SerializeField] private ScrollRect _scrollZonemap;
        [SerializeField] private RectTransform _contentZonemap;
        [SerializeField] private TextMeshProUGUI _textZonemapMilestoneSuperValue;
        [SerializeField] private TextMeshProUGUI _textZonemapMilestoneSafeValue;
        [SerializeField] private Button _cardZonemapMilestoneSuper;
        [SerializeField] private Button _cardZonemapMilestoneSafe;

        public ScrollRect Scroll
        {
            get { return _scrollZonemap; }
        }
        public RectTransform Content
        {
            get { return _contentZonemap; }
        }

        /// <summary>Raised when the player taps a milestone badge — opens the preview teaser.</summary>
        public event Action SafeMilestoneClicked;
        public event Action SuperMilestoneClicked;

        protected override void CacheReferences()
        {
            Bind(ref _scrollZonemap, "ui_scroll_zonemap");
            Bind(ref _contentZonemap, "ui_content_zonemap");
            Bind(ref _textZonemapMilestoneSuperValue, "ui_text_zonemap_milestone_super_value");
            Bind(ref _textZonemapMilestoneSafeValue, "ui_text_zonemap_milestone_safe_value");
            Bind(ref _cardZonemapMilestoneSuper, "ui_card_zonemap_milestone_super");
            Bind(ref _cardZonemapMilestoneSafe, "ui_card_zonemap_milestone_safe");
        }

        private void OnEnable()
        {
            _cardZonemapMilestoneSafe.onClick.AddListener(RaiseSafe);
            _cardZonemapMilestoneSuper.onClick.AddListener(RaiseSuper);
        }

        private void OnDisable()
        {
            _cardZonemapMilestoneSafe.onClick.RemoveListener(RaiseSafe);
            _cardZonemapMilestoneSuper.onClick.RemoveListener(RaiseSuper);
        }

        private void RaiseSafe()
        {
            if (SafeMilestoneClicked != null) SafeMilestoneClicked();
        }
        private void RaiseSuper()
        {
            if (SuperMilestoneClicked != null) SuperMilestoneClicked();
        }

        /// <summary>
        /// <paramref name="nextSafeZone"/> / <paramref name="nextSuperZone"/> are absolute zone numbers, not
        /// intervals — the next safe/super zone strictly ahead of where the player is now.
        /// </summary>
        public void SetMilestoneTargets(int nextSafeZone, int nextSuperZone)
        {
            _textZonemapMilestoneSuperValue.SetText(ViewText.SUPER_ZONE_TARGET, nextSuperZone);
            _textZonemapMilestoneSafeValue.SetText(ViewText.SAFE_ZONE_TARGET, nextSafeZone);
        }
    }
}
