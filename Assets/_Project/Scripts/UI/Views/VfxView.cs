using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// The screen-space consequence layer: a shake target plus two full-bleed alpha-zero images the
    /// presenter fades in and out. Purely passive — every color and offset change is driven from
    /// <c>VfxPresenter</c>; this view only exposes the three nodes.
    /// </summary>
    public sealed class VfxView : UIViewBase
    {
        [SerializeField] private RectTransform _ui_transform_vfx_screenshake;
        [SerializeField] private Image _ui_image_vfx_flash;
        [SerializeField] private Image _ui_image_vfx_reward_burst;

        public RectTransform Shake => _ui_transform_vfx_screenshake;
        public Image Flash => _ui_image_vfx_flash;
        public Image Burst => _ui_image_vfx_reward_burst;

        protected override void CacheReferences()
        {
            Bind(ref _ui_transform_vfx_screenshake, "ui_transform_vfx_screenshake");
            Bind(ref _ui_image_vfx_flash, "ui_image_vfx_flash");
            Bind(ref _ui_image_vfx_reward_burst, "ui_image_vfx_reward_burst");
        }
    }
}
