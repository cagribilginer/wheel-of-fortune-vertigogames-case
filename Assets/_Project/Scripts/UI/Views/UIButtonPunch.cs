using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Wheel.UI.Views
{
    /// <summary>
    /// Shared click feedback for every interactable button: a quick scale punch on its dedicated
    /// <c>_anim</c> child, never on the button's own root — the root carries the raycastable Image and, on
    /// the spin button, an independent idle-breathe tween (see <c>WheelPresenter</c>); two tweens sharing
    /// one transform's localScale would fight each other.
    /// <para>
    /// Wired with <c>AddListener</c> in <see cref="OnEnable"/>, never an Inspector OnClick binding, for the
    /// same reason every other view in this project does it that way.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonPunch : MonoBehaviour
    {
        [SerializeField] private RectTransform _animTarget;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_animTarget == null) _animTarget = FindAnimChild();
        }

        private void OnEnable() => _button.onClick.AddListener(Punch);

        private void OnDisable()
        {
            _button.onClick.RemoveListener(Punch);
            if (_animTarget != null) _animTarget.DOKill();
        }

        private void Punch()
        {
            if (_animTarget == null) return;

            _animTarget.DOKill();
            _animTarget.localScale = Vector3.one;
            // A plain 2-loop DOScale over 0.06s is only a handful of frames each way and reads as
            // nothing next to the spin button's own idle-breathe tween running on the sibling root
            // transform. DOPunchScale's spring-back over more frames is what actually registers as
            // "the button just reacted to my click."
            _animTarget.DOPunchScale(Vector3.one * -0.18f, 0.28f, vibrato: 8, elasticity: 0.75f)
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
