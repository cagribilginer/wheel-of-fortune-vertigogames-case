using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Data.Services;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Shared click feedback for every interactable button: a click SFX plus a quick scale punch on its
    /// dedicated <c>_anim</c> child, never on the button's own root — the root carries the raycastable Image
    /// and, on the spin button, an independent idle-breathe tween (see <c>WheelPresenter</c>); two tweens
    /// sharing one transform's localScale would fight each other.
    /// <para>
    /// Wired with <c>AddListener</c> in <see cref="OnEnable"/>, never an Inspector OnClick binding, for the
    /// same reason every other view in this project does it that way.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonPunch : MonoBehaviour
    {
        // Every interactable button shares this exact feel, so it is named here rather than threaded
        // through a config — unlike JuiceConfig's values, there is nothing per-screen to tune.
        private const float PUNCH_SCALE = -0.18f;
        private const float PUNCH_DURATION = 0.28f;
        private const int PUNCH_VIBRATO = 8;
        private const float PUNCH_ELASTICITY = 0.75f;

        [SerializeField] private RectTransform _animTarget;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_animTarget == null) _animTarget = FindAnimChild();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Punch);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Punch);
            if (_animTarget != null) _animTarget.DOKill();
        }

        private void Punch()
        {
            AudioHub.PlayButtonClick();

            if (_animTarget == null) return;

            _animTarget.DOKill();
            _animTarget.localScale = Vector3.one;
            // DOPunchScale's spring-back reads as "the button reacted"; a plain short DOScale would not.
            _animTarget.DOPunchScale(Vector3.one * PUNCH_SCALE, PUNCH_DURATION, vibrato: PUNCH_VIBRATO, elasticity: PUNCH_ELASTICITY)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private RectTransform FindAnimChild()
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.EndsWith("_anim", StringComparison.Ordinal))
                    return (RectTransform)child;
            }

            Debug.LogWarning("[Vertigo] UIButtonPunch: no '_anim' child found to animate.", this);
            return null;
        }
    }
}
