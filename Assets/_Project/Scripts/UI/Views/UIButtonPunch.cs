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
            _animTarget.DOScale(0.94f, 0.06f)
                .SetLoops(2, LoopType.Yoyo)
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
