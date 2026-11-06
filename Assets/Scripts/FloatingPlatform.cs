using UnityEngine;

namespace AetherBlade
{
    public class FloatingPlatform : MonoBehaviour
    {
        public enum MoveAxis { None, Horizontal, Vertical }

        [Header("Movimiento")]
        [SerializeField] private MoveAxis moveAxis    = MoveAxis.None;
        [SerializeField] private float    moveRange   = 2f;
        [SerializeField] private float    moveSpeed   = 1.5f;

        [Header("Bob suave")]
        [SerializeField] private bool  bobbing     = true;
        [SerializeField] private float bobAmount   = 0.08f;
        [SerializeField] private float bobSpeed    = 1.2f;

        private Vector3 startPos;
        private float   seed;

        private void Start()
        {
            startPos = transform.position;
            seed     = Random.Range(0f, 100f);
        }

        private void Update()
        {
            Vector3 pos = startPos;
            if (moveAxis == MoveAxis.Horizontal)
                pos.x += Mathf.Sin(Time.time * moveSpeed + seed) * moveRange;
            else if (moveAxis == MoveAxis.Vertical)
                pos.y += Mathf.Sin(Time.time * moveSpeed + seed) * moveRange;
            if (bobbing)
                pos.y += Mathf.Sin(Time.time * bobSpeed + seed) * bobAmount;

            transform.position = pos;
        }

        private void OnCollisionStay2D(Collision2D col)
        {
            if (moveAxis == MoveAxis.None) return;
            if (!col.gameObject.CompareTag("Player")) return;

            if (moveAxis == MoveAxis.Horizontal)
            {
                float delta = Mathf.Cos(Time.time * moveSpeed + seed) * moveRange * moveSpeed * Time.deltaTime;
                col.transform.position += new Vector3(delta, 0, 0);
            }
        }
    }
}
