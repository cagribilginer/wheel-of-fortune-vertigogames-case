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
    /// The collected-rewards grid and the icons that fly a reward from the wheel into its cell. The grid follows
    /// what has arrived: a new cell is reserved hidden and revealed on the first landing, an existing number
    /// climbs on a landing. Every icon owns a ghost on the canvas root, outside the layout group.
    /// </summary>
    public sealed class BankPresenter
    {
        // Matches BankEntryViewMono's own icon box closely enough that a ghost doesn't visibly resize when it lands
        // (that box is 88x88, but the ghost also needs headroom to fly over other UI unclipped).
        private static readonly Vector2 s_ghostSize = new(72f, 72f);

        private readonly BankViewMono _view;
        private readonly RewardCatalog _catalog;
        private readonly RewardBank _bank;
        private readonly Transform _flightLayer;
        private readonly AudioPresenter _audio;
        private readonly JuiceConfig _juice;
        private readonly ObjectPool<BankEntryViewMono> _pool;
        private readonly List<BankCell> _cells = new();
        private readonly List<IconFlight> _flights = new();
        private readonly Stack<Ghost> _freeGhosts = new();

        // Bumped by every Refresh. A flight from an older generation was overtaken by a rebuild that already shows
        // the final state, so its landing must not add to it a second time.
        private int _generation;

        public BankPresenter(
            BankViewMono view, BankEntryViewMono entryPrefab, RewardCatalog catalog, RewardBank bank,
            Transform flightLayer, AudioPresenter audio, JuiceConfig juice)
        {
            _view = view;
            _catalog = catalog;
            _bank = bank;
            _flightLayer = flightLayer;
            _audio = audio;
            _juice = juice;

            _pool = new ObjectPool<BankEntryViewMono>(
                () => UnityEngine.Object.Instantiate(entryPrefab, _view.Content),
                e => e.gameObject.SetActive(true),
                e => e.gameObject.SetActive(false),
                e => UnityEngine.Object.Destroy(e.gameObject));
        }

        #region Bank grid
        /// <summary>Rebuilds the grid from the model. Anything still flying is finished off, since the rebuild shows its result.</summary>
        public void Refresh()
        {
            _generation++;
            CancelFlights();

            for (int i = 0; i < _cells.Count; i++)
            {
                _cells[i].StopCounting();
                _pool.Release(_cells[i].View);
            }
            _cells.Clear();

            IReadOnlyList<BankEntry> entries = _bank.Entries;
            for (int i = 0; i < entries.Count; i++)
                AddCell(entries[i].Reward, entries[i].Amount, revealed: true);

            _view.SetEmpty(entries.Count == 0);

            // Resolve the layout now so the ScrollRect sees the real content height this frame.
            LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Content);
        }

        private BankCell AddCell(RewardId reward, int amount, bool revealed)
        {
            BankEntryViewMono view = _pool.Get();
            view.SetEntry(_catalog.IconFor(reward), amount);
            view.SetRevealed(revealed);
            view.transform.SetSiblingIndex(_cells.Count);

            var cell = new BankCell(view, reward, amount, revealed);
            _cells.Add(cell);
            return cell;
        }

        private BankCell FindCell(RewardId reward)
        {
            for (int i = 0; i < _cells.Count; i++)
                if (_cells[i].Reward.Equals(reward)) return _cells[i];
            return null;
        }
        #endregion

        #region Flight
        /// <summary>
        /// Flies the reward from the wheel into the bank. <paramref name="onComplete"/> runs once, after the last icon
        /// has landed and its number has finished climbing. Flights may overlap.
        /// </summary>
        public void FlyIn(SpinOutcome outcome, Vector3 fromWorldPosition, Action onComplete)
        {
            BankCell cell = FindCell(outcome.Reward);
            if (cell == null)
            {
                // A new stack: reserve its slot now, hidden, so the layout knows where the icon lands. The cell shows
                // up when the first icon arrives, not before.
                cell = AddCell(outcome.Reward, 0, revealed: false);
                _view.SetEmpty(false);
                LayoutRebuilder.ForceRebuildLayoutImmediate(_view.Content);
            }

            int icons = Mathf.Clamp(_juice.BankFlyIconsPerReward, 1, Mathf.Max(1, outcome.Amount));
            var reward = new RewardFlight(icons, onComplete);

            for (int i = 0; i < icons; i++)
            {
                // The amount is split across the icons; the first ones take the remainder.
                int share = outcome.Amount / icons + (i < outcome.Amount % icons ? 1 : 0);
                var flight = new IconFlight(this, reward, cell, share, fromWorldPosition, _generation);
                _flights.Add(flight);
                flight.TakeOffAfter(i * _juice.BankFlyStagger);
            }
        }

        private void CancelFlights()
        {
            // Iterated over a copy: cancelling removes the flight from the live list.
            foreach (IconFlight flight in _flights.ToArray()) flight.Cancel();
        }

        // An icon reached its cell: reveal it if it was hidden, punch it, and count the number up.
        private void Arrive(IconFlight flight)
        {
            BankCell cell = flight.Cell;

            if (!cell.IsRevealed)
            {
                cell.IsRevealed = true;
                cell.View.SetRevealed(true);
            }

            cell.Target += flight.Share;

            // A second icon landing while the first is still counting finishes that count first (its own flight
            // completes with it), then counts on from there.
            cell.FinishCounting();

            RectTransform rect = cell.View.Rect;
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * _juice.BankPunchScale, _juice.BankPunchDuration)
                .SetLink(rect.gameObject, LinkBehaviour.KillOnDestroy);

            // The reveal sting already played at the wheel stop; this is the softer "into the bag" swoosh.
            _audio.PlayBankCollect();

            cell.StartCounting(_juice.BankCounterDuration, flight.OnCounted);
        }

        private Ghost TakeGhost()
        {
            if (_freeGhosts.Count > 0) return _freeGhosts.Pop();

            var go = new GameObject("bank_fly_ghost", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_flightLayer, false);
            rect.sizeDelta = s_ghostSize;
            go.SetActive(false);

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;

            return new Ghost(rect, image);
        }

        private void ReleaseGhost(Ghost ghost)
        {
            ghost.Rect.gameObject.SetActive(false);
            _freeGhosts.Push(ghost);
        }
        #endregion

        #region Flight state
        private sealed class BankCell
        {
            public readonly BankEntryViewMono View;
            public readonly RewardId Reward;
            public bool IsRevealed;

            /// <summary>What the number is counting towards: the amount of every icon that has landed so far.</summary>
            public int Target;

            /// <summary>What the number currently reads.</summary>
            public int Displayed;

            // Dropped on kill: with tween recycling on, a stale reference would kill whichever tween reuses the object.
            private Tween _count;

            public BankCell(BankEntryViewMono view, RewardId reward, int amount, bool revealed)
            {
                View = view;
                Reward = reward;
                IsRevealed = revealed;
                Target = amount;
                Displayed = amount;
            }

            public void StartCounting(float duration, TweenCallback onCounted)
            {
                _count = DOVirtual.Int(Displayed, Target, duration, SetDisplayed)
                    .SetLink(View.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnComplete(onCounted)
                    .OnKill(OnCountKilled);
            }

            /// <summary>Jumps the running count to its target, which also reports its flight as done.</summary>
            public void FinishCounting()
            {
                if (_count != null) _count.Kill(complete: true);
            }

            /// <summary>Abandons the running count without reporting anything.</summary>
            public void StopCounting()
            {
                if (_count != null) _count.Kill();
            }

            private void SetDisplayed(int value)
            {
                Displayed = value;
                View.SetAmount(value);
            }

            private void OnCountKilled()
            {
                _count = null;
            }
        }

        private readonly struct Ghost
        {
            public readonly RectTransform Rect;
            public readonly Image Image;

            public Ghost(RectTransform rect, Image image)
            {
                Rect = rect;
                Image = image;
            }
        }

        /// <summary>One reward's flight: calls back once when all of its icons are done.</summary>
        private sealed class RewardFlight
        {
            private readonly Action _onComplete;
            private int _remaining;

            public RewardFlight(int icons, Action onComplete)
            {
                _remaining = icons;
                _onComplete = onComplete;
            }

            public void IconDone()
            {
                if (--_remaining == 0) _onComplete();
            }
        }

        /// <summary>One icon on its way to a cell. Its steps are named handlers so the flow reads top to bottom.</summary>
        private sealed class IconFlight
        {
            private readonly BankPresenter _owner;
            private readonly RewardFlight _reward;
            private readonly Vector3 _from;
            private readonly int _generation;
            private Ghost _ghost;
            private bool _hasGhost;
            private Tween _tween;
            private bool _isFinished;

            public readonly BankCell Cell;
            public readonly int Share;

            public IconFlight(
                BankPresenter owner, RewardFlight reward, BankCell cell, int share, Vector3 from, int generation)
            {
                _owner = owner;
                _reward = reward;
                Cell = cell;
                Share = share;
                _from = from;
                _generation = generation;
            }

            public void TakeOffAfter(float delay)
            {
                _ghost = _owner.TakeGhost();
                _hasGhost = true;
                _ghost.Image.sprite = _owner._catalog.IconFor(Cell.Reward);

                // Both ends are given explicitly: a DOMove would read the ghost's position as its start before any
                // OnStart could move it to the wheel, so the icon would take off from wherever the ghost last was.
                _tween = DOVirtual.Vector3(_from, Cell.View.Rect.position, _owner._juice.BankFlyDuration, OnFlyStep)
                    .SetDelay(delay)
                    .SetEase(Ease.InBack)
                    .SetLink(_ghost.Rect.gameObject, LinkBehaviour.KillOnDestroy)
                    .OnStart(OnTakeOff)
                    .OnComplete(OnLanded);
            }

            private void OnTakeOff()
            {
                _ghost.Rect.position = _from;
                _ghost.Rect.gameObject.SetActive(true);
            }

            private void OnFlyStep(Vector3 position)
            {
                _ghost.Rect.position = position;
            }

            private void OnLanded()
            {
                _tween = null;
                ReleaseGhost();

                // A rebuild overtook this flight and already shows the final amount; counting it again would double it.
                if (_generation != _owner._generation)
                {
                    Finish();
                    return;
                }

                _owner.Arrive(this);
            }

            /// <summary>The number has finished climbing.</summary>
            public void OnCounted()
            {
                Finish();
            }

            /// <summary>A rebuild supersedes this flight: stop it without counting, but still report it done.</summary>
            public void Cancel()
            {
                if (_isFinished) return;

                // Only a flight still in the air owns a live tween; a landed one's tween is finished and may be recycled.
                if (_tween != null) _tween.Kill();
                _tween = null;
                ReleaseGhost();
                Finish();
            }

            private void ReleaseGhost()
            {
                if (!_hasGhost) return;

                _hasGhost = false;
                _owner.ReleaseGhost(_ghost);
            }

            private void Finish()
            {
                if (_isFinished) return;

                _isFinished = true;
                _owner._flights.Remove(this);
                _reward.IconDone();
            }
        }
        #endregion
    }
}
