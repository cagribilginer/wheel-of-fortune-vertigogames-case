using Vertigo.Wheel.UI.Views;
using Vertigo.Wheel.UI.Views.Popups;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Opens the milestone teaser when the player taps a "SAFE ZONE" / "SUPER ZONE" badge, and closes it
    /// again on a backdrop or X tap. Informational only — it never touches the run — so it lives entirely
    /// in the presentation layer and wires itself the moment it is constructed.
    /// </summary>
    public sealed class MilestonePreviewPresenter
    {
        private readonly MilestonePreviewPopupView _popup;

        public MilestonePreviewPresenter(ZoneMapView zoneMap, MilestonePreviewPopupView popup)
        {
            _popup = popup;

            zoneMap.SafeMilestoneClicked += () => _popup.Show(isSuper: false);
            zoneMap.SuperMilestoneClicked += () => _popup.Show(isSuper: true);
            _popup.CloseClicked += _popup.Hide;
        }
    }
}
