using System;
using Vertigo.Wheel.UI.Views;
using Vertigo.Wheel.UI.Views.Popups;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Opens the milestone teaser when the player taps a "SAFE ZONE" / "SUPER ZONE" badge, and closes it
    /// again on a backdrop or X tap. Informational only — it never touches the run — so it lives entirely
    /// in the presentation layer and wires itself the moment it is constructed.
    /// </summary>
    public sealed class MilestonePreviewPresenter : IDisposable
    {
        private readonly ZoneMapView _zoneMap;
        private readonly MilestonePreviewPopupView _popup;

        public MilestonePreviewPresenter(ZoneMapView zoneMap, MilestonePreviewPopupView popup)
        {
            _zoneMap = zoneMap;
            _popup = popup;

            _zoneMap.SafeMilestoneClicked += ShowSafePreview;
            _zoneMap.SuperMilestoneClicked += ShowSuperPreview;
            _popup.CloseClicked += _popup.Hide;
        }

        public void Dispose()
        {
            _zoneMap.SafeMilestoneClicked -= ShowSafePreview;
            _zoneMap.SuperMilestoneClicked -= ShowSuperPreview;
            _popup.CloseClicked -= _popup.Hide;
        }

        private void ShowSafePreview()
        {
            _popup.Show(isSuper: false);
        }
        private void ShowSuperPreview()
        {
            _popup.Show(isSuper: true);
        }
    }
}
