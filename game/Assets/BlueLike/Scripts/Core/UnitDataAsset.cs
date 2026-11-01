using UnityEngine;

namespace BlueLike.Core
{
    /// <summary>
    /// Inspector-authorable unit, for when you outgrow building the roster in code.
    /// Create via Assets > Create > BlueLike > Unit Data.
    /// </summary>
    [CreateAssetMenu(menuName = "BlueLike/Unit Data", fileName = "NewUnit")]
    public class UnitDataAsset : ScriptableObject
    {
        public UnitStats stats = new UnitStats();

        [Tooltip("Optional character prefab. Leave empty to use the placeholder capsule.")]
        public GameObject modelPrefab;

        [Tooltip("Placeholder body colour, used when no prefab is assigned.")]
        public Color placeholderColor = Color.white;

        [Tooltip("Halo colour for the placeholder model.")]
        public Color haloColor = Color.white;
    }
}
