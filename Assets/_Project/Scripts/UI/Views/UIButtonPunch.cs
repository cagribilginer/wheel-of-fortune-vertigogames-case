using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Wheel.Data.Configs;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Shared click feedback: a click SFX plus a scale punch on the button's <c>_anim</c> child, never its root, which
    /// can carry an independent tween (the spin button's breathe). Wired with <c>AddListener</c>, never OnClick.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonPunch : MonoBehaviour
    {
        [SerializeField] private RectTransform _animTarget;

        private Button _button;
        private JuiceConfig _juice;
        private Action _onClicked;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (!_animTarget) _animTarget = FindAnimChild();
        }

        /// <summary>The installer hands over the shared punch feel and what to do for the click sound; the view never reaches for a service itself.</summary>
        public void Configure(JuiceConfig juice, Action onClicked)
        {
            _juice = juice;
            _onClicked = onClicked;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Punch);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Punch);
            if (_animTarget) _animTarget.DOKill();
        }

        private void Punch()
        {
            if (_onClicked != null) _onClicked();

            if (!_animTarget || !_juice) return;

            _animTarget.DOKill();
            _animTarget.localScale = Vector3.one;
            // DOPunchScale's spring-back reads as "the button reacted"; a plain short DOScale would not.
            _animTarget.DOPunchScale(
                    Vector3.one * _juice.ButtonPunchScale, _juice.ButtonPunchDuration,
                    _juice.ButtonPunchVibrato, _juice.ButtonPunchElasticity)
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
