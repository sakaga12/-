using UnityEngine;

namespace BlueLike.Setup
{
    /// <summary>Slow idle spin so the halo has a little life to it.</summary>
    public class HaloSpin : MonoBehaviour
    {
        public float DegreesPerSecond = 28f;

        private void Update()
        {
            transform.Rotate(Vector3.up, DegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
