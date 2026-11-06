using UnityEngine;

namespace AetherBlade
{
    public class CameraFollow : MonoBehaviour
    {
        [Header("Objetivo")]
        [SerializeField] private Transform target;         

        [Header("Suavidad")]
        [SerializeField] private float smoothSpeedX = 5f;
        [SerializeField] private float smoothSpeedY = 3f;

        [Header("Offset")]
        [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

        [Header("Límites de cámara (opcional)")]
        [SerializeField] private bool useLimits = false;
        [SerializeField] private float minY = -5f;
        [SerializeField] private float maxY = 10f;

        private void LateUpdate()
        {
            if (target == null) return;

            float targetX = target.position.x + offset.x;
            float targetY = target.position.y + offset.y;

            if (useLimits)
                targetY = Mathf.Clamp(targetY, minY, maxY);

            float newX = Mathf.Lerp(transform.position.x, targetX, smoothSpeedX * Time.deltaTime);
            float newY = Mathf.Lerp(transform.position.y, targetY, smoothSpeedY * Time.deltaTime);

            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }
}
