using System;
using System.Collections.Generic;
using UnityEngine;
using BlueLike.Core;
using BlueLike.Battle;

namespace BlueLike.Skills
{
    /// <summary>
    /// The three-card hand. Cards are drawn from a shuffled queue of the squad's Strikers;
    /// a played card goes to the back of the queue and sits out a cooldown before it can be
    /// drawn again. Discarding one card costs a single cost point, which gives the player a
    /// way to dig for the card they actually need.
    /// </summary>
    public class SkillCardManager : MonoBehaviour
    {
        public const int HandSize = 3;
        public const int DiscardCost = 1;

        [Tooltip("Seconds a played card waits before re-entering the draw queue.")]
        public float CardCooldown = DamageTable.DefaultCardCooldown;

        private readonly List<SkillCard> _queue = new List<SkillCard>();
        private readonly SkillCard[] _hand = new SkillCard[HandSize];

        public IReadOnlyList<SkillCard> Hand => _hand;

        public event Action HandChanged;
        /// <summary>Raised when a card is successfully played, before the skill resolves.</summary>
        public event Action<SkillCard> CardPlayed;

        private ExSkillRunner _runner;

        private void Awake()
        {
            _runner = GetComponent<ExSkillRunner>() ?? gameObject.AddComponent<ExSkillRunner>();
        }

        public void BuildDeck(List<BattleUnit> playerUnits)
        {
            _queue.Clear();
            for (int i = 0; i < _hand.Length; i++) _hand[i] = null;

            for (int i = 0; i < playerUnits.Count; i++)
            {
                BattleUnit unit = playerUnits[i];
                if (unit == null) continue;
                // Only Strikers get a manual card. Specials fire their skill automatically.
                if (unit.Stats.role != UnitRole.Striker) continue;
                _queue.Add(new SkillCard(unit));
            }

            Shuffle(_queue);
            RefillHand();
        }

        private void Update()
        {
            // Empty hand slots keep trying to fill as cooldowns expire.
            if (BattleManager.Instance == null) return;
            if (BattleManager.Instance.Phase != BattlePhase.Fighting) return;

            bool changed = false;
            for (int i = 0; i < _hand.Length; i++)
            {
                if (_hand[i] == null || !_hand[i].IsPlayable)
                {
                    if (_hand[i] != null && !_hand[i].IsPlayable)
                    {
                        // Owner died: the card is gone for good.
                        _hand[i] = null;
                    }
                    if (TryDraw(out SkillCard drawn))
                    {
                        _hand[i] = drawn;
                        changed = true;
                    }
                }
            }
            if (changed) HandChanged?.Invoke();
        }

        private void RefillHand()
        {
            for (int i = 0; i < _hand.Length; i++)
            {
                if (_hand[i] != null) continue;
                if (TryDraw(out SkillCard drawn)) _hand[i] = drawn;
            }
            HandChanged?.Invoke();
        }

        private bool TryDraw(out SkillCard card)
        {
            card = null;
            for (int i = 0; i < _queue.Count; i++)
            {
                SkillCard candidate = _queue[i];
                if (!candidate.IsReady) continue;
                if (!candidate.IsPlayable)
                {
                    _queue.RemoveAt(i);
                    i--;
                    continue;
                }
                _queue.RemoveAt(i);
                card = candidate;
                return true;
            }
            return false;
        }

        /// <summary>Plays the card in the given hand slot. Returns false if it could not be played.</summary>
        public bool PlayCard(int slot)
        {
            if (slot < 0 || slot >= _hand.Length) return false;
            SkillCard card = _hand[slot];
            if (card == null || !card.IsPlayable) return false;

            var mgr = BattleManager.Instance;
            if (mgr == null || mgr.Phase != BattlePhase.Fighting) return false;
            if (!mgr.Cost.CanAfford(card.Cost)) return false;
            if (_runner.IsBusy) return false;

            mgr.Cost.Spend(card.Cost);
            _hand[slot] = null;

            card.AvailableAt = Time.time + CardCooldown;
            _queue.Add(card);

            CardPlayed?.Invoke(card);
            _runner.Run(card);

            RefillHand();
            return true;
        }

        /// <summary>Throws away one card for a single cost point and draws a replacement.</summary>
        public bool DiscardCard(int slot)
        {
            if (slot < 0 || slot >= _hand.Length) return false;
            SkillCard card = _hand[slot];
            if (card == null) return false;

            var mgr = BattleManager.Instance;
            if (mgr == null || !mgr.Cost.CanAfford(DiscardCost)) return false;

            mgr.Cost.Spend(DiscardCost);
            _hand[slot] = null;
            card.AvailableAt = Time.time + CardCooldown * 0.5f;
            _queue.Add(card);
            RefillHand();
            return true;
        }

        private static void Shuffle(List<SkillCard> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                SkillCard tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
    }
}
