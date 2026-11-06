using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;

namespace AetherBlade
{
    
    public class SwordAttack : MonoBehaviour
    {
        [Header("Daño")]
        [SerializeField] private int   baseDamage   = 25;
        [SerializeField] private float attackRange  = 1.8f;
        [SerializeField] private float attackAngle  = 90f;
        [SerializeField] private LayerMask enemyLayer;

        [Header("Combo")]
        [SerializeField] private int   maxComboHits  = 3;
        [SerializeField] private float comboWindow   = 0.6f;   
        [SerializeField] private float[] comboDamageMultipliers = { 1f, 1.2f, 1.8f };

        [Header("Cooldown")]
        [SerializeField] private float attackCooldown = 0.25f;

        [Header("VFX - Espada")]
        [SerializeField] private SpriteRenderer bladeRenderer;
        [SerializeField] private Color bladeIdleColor   = new Color(0.87f, 0.87f, 0.95f, 1f);   
        [SerializeField] private Color bladeAttackColor = new Color(0.53f, 0.73f, 1f,  1f);    
        [SerializeField] private Light2D bladeLight;
        [SerializeField] private float bladeLightIntensityIdle   = 0.4f;
        [SerializeField] private float bladeLightIntensityAttack = 2.2f;
        [SerializeField] private ParticleSystem slashVFX;
        [SerializeField] private ParticleSystem impactSparkVFX;

        [Header("Cámara")]
        [SerializeField] private CinemachineImpulseSource impulseSource;  
    
        private int   comboCount;
        private float lastAttackTime = -999f;
        private float lastHitTime    = -999f;
        private bool  canAttack = true;

        private Transform heroTransform;
        private Animator  anim;
        private HeroController heroCtrl;

        private static readonly int HashAttack = Animator.StringToHash("Attack");

        private void Awake()
        {
            heroTransform = transform;
            anim          = GetComponent<Animator>();
            heroCtrl      = GetComponent<HeroController>();

            if (bladeRenderer != null)
                bladeRenderer.color = bladeIdleColor;
            if (bladeLight != null)
                bladeLight.intensity = bladeLightIntensityIdle;
        }

        private void Update()
        {
          
            if (comboCount > 0 && Time.time - lastAttackTime > comboWindow)
                ResetCombo();
        }

    
        public void OnAttackHitFrame()
        {
            Vector2 origin    = (Vector2)heroTransform.position + Vector2.up * 0.5f;
            Vector2 direction = heroTransform.localScale.x > 0 ? Vector2.right : Vector2.left;

            Collider2D[] hits = Physics2D.OverlapCircleAll(origin + direction * (attackRange * 0.5f),
                                                           attackRange * 0.5f, enemyLayer);

            bool didHit = false;
            foreach (var hit in hits)
            {
                if (hit.gameObject == heroTransform.gameObject) continue;
                if (hit.TryGetComponent<IDamageable>(out var damageable))
                {
                    int safeIndex = Mathf.Clamp(comboCount - 1, 0, comboDamageMultipliers.Length - 1);
                    float multiplier = comboDamageMultipliers[safeIndex];
                    int   finalDamage = Mathf.RoundToInt(baseDamage * multiplier);
                    damageable.TakeDamage(finalDamage, origin);
                    didHit = true;
                }
            }

            if (didHit)
            {
                lastHitTime = Time.time;
                if (impactSparkVFX != null) impactSparkVFX.Play();
                if (impulseSource   != null) impulseSource.GenerateImpulse(0.15f);
                StartCoroutine(FlashBlade());
            }
        }

 
        public void TriggerAttack()
        {
            if (!canAttack) return;
            if (Time.time - lastAttackTime < attackCooldown) return;

            lastAttackTime = Time.time;
            comboCount     = Mathf.Clamp(comboCount + 1, 0, maxComboHits);

            if (slashVFX != null) slashVFX.Play();
            StartCoroutine(AttackCooldownRoutine());
            StartCoroutine(DelayedHit());
        }

        private IEnumerator DelayedHit()
        {
            yield return new WaitForSeconds(0.1f);
            OnAttackHitFrame();
        }

        private IEnumerator AttackCooldownRoutine()
        {
            canAttack = false;
            yield return new WaitForSeconds(attackCooldown);
            canAttack = true;
        }

        private IEnumerator FlashBlade()
        {
            if (bladeRenderer != null) bladeRenderer.color = bladeAttackColor;
            if (bladeLight    != null) bladeLight.intensity = bladeLightIntensityAttack;

            yield return new WaitForSeconds(0.12f);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                if (bladeRenderer != null)
                    bladeRenderer.color = Color.Lerp(bladeAttackColor, bladeIdleColor, t);
                if (bladeLight != null)
                    bladeLight.intensity = Mathf.Lerp(bladeLightIntensityAttack, bladeLightIntensityIdle, t);
                yield return null;
            }
        }


        private void ResetCombo() => comboCount = 0;

        public int GetComboCount() => comboCount;

        private void OnDrawGizmosSelected()
        {
            Vector2 origin    = (Vector2)transform.position + Vector2.up * 0.5f;
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Gizmos.color = new Color(0.53f, 0.73f, 1f, 0.5f);
            Gizmos.DrawWireSphere(origin + direction * (attackRange * 0.5f), attackRange * 0.5f);
        }
    }
}
