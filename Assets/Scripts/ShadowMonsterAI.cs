using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace AetherBlade
{
    
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public class ShadowMonsterAI : MonoBehaviour, IDamageable
    {
        
        private enum State { Idle, Patrol, Chase, Attack, Hurt, Death }

        [Header("Vida")]
        [SerializeField] private int maxHealth = 180;
        [SerializeField] private float invincibilityTime = 0.4f;

        [Header("Detección")]
        [SerializeField] private float detectRadius  = 8f;
        [SerializeField] private float loseRadius    = 14f;
        [SerializeField] private float attackRadius  = 1.6f;
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Movimiento")]
        [SerializeField] private float patrolSpeed = 1.8f;
        [SerializeField] private float chaseSpeed  = 4.5f;
        [SerializeField] private float patrolRange = 5f;

        [Header("Ataque")]
        [SerializeField] private int   attackDamage  = 20;
        [SerializeField] private float attackCooldown = 1.2f;
        [SerializeField] private float attackHitDelay = 0.3f;

        [Header("VFX")]
        [SerializeField] private GameObject[] eyeObjects;      
                [SerializeField] private ParticleSystem hurtVFX;
        [SerializeField] private ParticleSystem deathVFX;
        [SerializeField] private Material shadowBodyMaterial;  
        [Header("Audio")]
        [SerializeField] private AudioClip[] hurtSounds;
        [SerializeField] private AudioClip   deathSound;
        [SerializeField] private AudioClip   attackSound;

        
        private State currentState = State.Idle;
        private int   currentHealth;
        private bool  isInvincible;

        private Rigidbody2D rb;
        private Animator    anim;
        private SpriteRenderer sr;
        private AudioSource audioSource;

        private Transform  player;
        private Vector2    patrolTarget;
        private Vector2    patrolOrigin;
        private float      lastAttackTime = -999f;
        private float      lastHurtTime   = -999f;

      
        private static readonly int HashState      = Animator.StringToHash("State");
        private static readonly int HashIsMoving   = Animator.StringToHash("IsMoving");
        private static readonly int HashAttack     = Animator.StringToHash("Attack");
        private static readonly int HashHurt       = Animator.StringToHash("Hurt");
        private static readonly int HashDie        = Animator.StringToHash("Die");

     

        private void Awake()
        {
            rb          = GetComponent<Rigidbody2D>();
            anim        = GetComponent<Animator>();
            sr          = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();

            currentHealth = maxHealth;
        }

        private void Start()
        {
           
            patrolOrigin = transform.position;
            SetNewPatrolTarget();

            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null) player = playerGO.transform;

            StartCoroutine(EyeBlinkRoutine());
        }

    

        private void Update()
        {
            if (currentState == State.Death) return;

            switch (currentState)
            {
                case State.Idle:   UpdateIdle();   break;
                case State.Patrol: UpdatePatrol(); break;
                case State.Chase:  UpdateChase();  break;
                case State.Attack: break; // controlado por animación
                case State.Hurt:   break;
            }
        }

 

        private void UpdateIdle()
        {
            if (CanDetectPlayer()) TransitionTo(State.Chase);
            else if (Random.value < 0.005f) TransitionTo(State.Patrol);
        }

        private void UpdatePatrol()
        {
            if (CanDetectPlayer()) { TransitionTo(State.Chase); return; }

            Vector2 toTarget = patrolTarget - (Vector2)transform.position;
            if (toTarget.magnitude < 0.3f)
            {
                SetNewPatrolTarget();
                return;
            }

            rb.linearVelocity = new Vector2(Mathf.Sign(toTarget.x) * patrolSpeed, rb.linearVelocity.y);
            FlipToward(toTarget.x);
            if (anim != null) anim.SetBool(HashIsMoving, true);
        }

        private void UpdateChase()
        {
            if (player == null) { TransitionTo(State.Idle); return; }

            float distToPlayer = Vector2.Distance(transform.position, player.position);

            if (distToPlayer > loseRadius)      { TransitionTo(State.Patrol); return; }
            if (distToPlayer <= attackRadius)    { TryAttack(); return; }

            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);
            FlipToward(direction.x);
            if (anim != null) anim.SetBool(HashIsMoving, true);
        }



        private void TransitionTo(State newState)
        {
            currentState = newState;
            rb.linearVelocity  = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetBool(HashIsMoving, false);
            if (anim != null) anim.SetInteger(HashState, (int)newState);
        }


        private void TryAttack()
        {
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;
            TransitionTo(State.Attack);
            if (anim != null) anim.SetTrigger(HashAttack);
            if (attackSound != null) audioSource.PlayOneShot(attackSound);
            StartCoroutine(AttackHitRoutine());
        }

        private IEnumerator AttackHitRoutine()
        {
            yield return new WaitForSeconds(attackHitDelay);

            if (currentState == State.Death) yield break;
            if (player == null) yield break;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist <= attackRadius + 0.5f)
            {
                if (player.TryGetComponent<IDamageable>(out var d))
                    d.TakeDamage(attackDamage, transform.position);
            }

            yield return new WaitForSeconds(attackCooldown * 0.5f);
            if (currentState == State.Attack) TransitionTo(State.Chase);
        }

        // ─── IDamageable ──────────────────────────────────────────

        public void TakeDamage(int amount, Vector2 sourcePosition)
        {
            
            if (currentState == State.Death) return;
            if (isInvincible) return;
            if (amount <= 0) return;

           Debug.Log($"Monstruo recibió {amount} daño desde {sourcePosition} — llamado por: {new System.Diagnostics.StackTrace()}");

            currentHealth -= amount;
            lastHurtTime   = Time.time;

            if (hurtVFX != null) hurtVFX.Play();
            PlayRandomHurtSound();
            StartCoroutine(InvincibilityRoutine());

         
            Vector2 knockDir = ((Vector2)transform.position - sourcePosition).normalized;
            rb.AddForce(knockDir * 5f, ForceMode2D.Impulse);

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            if (anim != null) anim.SetTrigger(HashHurt);
            TransitionTo(State.Hurt);
            StartCoroutine(RecoverFromHurt());
            FlashEyes(Color.white);
            
        }

        private IEnumerator RecoverFromHurt()
        {
            yield return new WaitForSeconds(0.5f);
            if (currentState == State.Hurt) TransitionTo(State.Chase);
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            yield return new WaitForSeconds(invincibilityTime);
            isInvincible = false;
        }

        private void Die()
        {
            currentState   = State.Death;
            isInvincible   = false;
            rb.linearVelocity = Vector2.zero;
            rb.isKinematic = true;
            StopAllCoroutines();  
            if (anim != null) anim.SetTrigger(HashDie);
            if (deathVFX   != null) deathVFX.Play();
            if (deathSound != null && audioSource != null) audioSource.PlayOneShot(deathSound);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            Destroy(gameObject, 3f);
        }

    

        private IEnumerator EyeBlinkRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(1.5f, 5f));
                if (eyeObjects == null || eyeObjects.Length == 0) continue;
                int eyeIndex = Random.Range(0, eyeObjects.Length);
                var eye = eyeObjects[eyeIndex];
                if (eye == null) continue;
                eye.SetActive(false);
                yield return new WaitForSeconds(0.08f);
                eye.SetActive(true);
            }
        }

        private void FlashEyes(Color color)
        {
            if (eyeObjects == null || eyeObjects.Length == 0) return;
            foreach (var eye in eyeObjects)
            {
                if (eye == null) continue;
                if (eye.TryGetComponent<SpriteRenderer>(out var esr))
                    StartCoroutine(FlashEyeRoutine(esr, color));
            }
        }

        private IEnumerator FlashEyeRoutine(SpriteRenderer esr, Color flashColor)
        {
            Color original = esr.color;
            esr.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            esr.color = original;
        }

   

        private bool CanDetectPlayer()
        {
            if (player == null) return false;
            float dist = Vector2.Distance(transform.position, player.position);
            return dist <= detectRadius;
        }

        private void FlipToward(float dirX)
        {
            if (dirX > 0) transform.localScale = new Vector3( 1, 1, 1);
            else          transform.localScale = new Vector3(-1, 1, 1);
        }

        private void SetNewPatrolTarget()
        {
            float offset = Random.Range(-patrolRange, patrolRange);
            patrolTarget = patrolOrigin + new Vector2(offset, 0);
        }

        private void PlayRandomHurtSound()
        {
            if (hurtSounds == null || hurtSounds.Length == 0) return;
            audioSource.PlayOneShot(hurtSounds[Random.Range(0, hurtSounds.Length)]);
        }



        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRadius);
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, loseRadius);
        }
    }
}
