using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AetherBlade
{

    public class HUDManager : MonoBehaviour
    {
   
        public static HUDManager Instance { get; private set; }

        [Header("Barra de Vida")]
        [SerializeField] private Slider   healthSlider;
        [SerializeField] private Image    healthFill;
        [SerializeField] private Gradient healthGradient;     
        [SerializeField] private Image    healthDelayedFill;    
        [SerializeField] private float    healthDelaySpeed = 2f;

        [Header("Barra de Magia")]
        [SerializeField] private Slider   manaSlider;
        [SerializeField] private Image    manaFill;
        [SerializeField] private Color    manaColor     = new Color(0.2f, 0.4f, 1f, 1f);
        [SerializeField] private Color    manaLowColor  = new Color(0.5f, 0.1f, 0.8f, 1f);
        [SerializeField] private float    manaLowThreshold = 0.2f;

        [Header("Combo")]
        [SerializeField] private GameObject comboPanel;
        [SerializeField] private TextMeshProUGUI comboText;
        [SerializeField] private TextMeshProUGUI comboLabel;              
        [SerializeField] private Animator   comboAnimator;
        [SerializeField] private float      comboClearTime = 2.5f;  

        [Header("Nivel / EXP")]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider expSlider;
        [SerializeField] private TextMeshProUGUI expText;

        [Header("Iconos de habilidad")]
        [SerializeField] private Image[]    abilityIcons;
        [SerializeField] private Image[]    abilityCooldownOverlays;  
      
        private float targetHealthNormalized;
        private float displayedDelayedHealth;

        private int   comboCount;
        private Coroutine comboClearCoroutine;

        private static readonly int HashComboHit  = Animator.StringToHash("Hit");
        private static readonly int HashComboClear = Animator.StringToHash("Clear");

  

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (comboPanel != null) comboPanel.SetActive(false);
            if (manaFill   != null) manaFill.color = manaColor;
        }

      

        private void Update()
        {
         
            if (healthDelayedFill != null)
            {
                float target = healthSlider != null ? healthSlider.value : 0f;
                if (healthDelayedFill.fillAmount > target)
                    healthDelayedFill.fillAmount = Mathf.MoveTowards(
                        healthDelayedFill.fillAmount, target, healthDelaySpeed * Time.deltaTime);
            }
        }

 
        public void SetHealth(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);

            if (healthSlider != null) healthSlider.value = normalized;

            if (healthFill != null)
                healthFill.color = healthGradient.Evaluate(normalized);

      
            if (healthDelayedFill != null && normalized < healthDelayedFill.fillAmount)
            {
             
                if (normalized > healthDelayedFill.fillAmount)
                    healthDelayedFill.fillAmount = normalized;
            }
            else if (healthDelayedFill != null)
            {
                healthDelayedFill.fillAmount = normalized;
            }
        }

       

        public void SetMana(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            if (manaSlider != null) manaSlider.value = normalized;
            if (manaFill   != null) manaFill.color = normalized < manaLowThreshold ? manaLowColor : manaColor;
        }

  

        public void RegisterHit()
        {
            comboCount++;
            if (comboPanel != null) comboPanel.SetActive(true);
            if (comboText  != null) comboText.text = $"x{comboCount}";

            if (comboAnimator != null) comboAnimator.SetTrigger(HashComboHit);

     
            if (comboClearCoroutine != null) StopCoroutine(comboClearCoroutine);
            comboClearCoroutine = StartCoroutine(ComboClearRoutine());
        }

        private IEnumerator ComboClearRoutine()
        {
            yield return new WaitForSeconds(comboClearTime);
            ClearCombo();
        }

        private void ClearCombo()
        {
            comboCount = 0;
            if (comboAnimator != null) comboAnimator.SetTrigger(HashComboClear);
            StartCoroutine(HideComboPanelAfterAnim());
        }

        private IEnumerator HideComboPanelAfterAnim()
        {
            yield return new WaitForSeconds(0.5f);
            if (comboPanel != null) comboPanel.SetActive(false);
        }

  

        public void SetLevel(int level)
        {
            if (levelText != null) levelText.text = $"LVL  {level:D2}";
        }

        public void SetExp(int current, int max)
        {
            if (expSlider != null) expSlider.value = (float)current / max;
            if (expText   != null) expText.text    = $"EXP  {current}/{max}";
        }

       
        public void StartAbilityCooldown(int index, float duration)
        {
            if (index >= abilityCooldownOverlays.Length) return;
            StartCoroutine(CooldownRoutine(index, duration));
        }

        private IEnumerator CooldownRoutine(int index, float duration)
        {
            var overlay = abilityCooldownOverlays[index];
            overlay.gameObject.SetActive(true);
            overlay.fillAmount = 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                overlay.fillAmount = 1f - (elapsed / duration);
                yield return null;
            }
            overlay.fillAmount = 0f;
            overlay.gameObject.SetActive(false);
        }



        [Header("Pantalla Flash")]
        [SerializeField] private Image screenFlash;
        [SerializeField] private Color damageFlashColor = new Color(1, 0, 0, 0.35f);

        public void TriggerDamageFlash()
        {
            StopCoroutine(nameof(FlashRoutine));
            StartCoroutine(FlashRoutine(damageFlashColor, 0.15f));
        }

        private IEnumerator FlashRoutine(Color color, float duration)
        {
            if (screenFlash == null) yield break;
            screenFlash.color = color;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                screenFlash.color = Color.Lerp(color, Color.clear, t);
                yield return null;
            }
        }
    }
}
