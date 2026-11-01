using UnityEngine;
using BlueLike.Core;
using BlueLike.Battle;

namespace BlueLike.Presentation
{
    /// <summary>Turns resolved hits into damage numbers and tracer lines.</summary>
    public class HitFeedback : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(1f, 0.96f, 0.86f);
        private static readonly Color CritColor = new Color(1f, 0.74f, 0.25f);
        private static readonly Color WeakColor = new Color(0.68f, 0.78f, 0.95f);
        private static readonly Color BlockColor = new Color(0.65f, 0.65f, 0.7f);
        private static readonly Color ExColor = new Color(1f, 0.45f, 0.65f);

        private void Start()
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.HitReported += OnHit;
        }

        private void OnDestroy()
        {
            if (BattleManager.Instance != null)
                BattleManager.Instance.HitReported -= OnHit;
        }

        private void OnHit(BattleUnit attacker, BattleUnit defender, DamageResult result, bool fromEx)
        {
            Color color;
            float scale = 1f;

            if (result.missed || result.blockedByCover)
            {
                color = BlockColor;
                scale = 0.8f;
            }
            else if (fromEx)
            {
                color = ExColor;
                scale = 1.5f;
            }
            else if (result.isCrit)
            {
                color = CritColor;
                scale = 1.3f;
            }
            else if (result.typeMultiplier < 0.95f)
            {
                color = WeakColor;
            }
            else
            {
                color = NormalColor;
            }

            Vector3 at = defender.transform.position + Vector3.up * Random.Range(1.5f, 2.1f);
            FloatingTextLayer.Spawn(at, result.Label, color, scale);

            if (!result.missed) TracerLayer.Spawn(attacker.MuzzlePosition, at, fromEx);
        }
    }
}
