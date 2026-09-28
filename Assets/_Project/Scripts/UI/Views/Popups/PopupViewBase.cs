using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>The backdrop fade and card scale every popup opens and closes with, tuned by <see cref="JuiceConfig"/>.</summary>
    public abstract class PopupViewBase : UIViewBase
    {
        private JuiceConfig _juice;

        protected JuiceConfig Juice
        {
            get { return _juice; }
        }

        public void Configure(JuiceConfig juice)
        {
            _juice = juice;
        }

        protected void PlayOpen(Image backdrop, RectTransform card)
        {
            PlayOpen(backdrop, card, _juice.PopupBackdropAlpha);
        }

        protected void PlayOpen(Image backdrop, RectTransform card, float backdropAlpha)
        {
            gameObject.SetActive(true);

            backdrop.DOKill();
            card.DOKill();

            Color c = backdrop.color;
            backdrop.color = new Color(c.r, c.g, c.b, 0f);
            backdrop.DOFade(backdropAlpha, _juice.PopupFadeDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            card.localScale = Vector3.one * _juice.PopupClosedScale;
            card.DOScale(1f, _juice.PopupOpenDuration).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        protected void PlayClose(Image backdrop, RectTransform card)
        {
            backdrop.DOKill();
            card.DOKill();

            backdrop.DOFade(0f, _juice.PopupFadeDuration).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            card.DOScale(_juice.PopupClosedScale, _juice.PopupFadeDuration)
                .SetEase(Ease.InBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
