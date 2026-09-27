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
        // Named rather than passed through a config: this choreography is shared, self-contained View
        // plumbing (all three popups use the exact same shape), not a presenter-level tuning knob like
        // JuiceConfig's values — a subclass overrides just the one value it needs (see BombPopupView's
        // own BACKDROP_ALPHA) instead of every popup needing a config reference for a fade curve.
        private const float DEFAULT_BACKDROP_ALPHA = 0.82f;

        // Protected: BombPopupView's own vignette fade (a second, unrelated element on the same screen)
        // reuses this so its dismiss duration can't drift out of sync with the backdrop's.
        protected const float FADE_DURATION = 0.2f;

        private const float CLOSED_SCALE = 0.85f;
        private const float OPEN_SCALE_DURATION = 0.3f;

        protected void PlayOpen(Image backdrop, RectTransform card, float backdropAlpha = DEFAULT_BACKDROP_ALPHA)
        {
            gameObject.SetActive(true);

            backdrop.DOKill();
            card.DOKill();

            Color c = backdrop.color;
            backdrop.color = new Color(c.r, c.g, c.b, 0f);
            backdrop.DOFade(backdropAlpha, FADE_DURATION).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

            card.localScale = Vector3.one * CLOSED_SCALE;
            card.DOScale(1f, OPEN_SCALE_DURATION).SetEase(Ease.OutBack).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        protected void PlayClose(Image backdrop, RectTransform card)
        {
            backdrop.DOKill();
            card.DOKill();

            backdrop.DOFade(0f, FADE_DURATION).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            card.DOScale(CLOSED_SCALE, FADE_DURATION)
                .SetEase(Ease.InBack)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }
}
