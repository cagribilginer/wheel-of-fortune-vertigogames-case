using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The consequence layer: screen shake and a red flash on a bomb, a glow burst on a big reward or a
    /// safe/super zone clear. Fire-and-forget — nothing else in the flow waits on these finishing.
    /// </summary>
    public sealed class VfxPresenter
    {
        private readonly VfxView _view;

        public VfxPresenter(VfxView view) => _view = view;

        public void PlayBombImpact()
        {
            RectTransform shake = _view.Shake;
            shake.DOKill();
            shake.anchoredPosition = Vector2.zero;
            shake.DOShakeAnchorPos(0.5f, 34f, 22, 90f, fadeOut: true)
                .SetLink(shake.gameObject, LinkBehaviour.KillOnDestroy);

            Flash(_view.Flash, new Color(1f, 0.2f, 0.2f, 1f));
        }

        public void PlayRewardBurst() => Flash(_view.Burst, Color.white);

        // A quick spike in then a slower fade out, on whichever image is passed — the flash and the reward
        // burst are the exact same shape, just different tint and target image.
        private static void Flash(Image image, Color tint)
        {
            image.DOKill();
            image.color = new Color(tint.r, tint.g, tint.b, 0f);

            Sequence sequence = DOTween.Sequence().SetLink(image.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(image.DOFade(0.85f, 0.06f));
            sequence.Append(image.DOFade(0f, 0.45f));
        }
    }
}
