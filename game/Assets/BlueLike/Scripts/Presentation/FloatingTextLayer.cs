using System.Collections.Generic;
using UnityEngine;

namespace BlueLike.Presentation
{
    /// <summary>
    /// Cheap world-space damage numbers drawn with IMGUI. Prototype quality on purpose -
    /// swap this for pooled TextMeshPro quads when you care about the look.
    /// </summary>
    public class FloatingTextLayer : MonoBehaviour
    {
        private struct Entry
        {
            public Vector3 worldPos;
            public string text;
            public Color color;
            public float bornAt;
            public float lifetime;
            public float scale;
            public float driftX;
        }

        private static FloatingTextLayer _instance;
        private readonly List<Entry> _entries = new List<Entry>(64);
        private GUIStyle _style;

        public static void Spawn(Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            if (_instance == null)
            {
                var go = new GameObject("FloatingTextLayer");
                _instance = go.AddComponent<FloatingTextLayer>();
            }
            _instance._entries.Add(new Entry
            {
                worldPos = worldPos,
                text = text,
                color = color,
                bornAt = Time.unscaledTime,
                lifetime = 0.85f,
                scale = scale,
                driftX = Random.Range(-0.35f, 0.35f)
            });
        }

        private void Awake()
        {
            _instance = this;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                if (now - _entries[i].bornAt > _entries[i].lifetime) _entries.RemoveAt(i);
            }
        }

        private void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null || _entries.Count == 0) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            float dpi = Mathf.Max(1f, Screen.height / 720f);
            float now = Time.unscaledTime;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry e = _entries[i];
                float t = Mathf.Clamp01((now - e.bornAt) / e.lifetime);

                Vector3 world = e.worldPos + new Vector3(e.driftX * t, 1.3f * t, 0f);
                Vector3 screen = cam.WorldToScreenPoint(world);
                if (screen.z <= 0f) continue;

                _style.fontSize = Mathf.RoundToInt(22f * e.scale * dpi);
                Color c = e.color;
                c.a = 1f - t * t;

                var rect = new Rect(screen.x - 90f, Screen.height - screen.y - 20f, 180f, 40f);

                // Cheap drop shadow so numbers stay readable over any background.
                Color prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, c.a * 0.6f);
                GUI.Label(new Rect(rect.x + 2f * dpi, rect.y + 2f * dpi, rect.width, rect.height), e.text, _style);
                GUI.color = c;
                GUI.Label(rect, e.text, _style);
                GUI.color = prev;
            }
        }
    }
}
