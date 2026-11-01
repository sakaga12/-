using UnityEngine;

namespace BlueLike.Characters
{
    /// <summary>
    /// Feeds the head's world-space forward and right vectors into the face material so the
    /// SDF face shadow knows which way the character is looking.
    ///
    /// Without this, the face shadow will not move when the character turns. Attach it to the
    /// character root, point <see cref="HeadBone"/> at the head, and <see cref="FaceRenderer"/>
    /// at the renderer whose material has "Is Face Material" switched on.
    /// </summary>
    public class FaceDirectionBinder : MonoBehaviour
    {
        [Tooltip("The head bone. Leave empty to auto-find it from a humanoid Animator.")]
        public Transform HeadBone;

        [Tooltip("Renderer carrying the face material. Leave empty to search children.")]
        public Renderer FaceRenderer;

        [Tooltip("Index of the face material inside the renderer. Usually 0.")]
        public int MaterialIndex = 0;

        [Tooltip("Rotate the forward vector if your model's head bone does not face +Z.")]
        public Vector3 ForwardEuler = Vector3.zero;

        private static readonly int FaceForwardId = Shader.PropertyToID("_FaceForward");
        private static readonly int FaceRightId = Shader.PropertyToID("_FaceRight");

        private Material _material;

        private void Start()
        {
            if (HeadBone == null)
            {
                var animator = GetComponentInChildren<Animator>();
                if (animator != null && animator.isHuman)
                {
                    HeadBone = animator.GetBoneTransform(HumanBodyBones.Head);
                }
            }
            if (HeadBone == null) HeadBone = transform;

            if (FaceRenderer == null) FaceRenderer = FindFaceRenderer();
            if (FaceRenderer == null)
            {
                Debug.LogWarning("[FaceDirectionBinder] No face renderer found on " + name +
                                 ". SDF face shadows will not update.", this);
                enabled = false;
                return;
            }

            // Materials are instanced on purpose: each character needs its own face vectors,
            // and these properties live in the per-material constant buffer.
            Material[] materials = FaceRenderer.materials;
            int index = Mathf.Clamp(MaterialIndex, 0, materials.Length - 1);
            _material = materials[index];
        }

        private Renderer FindFaceRenderer()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Material[] mats = renderers[i].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null) continue;
                    if (mats[m].HasProperty("_IsFace") && mats[m].GetFloat("_IsFace") > 0.5f)
                    {
                        MaterialIndex = m;
                        return renderers[i];
                    }
                }
            }
            return null;
        }

        private void LateUpdate()
        {
            if (_material == null || HeadBone == null) return;

            Quaternion correction = Quaternion.Euler(ForwardEuler);
            Vector3 forward = HeadBone.rotation * correction * Vector3.forward;
            Vector3 right = HeadBone.rotation * correction * Vector3.right;

            _material.SetVector(FaceForwardId, new Vector4(forward.x, forward.y, forward.z, 0f));
            _material.SetVector(FaceRightId, new Vector4(right.x, right.y, right.z, 0f));
        }
    }
}
