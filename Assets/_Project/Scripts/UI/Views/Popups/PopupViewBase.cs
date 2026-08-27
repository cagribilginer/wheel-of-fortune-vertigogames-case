using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views.Popups
{
    /// <summary>
    /// The scale/fade choreography every popup opens and closes with. Shared here because all three popups
    /// use the exact same shape — backdrop fade plus card scale — differing only in which backdrop and
    /// which <c>_anim</c> card each one passes.
    /// </summary>
    public abstract class PopupViewBase : UIViewBase
    {
        protected void PlayOpen(Image backdrop, RectTransform card)
        {
            gameObject.SetActive(true);

            backdrop.DOKill();
            card.DOKill();

            Color c = backdrop.color;
            backdrop.color = new Color(c.r, c.g, c.b, 0f);
            backdrop.DOFade(0.82f, 0.2f);

            card.localScale = Vector3.one * 0.85f;
            card.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        protected void PlayClose(Image backdrop, RectTransform card)
        {
            backdrop.DOKill();
            card.DOKill();

            backdrop.DOFade(0f, 0.2f);
            card.DOScale(0.85f, 0.2f)
                .SetEase(Ease.InBack)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
