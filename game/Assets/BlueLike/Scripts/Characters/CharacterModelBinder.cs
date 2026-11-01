using UnityEngine;
using BlueLike.Battle;

namespace BlueLike.Characters
{
    /// <summary>
    /// Swaps a unit's placeholder capsule for a real character model.
    ///
    /// This is the seam between the game and the art. The battle systems never touch a mesh;
    /// they only move the unit transform and read <see cref="BattleUnit.Muzzle"/>. So you can
    /// bring a model in at any time without changing a line of combat code.
    ///
    /// Usage: add this to a unit GameObject, assign ModelPrefab, press play.
    /// </summary>
    [RequireComponent(typeof(BattleUnit))]
    public class CharacterModelBinder : MonoBehaviour
    {
        [Tooltip("Humanoid character prefab, e.g. an imported VRM or FBX.")]
        public GameObject ModelPrefab;

        [Tooltip("Uniform scale applied to the spawned model.")]
        public float ModelScale = 1f;

        [Tooltip("Name of the transform to use as the muzzle. Falls back to the right hand.")]
        public string MuzzleTransformName = "Muzzle";

        [Header("Animation state names")]
        public string IdleState = "Idle";
        public string MoveState = "Move";
        public string FireState = "Fire";
        public string ReloadState = "Reload";
        public string ExState = "ExSkill";
        public string DeathState = "Death";

        private BattleUnit _unit;
        private Animator _animator;
        private Core.UnitState _lastState = Core.UnitState.Idle;

        private void Awake()
        {
            _unit = GetComponent<BattleUnit>();
        }

        private void Start()
        {
            if (ModelPrefab == null) return;

            // Hide whatever placeholder art the bootstrap created.
            if (_unit.ModelRoot != null) _unit.ModelRoot.gameObject.SetActive(false);

            GameObject instance = Instantiate(ModelPrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * ModelScale;

            _unit.ModelRoot = instance.transform;
            _unit.FaceEnemySide();

            _animator = instance.GetComponentInChildren<Animator>();

            Transform muzzle = FindMuzzle(instance.transform);
            if (muzzle != null) _unit.Muzzle = muzzle;

            Renderer renderer = instance.GetComponentInChildren<Renderer>();
            if (renderer != null) _unit.BodyRenderer = renderer;

            // Wire up the SDF face shadow if a face material is present.
            if (GetComponentInChildren<FaceDirectionBinder>() == null)
            {
                instance.AddComponent<FaceDirectionBinder>();
            }
        }

        private Transform FindMuzzle(Transform root)
        {
            Transform named = FindDeep(root, MuzzleTransformName);
            if (named != null) return named;

            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                if (hand != null) return hand;
            }
            return null;
        }

        private static Transform FindDeep(Transform root, string targetName)
        {
            if (string.IsNullOrEmpty(targetName)) return null;
            if (root.name == targetName) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), targetName);
                if (found != null) return found;
            }
            return null;
        }

        private void Update()
        {
            if (_animator == null) return;
            if (_unit.State == _lastState) return;
            _lastState = _unit.State;

            string state = null;
            switch (_unit.State)
            {
                case Core.UnitState.Advancing: state = MoveState; break;
                case Core.UnitState.Firing: state = FireState; break;
                case Core.UnitState.Reloading: state = ReloadState; break;
                case Core.UnitState.CastingEx: state = ExState; break;
                case Core.UnitState.Dead: state = DeathState; break;
                default: state = IdleState; break;
            }

            if (!string.IsNullOrEmpty(state) && HasState(state))
            {
                _animator.CrossFade(state, 0.12f);
            }
        }

        private bool HasState(string stateName)
        {
            // Avoids spamming warnings for clips the model does not ship with.
            return _animator.HasState(0, Animator.StringToHash(stateName));
        }
    }
}
