using Vertigo.Wheel.Core.Run;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// Top strip: the persistent gold balance, mirrored live from <see cref="GoldWallet"/> rather than
    /// routed through the state machine — gold can change independently of any zone transition once
    /// continue spending lands. The zone number is shown by the zone strip, not here.
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
    }
}
