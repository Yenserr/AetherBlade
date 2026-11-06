using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

namespace AetherBlade
{

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class HeroController : MonoBehaviour, IDamageable
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpForce = 7f;
        [SerializeField] private float fallMultiplier = 4f;
        [SerializeField] private float lowJumpMultiplier = 2f;

        [Header("Doble Salto")]
        [SerializeField] private int maxJumps = 2;
        [SerializeField] private ParticleSystem doubleJumpVFX;
        [SerializeField] private float doubleJumpAuraRadius = 1.5f;

        [Header("Rastro Etéreo")]
        [SerializeField] private TrailRenderer etherealTrail;
        [SerializeField] private Color trailColorStart = new Color(0.27f, 0.53f, 1f, 0.9f);
        [SerializeField] private Color trailColorEnd   = new Color(0.27f, 0.53f, 1f, 0f);
        [SerializeField] private float trailTimeGround = 0f;
        [SerializeField] private float trailTimeAir    = 0.35f;

        [Header("Detección de suelo")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.15f;

        [Header("Vida")]
        [SerializeField] private int   maxHealth       = 100;
        [SerializeField] private float invincibilityTime = 0.8f;
        [SerializeField] private float knockbackForce  = 4f;

        [Header("Magia")]
        [SerializeField] private int maxMana  = 100;
        [SerializeField] private int currMana = 80;

        private Rigidbody2D rb;
        private Animator    anim;
        private SpriteRenderer sr;

        private int   currentHealth;
        private bool  isInvincible;
        private bool  isDead;

        private int   jumpsRemaining;
        private bool  isGrounded;
        private bool  facingRight = true;
        private float moveInput;

        private static readonly int HashSpeed      = Animator.StringToHash("Speed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashJump       = Animator.StringToHash("Jump");
        private static readonly int HashDoubleJump = Animator.StringToHash("DoubleJump");
        private static readonly int HashAttack     = Animator.StringToHash("Attack");
        private static readonly int HashHurt       = Animator.StringToHash("Hurt");
        private static readonly int HashDie        = Animator.StringToHash("Die");

        private void Awake()
        {
            rb   = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            sr   = GetComponent<SpriteRenderer>();

            currentHealth = maxHealth;

            if (etherealTrail != null)
            {
                etherealTrail.startColor = trailColorStart;
                etherealTrail.endColor   = trailColorEnd;
                etherealTrail.time       = trailTimeGround;
            }
        }

        private void Start()
        {
    
            HUDManager.Instance?.SetHealth(1f);
            HUDManager.Instance?.SetMana((float)currMana / maxMana);
        }

        private void Update()
        {
            if (isDead) return;
            ReadInput();
            HandleFlip();
            HandleJump();
            HandleAttack();
            UpdateAnimator();
            UpdateTrail();
        }

        private void FixedUpdate()
        {
            if (isDead) return;
            CheckGround();
            Move();
            ApplyBetterGravity();
        }



        private void ReadInput()
        {
            moveInput = 0f;
            if (Keyboard.current == null) return;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                moveInput = -1f;
            else if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                moveInput = 1f;
        }


        private void Move()
        {
            rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
        }

        private void HandleFlip()
        {
            if (moveInput > 0 && !facingRight) Flip();
            else if (moveInput < 0 && facingRight) Flip();
        }

        private void Flip()
        {
            facingRight = !facingRight;
            if (sr != null) sr.flipX = !sr.flipX;
        }


        private void CheckGround()
        {
            bool wasGrounded = isGrounded;
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            if (isGrounded && !wasGrounded)
                jumpsRemaining = maxJumps;
        }

        private void HandleJump()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.spaceKey.wasPressedThisFrame &&
                !Keyboard.current.upArrowKey.wasPressedThisFrame) return;
            if (jumpsRemaining <= 0) return;

            bool isDoubleJump = jumpsRemaining < maxJumps;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpsRemaining--;

            if (isDoubleJump)
            {
                anim.SetTrigger(HashDoubleJump);
                if (doubleJumpVFX != null) doubleJumpVFX.Play();
            }
            else
            {
                anim.SetTrigger(HashJump);
            }
        }

        private void ApplyBetterGravity()
        {
            if (rb.linearVelocity.y < 0)
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            else if (rb.linearVelocity.y > 0 && (Keyboard.current == null ||
                     (!Keyboard.current.spaceKey.isPressed && !Keyboard.current.upArrowKey.isPressed)))
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }



        private void UpdateTrail()
        {
            if (etherealTrail == null) return;
            etherealTrail.time = isGrounded ? trailTimeGround : trailTimeAir;
        }

  

        [Header("Ataque directo")]
        [SerializeField] private float attackRange  = 2.5f;
        [SerializeField] private int   attackDamage = 25;
        [SerializeField] private LayerMask enemyLayer;
        private float lastAttackTime = -999f;
        private float attackCooldown = 0.4f;

        private void HandleAttack()
        {
            if (Keyboard.current == null) return;
            if (!Keyboard.current.zKey.wasPressedThisFrame) return;
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;
            anim.SetTrigger(HashAttack);
            StartCoroutine(AttackHitRoutine());
        }

        private IEnumerator AttackHitRoutine()
        {
            yield return new WaitForSeconds(0.1f);
            Vector2 origin    = (Vector2)transform.position + Vector2.up * 0.5f;
            Vector2 direction = facingRight ? Vector2.right : Vector2.left;
            Vector2 hitCenter = origin + direction * (attackRange * 0.5f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(hitCenter, attackRange * 0.5f, enemyLayer);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(attackDamage, transform.position);
            }
        }


        private void UpdateAnimator()
        {
            anim.SetFloat(HashSpeed,     Mathf.Abs(moveInput));
            anim.SetBool(HashIsGrounded, isGrounded);
        }



        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            if (isInvincible || isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);

       
            HUDManager.Instance?.SetHealth((float)currentHealth / maxHealth);
            HUDManager.Instance?.TriggerDamageFlash();

  
            Vector2 knockDir = ((Vector2)transform.position - sourcePosition).normalized;
            rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            anim.SetTrigger(HashHurt);
            StartCoroutine(InvincibilityRoutine());
            StartCoroutine(FlashRoutine());
        }

        private void Die()
        {
            isDead = true;
            rb.linearVelocity    = Vector2.zero;
            rb.isKinematic = true;
            anim.SetTrigger(HashDie);
            GetComponent<Collider2D>().enabled = false;
            StartCoroutine(ShowGameOverRoutine());
        }

        private IEnumerator ShowGameOverRoutine()
        {
            yield return new WaitForSeconds(1.5f);
            GameOverScreen.Instance?.Show();
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            yield return new WaitForSeconds(invincibilityTime);
            isInvincible = false;
        }


        private IEnumerator FlashRoutine()
        {
            if (sr == null) yield break;
            for (int i = 0; i < 3; i++)
            {
                sr.color = Color.red;
                yield return new WaitForSeconds(0.08f);
                sr.color = Color.white;
                yield return new WaitForSeconds(0.08f);
            }
            sr.color = Color.white;
        }


        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            Gizmos.color = new Color(0.27f, 0.53f, 1f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, doubleJumpAuraRadius);
        }
    }
}
