using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The consequence layer: screen shake and a red flash on a bomb, a glow burst on a big reward or a
    /// safe/super zone clear. Fire-and-forget — nothing else in the flow waits on these finishing.
    /// </summary>
    public sealed class VfxPresenter
    {
        private static readonly Color BOMB_FLASH_COLOR = new(1f, 0.2f, 0.2f, 1f);

        private readonly VfxView _view;
        private readonly JuiceConfig _juice;

        public VfxPresenter(VfxView view, JuiceConfig juice)
        {
            _view = view;
            _juice = juice;
        }

        public void PlayBombImpact()
        {
            RectTransform shake = _view.Shake;
            shake.DOKill();
            shake.anchoredPosition = Vector2.zero;
            shake.DOShakeAnchorPos(
                    _juice.BombShakeDuration, _juice.BombShakeStrength,
                    _juice.BombShakeVibrato, _juice.BombShakeRandomness, fadeOut: true)
                .SetLink(shake.gameObject, LinkBehaviour.KillOnDestroy);

            Flash(_view.Flash, BOMB_FLASH_COLOR);
        }

        public void PlayRewardBurst()
        {
            Flash(_view.Burst, Color.white);
        }

        // A quick spike in then a slower fade out, on whichever image is passed — the flash and the reward
        // burst are the exact same shape, just different tint and target image.
        private void Flash(Image image, Color tint)
        {
            image.DOKill();
            image.color = new Color(tint.r, tint.g, tint.b, 0f);

            Sequence sequence = DOTween.Sequence().SetLink(image.gameObject, LinkBehaviour.KillOnDestroy);
            sequence.Append(image.DOFade(_juice.FlashPeakAlpha, _juice.FlashInDuration));
            sequence.Append(image.DOFade(0f, _juice.FlashOutDuration));
        }
    }
}
