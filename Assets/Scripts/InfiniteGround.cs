using UnityEngine;

namespace AetherBlade
{
    public class InfiniteGround : MonoBehaviour
    {
        [Header("Cámara")]
        [SerializeField] private Transform cameraTransform;

        [Header("Configuración")]
        [SerializeField] private float groundY = -2f;
        [SerializeField] private bool  lockY   = true;

        private void Start()
        {
            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.drawMode = SpriteDrawMode.Tiled;
                sr.size = new Vector2(200f, sr.size.y);
            }
            var col = GetComponent<BoxCollider2D>();
            if (col != null)
                col.size = new Vector2(200f, col.size.y);
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            float newX = cameraTransform.position.x;
            float newY = lockY ? groundY : transform.position.y;
            transform.position = new Vector3(newX, newY, transform.position.z);
        }
    }
}
