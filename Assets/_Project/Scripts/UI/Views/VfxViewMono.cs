using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The screen-space consequence layer: a shake target plus two full-bleed alpha-zero images the
    /// presenter fades in and out. Purely passive — every color and offset change is driven from
    /// <c>VfxPresenter</c>; this view only exposes the three nodes.
    /// </summary>
    public sealed class VfxViewMono : UIViewBaseMono
    {
        [SerializeField] private RectTransform _transformVfxScreenshake;
        [SerializeField] private Image _imageVfxFlash;
        [SerializeField] private Image _imageVfxRewardBurst;

        public RectTransform Shake
        {
            get { return _transformVfxScreenshake; }
        }
        public Image Flash
        {
            get { return _imageVfxFlash; }
        }
        public Image Burst
        {
            get { return _imageVfxRewardBurst; }
        }

        protected override void CacheReferences()
        {
            Bind(ref _transformVfxScreenshake, "ui_transform_vfx_screenshake");
            Bind(ref _imageVfxFlash, "ui_image_vfx_flash");
            Bind(ref _imageVfxRewardBurst, "ui_image_vfx_reward_burst");
        }
    }
}
