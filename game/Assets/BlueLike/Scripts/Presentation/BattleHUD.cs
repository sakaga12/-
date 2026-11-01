using System.Collections.Generic;
using UnityEngine;
using BlueLike.Core;
using BlueLike.Battle;
using BlueLike.Skills;

namespace BlueLike.Presentation
{
    /// <summary>
    /// Prototype HUD drawn with IMGUI so the game is playable without a single UI asset.
    /// It is intentionally throwaway: when the systems feel right, rebuild this in uGUI or
    /// UI Toolkit. Everything it reads is public API on the managers, so nothing else has to
    /// change when you do.
    /// </summary>
    public class BattleHUD : MonoBehaviour
    {
        [Tooltip("Reference height the layout is authored against.")]
        public float ReferenceHeight = 720f;

        private GUIStyle _label;
        private GUIStyle _cardTitle;
        private GUIStyle _cardCost;
        private GUIStyle _banner;
        private Texture2D _white;

        private float _scale = 1f;

        private static readonly Color PanelBg = new Color(0.07f, 0.09f, 0.14f, 0.82f);
        private static readonly Color CardBg = new Color(0.14f, 0.18f, 0.28f, 0.95f);
        private static readonly Color CardBgReady = new Color(0.18f, 0.30f, 0.46f, 0.97f);
        private static readonly Color CostFull = new Color(0.33f, 0.76f, 1f);
        private static readonly Color CostEmpty = new Color(0.18f, 0.22f, 0.30f);
        private static readonly Color HpAlly = new Color(0.35f, 0.85f, 0.45f);
        private static readonly Color HpEnemy = new Color(0.90f, 0.33f, 0.36f);
        private static readonly Color HpBack = new Color(0.08f, 0.08f, 0.10f, 0.85f);

        private void Awake()
        {
            _white = new Texture2D(1, 1);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
            _white.hideFlags = HideFlags.HideAndDontSave;
        }

        private void EnsureStyles()
        {
            if (_label != null) return;

            _label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _cardTitle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold, wordWrap = true };
            _cardCost = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            _banner = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        private void Fill(Rect r, Color c)
        {
            Color prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _white);
            GUI.color = prev;
        }

        private void OnGUI()
        {
            BattleManager mgr = BattleManager.Instance;
            if (mgr == null) return;

            EnsureStyles();
            _scale = Mathf.Max(1f, Screen.height / ReferenceHeight);

            _label.fontSize = Mathf.RoundToInt(16f * _scale);
            _cardTitle.fontSize = Mathf.RoundToInt(13f * _scale);
            _cardCost.fontSize = Mathf.RoundToInt(20f * _scale);
            _banner.fontSize = Mathf.RoundToInt(48f * _scale);

            DrawTopBar(mgr);
            DrawWorldHealthBars(mgr);
            DrawSquadPanel(mgr);
            DrawCostAndCards(mgr);
            DrawResultBanner(mgr);
        }

        // -------------------------------------------------------------------------------
        private void DrawTopBar(BattleManager mgr)
        {
            float w = 220f * _scale;
            float h = 44f * _scale;
            var r = new Rect((Screen.width - w) * 0.5f, 8f * _scale, w, h);
            Fill(r, PanelBg);

            int seconds = Mathf.CeilToInt(mgr.TimeRemaining);
            string time = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);

            var style = new GUIStyle(_label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(24f * _scale)
            };
            GUI.color = mgr.TimeRemaining < 20f ? new Color(1f, 0.45f, 0.4f) : Color.white;
            GUI.Label(r, time, style);
            GUI.color = Color.white;

            // Terrain readout, since terrain adaptation swings damage a lot.
            var tr = new Rect(r.x, r.yMax + 4f * _scale, w, 24f * _scale);
            var tstyle = new GUIStyle(_label) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(13f * _scale) };
            Fill(tr, new Color(0.07f, 0.09f, 0.14f, 0.6f));
            GUI.Label(tr, "TERRAIN: " + mgr.Terrain.ToString().ToUpper(), tstyle);
        }

        // -------------------------------------------------------------------------------
        private void DrawWorldHealthBars(BattleManager mgr)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            DrawBarsFor(mgr.PlayerUnits, cam, HpAlly);
            DrawBarsFor(mgr.EnemyUnits, cam, HpEnemy);
        }

        private void DrawBarsFor(List<BattleUnit> units, Camera cam, Color fill)
        {
            float barW = 56f * _scale;
            float barH = 6f * _scale;

            for (int i = 0; i < units.Count; i++)
            {
                BattleUnit u = units[i];
                if (u == null || u.IsDead) continue;

                Vector3 screen = cam.WorldToScreenPoint(u.transform.position + Vector3.up * 2.3f);
                if (screen.z <= 0f) continue;

                float x = screen.x - barW * 0.5f;
                float y = Screen.height - screen.y;

                Fill(new Rect(x - 1f, y - 1f, barW + 2f, barH + 2f), HpBack);
                Fill(new Rect(x, y, barW * u.HpFraction, barH), fill);

                // Cover and reload are the two states the player must be able to read instantly.
                string tag = null;
                if (u.State == UnitState.Reloading) tag = "RELOAD";
                else if (u.InCover) tag = "COVER";

                if (tag != null)
                {
                    var ts = new GUIStyle(_label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = Mathf.RoundToInt(10f * _scale)
                    };
                    GUI.color = new Color(1f, 1f, 1f, 0.75f);
                    GUI.Label(new Rect(x - 20f, y - 18f * _scale, barW + 40f, 16f * _scale), tag, ts);
                    GUI.color = Color.white;
                }
            }
        }

        // -------------------------------------------------------------------------------
        private void DrawSquadPanel(BattleManager mgr)
        {
            float cardW = 96f * _scale;
            float cardH = 54f * _scale;
            float pad = 6f * _scale;
            float x = pad;
            float y = Screen.height - cardH - pad;

            for (int i = 0; i < mgr.PlayerUnits.Count; i++)
            {
                BattleUnit u = mgr.PlayerUnits[i];
                if (u == null) continue;

                var r = new Rect(x, y, cardW, cardH);
                Fill(r, u.IsDead ? new Color(0.2f, 0.08f, 0.09f, 0.85f) : PanelBg);

                var ns = new GUIStyle(_label) { fontSize = Mathf.RoundToInt(12f * _scale) };
                GUI.Label(new Rect(r.x + 5f * _scale, r.y + 2f * _scale, r.width - 10f * _scale, 18f * _scale),
                    u.Stats.displayName, ns);

                var ts = new GUIStyle(_label) { fontSize = Mathf.RoundToInt(10f * _scale) };
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                GUI.Label(new Rect(r.x + 5f * _scale, r.y + 17f * _scale, r.width - 10f * _scale, 16f * _scale),
                    u.Stats.attackType.ToString().Substring(0, 3).ToUpper() + " / " + u.Stats.role, ts);
                GUI.color = Color.white;

                var bar = new Rect(r.x + 5f * _scale, r.yMax - 12f * _scale, r.width - 10f * _scale, 7f * _scale);
                Fill(bar, HpBack);
                Fill(new Rect(bar.x, bar.y, bar.width * u.HpFraction, bar.height), u.IsDead ? Color.gray : HpAlly);

                x += cardW + pad;
            }
        }

        // -------------------------------------------------------------------------------
        private void DrawCostAndCards(BattleManager mgr)
        {
            CostManager cost = mgr.Cost;
            SkillCardManager cards = mgr.Cards;
            if (cost == null || cards == null) return;

            float cardW = 118f * _scale;
            float cardH = 92f * _scale;
            float pad = 8f * _scale;

            float totalW = cardW * SkillCardManager.HandSize + pad * (SkillCardManager.HandSize - 1);
            float startX = Screen.width - totalW - pad * 2f;
            float cardY = Screen.height - cardH - 34f * _scale;

            // --- cost gauge -------------------------------------------------------------
            float gaugeY = Screen.height - 26f * _scale;
            var gaugeRect = new Rect(startX, gaugeY, totalW, 16f * _scale);
            int segments = (int)DamageTable.MaxCost;
            float segW = (gaugeRect.width - (segments - 1) * 2f) / segments;

            for (int i = 0; i < segments; i++)
            {
                var seg = new Rect(gaugeRect.x + i * (segW + 2f), gaugeRect.y, segW, gaugeRect.height);
                Fill(seg, CostEmpty);
                if (i < cost.Usable)
                {
                    Fill(seg, CostFull);
                }
                else if (i == cost.Usable)
                {
                    Fill(new Rect(seg.x, seg.y, seg.width * cost.PartialFill, seg.height),
                        new Color(CostFull.r, CostFull.g, CostFull.b, 0.55f));
                }
            }

            var cs = new GUIStyle(_label)
            {
                alignment = TextAnchor.MiddleRight,
                fontSize = Mathf.RoundToInt(18f * _scale)
            };
            GUI.Label(new Rect(gaugeRect.x - 70f * _scale, gaugeRect.y - 4f * _scale, 64f * _scale, 24f * _scale),
                cost.Current.ToString("0.00"), cs);

            // --- cards ------------------------------------------------------------------
            for (int i = 0; i < SkillCardManager.HandSize; i++)
            {
                var r = new Rect(startX + i * (cardW + pad), cardY, cardW, cardH);
                SkillCard card = cards.Hand[i];

                if (card == null)
                {
                    Fill(r, new Color(0.1f, 0.1f, 0.13f, 0.6f));
                    var es = new GUIStyle(_cardTitle) { alignment = TextAnchor.MiddleCenter };
                    GUI.color = new Color(1f, 1f, 1f, 0.35f);
                    GUI.Label(r, "WAIT", es);
                    GUI.color = Color.white;
                    continue;
                }

                bool affordable = cost.CanAfford(card.Cost) && mgr.Phase == BattlePhase.Fighting;
                Fill(r, affordable ? CardBgReady : CardBg);

                if (affordable)
                {
                    // Highlight border so the playable card is obvious at a glance.
                    Color accent = new Color(0.45f, 0.85f, 1f, 0.9f);
                    Fill(new Rect(r.x, r.y, r.width, 2f * _scale), accent);
                    Fill(new Rect(r.x, r.yMax - 2f * _scale, r.width, 2f * _scale), accent);
                }

                GUI.Label(new Rect(r.x + 4f * _scale, r.y + 5f * _scale, r.width - 8f * _scale, 20f * _scale),
                    card.Owner.Stats.displayName, _cardTitle);

                var ds = new GUIStyle(_cardTitle) { fontSize = Mathf.RoundToInt(11f * _scale) };
                GUI.color = new Color(1f, 1f, 1f, 0.8f);
                GUI.Label(new Rect(r.x + 4f * _scale, r.y + 26f * _scale, r.width - 8f * _scale, 40f * _scale),
                    card.SkillName, ds);
                GUI.color = Color.white;

                GUI.color = affordable ? new Color(0.5f, 0.9f, 1f) : new Color(1f, 1f, 1f, 0.45f);
                GUI.Label(new Rect(r.x, r.y, r.width - 7f * _scale, r.height - 4f * _scale),
                    card.Cost.ToString(), _cardCost);
                GUI.color = Color.white;

                // Transparent hit area over the whole card.
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    cards.PlayCard(i);
                }

                // Discard button, bottom-left of the card.
                var discard = new Rect(r.x + 3f * _scale, r.yMax - 22f * _scale, 22f * _scale, 19f * _scale);
                var dstyle = new GUIStyle(_label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.RoundToInt(13f * _scale)
                };
                Fill(discard, new Color(0f, 0f, 0f, 0.45f));
                if (GUI.Button(discard, "x", dstyle))
                {
                    cards.DiscardCard(i);
                }
            }
        }

        // -------------------------------------------------------------------------------
        private void DrawResultBanner(BattleManager mgr)
        {
            if (mgr.Phase != BattlePhase.Victory && mgr.Phase != BattlePhase.Defeat) return;

            var full = new Rect(0f, Screen.height * 0.34f, Screen.width, 120f * _scale);
            Fill(full, new Color(0f, 0f, 0f, 0.55f));

            bool win = mgr.Phase == BattlePhase.Victory;
            GUI.color = win ? new Color(0.5f, 0.95f, 1f) : new Color(1f, 0.45f, 0.45f);
            GUI.Label(full, win ? "VICTORY" : "DEFEAT", _banner);
            GUI.color = Color.white;

            float bw = 220f * _scale;
            float bh = 52f * _scale;
            var btn = new Rect((Screen.width - bw) * 0.5f, full.yMax + 16f * _scale, bw, bh);
            var bs = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20f * _scale), fontStyle = FontStyle.Bold };
            if (GUI.Button(btn, "RETRY", bs))
            {
                Setup.BattleBootstrap.Restart();
            }
        }
    }
}
