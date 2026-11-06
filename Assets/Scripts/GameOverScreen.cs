using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace AetherBlade
{
    public class GameOverScreen : MonoBehaviour
    {
        public static GameOverScreen Instance { get; private set; }

        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Button restartButton;
        [SerializeField] private float fadeInDuration = 1.2f;
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            if (panel != null) panel.SetActive(false);
        }

        public void Show()
        {
            if (panel != null) panel.SetActive(true);
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;
            canvasGroup.alpha = 0f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / fadeInDuration;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
