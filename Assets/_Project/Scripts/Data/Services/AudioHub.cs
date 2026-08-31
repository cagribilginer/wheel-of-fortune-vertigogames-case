using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// Ambient access to the one running <see cref="IAudioService"/>, for the handful of self-contained
    /// Views — <c>UIButtonPunch</c> is the only one today — that have no constructor-injection path at all
    /// by design (they auto-wire from the GameObject's own name, with zero external wiring). Every other
    /// audio call site (<c>WheelPresenter</c>, <c>ScreenPresentation</c>, <c>PopupPresenter</c>) receives
    /// its <see cref="IAudioService"/> through a normal constructor, exactly like every other dependency in
    /// the composition root; this static facade exists only where that path doesn't reach.
    /// <para>
    /// This is the same ambient-global shape the codebase already gives DOTween (<c>DOTween.Sequence()</c>
    /// et al. are called directly throughout, never injected) — not a general-purpose Service Locator. It
    /// resolves exactly one thing, is assigned exactly once (by <c>GameInstaller.Awake()</c>), and nothing
    /// branches on what it happens to hold.
    /// </para>
    /// </summary>
    public static class AudioHub
    {
        private static IAudioService _service = NullAudioService.Instance;
        private static AudioLibrary _library;

        /// <summary>Called once by <c>GameInstaller.Awake()</c>.</summary>
        public static void Initialize(IAudioService service, AudioLibrary library)
        {
            _service = service ?? NullAudioService.Instance;
            _library = library;
        }

        public static IAudioService Service => _service;

        public static void PlayButtonClick() => _service.PlayOneShot(_library != null ? _library.ButtonClick : null);
    }
}
