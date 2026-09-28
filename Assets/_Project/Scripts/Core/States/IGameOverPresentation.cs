namespace Vertigo.Wheel.Core.States
{
    /// <summary>The bomb defeat and revive screen.</summary>
    public interface IGameOverPresentation
    {
        void ShowGameOver(GameOverSummary summary);
        void HideGameOver();
    }
}
