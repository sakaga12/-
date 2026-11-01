using UnityEngine;
using BlueLike.Battle;

namespace BlueLike.Presentation
{
    /// <summary>
    /// Sinks and dims a unit when it dies, instead of popping it out of existence.
    /// Replace with a death animation once your characters have one.
    /// </summary>
    [RequireComponent(typeof(BattleUnit))]
    public class DeathFade : MonoBehaviour
    {
        public float SinkDistance = 1.4f;
        public float Duration = 0.7f;

        private BattleUnit _unit;
        private bool _playing;
        private float _startedAt;
        private Vector3 _startPos;

        private void Awake()
        {
            _unit = GetComponent<BattleUnit>();
            _unit.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_unit != null) _unit.Died -= OnDied;
        }

        private void OnDied(BattleUnit unit)
        {
            _playing = true;
            _startedAt = Time.unscaledTime;
            _startPos = transform.position;
            FloatingTextLayer.Spawn(transform.position + Vector3.up * 2f, "DOWN",
                new Color(1f, 0.4f, 0.4f), 1.1f);
        }

        private void Update()
        {
            if (!_playing) return;
            float t = Mathf.Clamp01((Time.unscaledTime - _startedAt) / Duration);
            transform.position = _startPos + Vector3.down * (SinkDistance * t * t);

            if (_unit.ModelRoot != null)
            {
                _unit.ModelRoot.localRotation = Quaternion.Euler(0f, 0f, 0f);
                _unit.ModelRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.85f, t);
            }

            if (t >= 1f)
            {
                _playing = false;
                if (_unit.ModelRoot != null) _unit.ModelRoot.gameObject.SetActive(false);
            }
        }
    }
}
