using System.Collections.Generic;
using UnityEngine;

namespace BlueLike.Presentation
{
    /// <summary>
    /// Pooled bullet tracers.
    ///
    /// Note on why this uses LineRenderer rather than GL lines: URP never calls
    /// OnRenderObject / OnPostRender, so the classic GL.Begin trick draws nothing under a
    /// scriptable render pipeline. Pooled LineRenderers work in every pipeline and batch
    /// well enough for the handful of shots on screen at once.
    /// </summary>
    public class TracerLayer : MonoBehaviour
    {
        private class Tracer
        {
            public LineRenderer line;
            public float bornAt;
            public bool active;
        }

        private const float Lifetime = 0.085f;
        private const int PoolSize = 48;

        private static TracerLayer _instance;
        private readonly List<Tracer> _pool = new List<Tracer>(PoolSize);
        private Material _material;

        private static readonly Color NormalStart = new Color(1f, 0.95f, 0.65f, 1f);
        private static readonly Color NormalEnd = new Color(1f, 0.75f, 0.35f, 0f);
        private static readonly Color ExStart = new Color(1f, 0.55f, 0.85f, 1f);
        private static readonly Color ExEnd = new Color(0.7f, 0.4f, 1f, 0f);

        public static void Spawn(Vector3 from, Vector3 to, bool isEx)
        {
            if (_instance == null)
            {
                var go = new GameObject("TracerLayer");
                _instance = go.AddComponent<TracerLayer>();
            }
            _instance.Emit(from, to, isEx);
        }

        private void Awake()
        {
            _instance = this;

            Shader shader = Shader.Find("BlueLike/UnlitAdditive");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor", Color.white);

            for (int i = 0; i < PoolSize; i++) _pool.Add(CreateTracer(i));
        }

        private Tracer CreateTracer(int index)
        {
            var go = new GameObject("Tracer_" + index);
            go.transform.SetParent(transform, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.numCapVertices = 0;
            line.alignment = LineAlignment.View;
            line.textureMode = LineTextureMode.Stretch;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = _material;
            line.widthMultiplier = 0.045f;
            line.enabled = false;

            return new Tracer { line = line, active = false };
        }

        private void Emit(Vector3 from, Vector3 to, bool isEx)
        {
            Tracer tracer = null;
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].active)
                {
                    tracer = _pool[i];
                    break;
                }
            }
            // Pool exhausted: steal the oldest rather than allocating mid-fight.
            if (tracer == null)
            {
                float oldest = float.MaxValue;
                for (int i = 0; i < _pool.Count; i++)
                {
                    if (_pool[i].bornAt < oldest)
                    {
                        oldest = _pool[i].bornAt;
                        tracer = _pool[i];
                    }
                }
            }
            if (tracer == null) return;

            tracer.line.SetPosition(0, from);
            tracer.line.SetPosition(1, to);
            tracer.line.startColor = isEx ? ExStart : NormalStart;
            tracer.line.endColor = isEx ? ExEnd : NormalEnd;
            tracer.line.widthMultiplier = isEx ? 0.11f : 0.045f;
            tracer.line.enabled = true;
            tracer.bornAt = Time.unscaledTime;
            tracer.active = true;
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            for (int i = 0; i < _pool.Count; i++)
            {
                Tracer t = _pool[i];
                if (!t.active) continue;

                float age = (now - t.bornAt) / Lifetime;
                if (age >= 1f)
                {
                    t.active = false;
                    t.line.enabled = false;
                    continue;
                }

                // Shrink toward the impact point so the tracer reads as travelling.
                float fade = 1f - age;
                Color start = t.line.startColor;
                start.a = fade;
                t.line.startColor = start;
            }
        }
    }
}
