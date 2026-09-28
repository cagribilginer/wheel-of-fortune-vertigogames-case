using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.Data.Services
{
    /// <summary>
    /// Ambient access to the one running <see cref="IAudioService"/>, for the rare self-wiring View
    /// (<c>UIButtonPunch</c>) that has no constructor-injection path to receive it directly. Not a general
    /// Service Locator: it resolves exactly one thing, assigned once by <c>GameInstaller.Awake()</c>.
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

        public static void PlayButtonClick()
        {
            _service.PlayOneShot(_library != null ? _library.ButtonClick : null);
        }
    }
}
