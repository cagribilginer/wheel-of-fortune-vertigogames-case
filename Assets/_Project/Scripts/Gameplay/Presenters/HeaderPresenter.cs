using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Top strip: zone number (set by <see cref="ScreenPresentation.ShowZone"/>) and the persistent gold
    /// balance, mirrored live from <see cref="GoldWallet"/> rather than routed through the state machine —
    /// gold can change independently of any zone transition once continue spending lands.
    /// </summary>
    public sealed class HeaderPresenter
    {
        private readonly HeaderView _view;

        public HeaderPresenter(HeaderView view, GoldWallet wallet)
        {
            _view = view;
            wallet.Changed += _view.SetGold;
            _view.SetGold(wallet.Balance);
        }

        public void SetZone(int zone) => _view.SetZone(zone);
    }
}
