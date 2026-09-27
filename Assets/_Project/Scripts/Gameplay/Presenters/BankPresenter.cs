using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Vertigo.Wheel.Core.Rewards;
using Vertigo.Wheel.Core.Spin;
using Vertigo.Wheel.Data.Configs;
using Vertigo.Wheel.UI.Views;

namespace Vertigo.Wheel.Gameplay.Presenters
{
    /// <summary>
    /// The collected-rewards grid: a pooled <see cref="BankEntryView"/> per stacked reward, rebuilt from
    /// <see cref="RewardBank"/> whenever it changes, plus the ghost that visibly carries a fresh reward from
    /// the wheel into its grid cell.
    /// <para>
    /// The ghost is a single reused Image parented on the canvas root — never inside the
    /// GridLayoutGroup-controlled content, because a layout rebuild would fight any tween applied to one of
    /// its own children — toggled active/inactive rather than instantiated and destroyed per grant, since
    /// only one ever flies at a time.
    /// </para>
    /// </summary>
    public sealed class BankPresenter
    {
        private readonly BankView _view;
        private readonly RewardCatalog _catalog;
        private readonly RewardBank _bank;
        private readonly Transform _flightLayer;
        private readonly AudioPresenter _audio;
        private readonly JuiceConfig _juice;
        private readonly ObjectPool<BankEntryView> _pool;
        private readonly List<BankEntryView> _active = new List<BankEntryView>();

        // Lazily created, then reused for every fly-in — only one ever flies at a time (the state machine
        // waits on FlyIn's onComplete before another spin can grant a reward), so there is nothing to pool
        // here beyond keeping this single instance alive instead of Instantiate/Destroy-ing it per grant.
        private RectTransform _ghostRect;
        private Image _ghostImage;

        public BankPresenter(
            BankView view, BankEntryView entryPrefab, RewardCatalog catalog, RewardBank bank,
            Transform flightLayer, AudioPresenter audio, JuiceConfig juice)
        {
            _view = view;
            _catalog = catalog;
            _bank = bank;
            _flightLayer = flightLayer;
            _audio = audio;
            _juice = juice;

            _pool = new ObjectPool<BankEntryView>(
                () => UnityEngine.Object.Instantiate(entryPrefab, _view.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => UnityEngine.Object.Destroy(e.gameObject));
        }

        public void Refresh()
        {
            for (int i = 0; i < _active.Count; i++) _pool.Release(_active[i]);
            _active.Clear();

            IReadOnlyList<BankEntry> entries = _bank.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                BankEntryView entry = _pool.Get();
                entry.SetEntry(_catalog.IconFor(entries[i].Reward), entries[i].Amount, _catalog.IconScaleFor(entries[i].Reward));
                entry.transform.SetSiblingIndex(i);
                _active.Add(entry);
            }

            _view.SetEmpty(entries.Count == 0);

            // Force the grid + ContentSizeFitter to resolve now so the ScrollRect sees the real content
            // height this frame — otherwise a freshly populated bank does not accept a drag until the next
            // layout pass.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Content);
        }

        public void FlyIn(SpinOutcome outcome, Vector3 fromWorldPosition, Action onComplete)
        {
            Refresh();

            // Refresh has already rebuilt the grid from the post-grant bank, so this cell exists whether the
            // reward is a brand-new row (appended last) or a stack that was already there.
            int index = IndexOf(outcome.Reward);
            if (index < 0) { onComplete(); return; }

            BankEntryView targetEntry = _active[index];
            RectTransform target = targetEntry.Rect;

            // Hold the number at its pre-win value until the icon actually lands, then count it up.
            int finalAmount = _bank.Entries[index].Amount;
            int startAmount = Mathf.Max(0, finalAmount - outcome.Amount);
            targetEntry.SetAmount(startAmount);

            EnsureGhost();
            _ghostRect.DOKill();
            _ghostRect.gameObject.SetActive(true);
            _ghostRect.position = fromWorldPosition;
            _ghostImage.sprite = _catalog.IconFor(outcome.Reward);
            _ghostImage.rectTransform.localScale = Vector3.one * _catalog.IconScaleFor(outcome.Reward);

            _ghostRect.DOMove(target.position, _juice.BankFlyDuration)
                .SetEase(Ease.InBack)
                .SetLink(_ghostRect.gameObject, LinkBehaviour.KillOnDestroy)
                .OnComplete(() =>
            {
                _ghostRect.gameObject.SetActive(false);

                target.DOKill();
                target.localScale = Vector3.one;
                target.DOPunchScale(Vector3.one * _juice.BankPunchScale, _juice.BankPunchDuration)
                    .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy);

                // The reveal sting already played at the wheel stop; this is the softer "into the bag" swoosh.
                _audio.PlayBankCollect();

                DOVirtual.Int(startAmount, finalAmount, _juice.BankCounterDuration, v => targetEntry.SetAmount(v))
                    .SetLink(target.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(() =>
                    {
                        targetEntry.SetAmount(finalAmount);
                        onComplete();
                    });
            });
        }

        private void EnsureGhost()
        {
            if (_ghostRect != null) return;

            var ghostGo = new GameObject("bank_fly_ghost", typeof(RectTransform), typeof(Image));
            _ghostRect = (RectTransform)ghostGo.transform;
            _ghostRect.SetParent(_flightLayer, false);
            _ghostRect.sizeDelta = new Vector2(72f, 72f);

            _ghostImage = ghostGo.GetComponent<Image>();
            _ghostImage.preserveAspect = true;
            _ghostImage.raycastTarget = false;
        }

        private int IndexOf(RewardId reward)
        {
            IReadOnlyList<BankEntry> entries = _bank.Entries;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Reward.Equals(reward)) return i;
            return -1;
        }
    }
}
