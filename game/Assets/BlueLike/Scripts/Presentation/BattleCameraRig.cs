using UnityEngine;
using BlueLike.Battle;

namespace BlueLike.Presentation
{
    /// <summary>
    /// Quarter-view camera that tracks the front line and swings in for EX skills.
    /// This is the single biggest "feel" lever in the game - tune the numbers here before
    /// you touch anything else.
    /// </summary>
    public class BattleCameraRig : MonoBehaviour
    {
        public static BattleCameraRig Instance { get; private set; }

        [Header("Default framing")]
        [Tooltip("Offset from the action centre in world space.")]
        public Vector3 BaseOffset = new Vector3(-3.5f, 6.5f, -11f);
        [Tooltip("Downward pitch of the camera in degrees.")]
        public float BasePitch = 22f;
        public float BaseFov = 42f;
        [Tooltip("Higher follows the front line more tightly.")]
        public float FollowSharpness = 2.5f;

        [Header("EX framing")]
        public Vector3 ExOffset = new Vector3(-1.2f, 2.4f, -5.0f);
        public float ExPitch = 8f;
        public float ExFov = 30f;
        public float ExSharpness = 7f;

        private Camera _camera;
        private bool _exActive;
        private Transform _exSubject;
        private Vector3 _lookTarget;

        private void Awake()
        {
            Instance = this;
            _camera = GetComponent<Camera>();
            if (_camera == null) _camera = gameObject.AddComponent<Camera>();
            _camera.fieldOfView = BaseFov;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void LateUpdate()
        {
            BattleManager mgr = BattleManager.Instance;
            if (mgr == null) return;

            // Real time so the EX slow-motion beat does not make the camera crawl.
            float dt = Time.unscaledDeltaTime;

            Vector3 focus;
            Vector3 offset;
            float pitch;
            float fov;
            float sharpness;

            if (_exActive && _exSubject != null)
            {
                focus = _exSubject.position + Vector3.up * 1.1f;
                offset = ExOffset;
                pitch = ExPitch;
                fov = ExFov;
                sharpness = ExSharpness;
            }
            else
            {
                focus = mgr.ActionCentre + Vector3.up * 1.0f;
                offset = BaseOffset;
                pitch = BasePitch;
                fov = BaseFov;
                sharpness = FollowSharpness;
            }

            _lookTarget = Vector3.Lerp(_lookTarget, focus, 1f - Mathf.Exp(-sharpness * dt));

            Vector3 desired = _lookTarget + offset;
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-sharpness * dt));

            Quaternion desiredRot = Quaternion.Euler(pitch, 18f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, 1f - Mathf.Exp(-sharpness * dt));

            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, fov, 1f - Mathf.Exp(-sharpness * dt));
        }

        public void BeginExFocus(BattleUnit caster, BattleUnit target)
        {
            _exActive = true;
            _exSubject = caster != null ? caster.transform : null;
        }

        public void EndExFocus()
        {
            _exActive = false;
            _exSubject = null;
        }
    }
}
