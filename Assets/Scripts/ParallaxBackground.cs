using UnityEngine;

namespace AetherBlade
{
      public class ParallaxBackground : MonoBehaviour
    {
        [Header("Parallax")]
        [SerializeField] private float parallaxFactor = 0.5f;

        [Header("Repetición")]
        [SerializeField] private bool  repeatInfinitely = true;
        [SerializeField] private float spriteWorldWidth = 9f; // ancho del sprite en unidades Unity

        private Transform cam;
        private Vector3   lastCamPos;

        private void Start()
        {
            cam        = Camera.main.transform;
            lastCamPos = cam.position;
        }

        private void LateUpdate()
        {
            Vector3 delta = cam.position - lastCamPos;
            transform.position += new Vector3(delta.x * parallaxFactor, 0, 0);
            lastCamPos = cam.position;

            if (!repeatInfinitely || spriteWorldWidth <= 0) return;

            float distX = cam.position.x - transform.position.x;

            if (distX > spriteWorldWidth)
                transform.position += new Vector3(spriteWorldWidth * 2f, 0, 0);
            else if (distX < -spriteWorldWidth)
                transform.position -= new Vector3(spriteWorldWidth * 2f, 0, 0);
        }
    }
}
