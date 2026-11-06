using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AetherBlade
{
    public class GlowingMoss : MonoBehaviour
    {
        [Header("Luz")]
        [SerializeField] private Light2D mossLight;
        [SerializeField] private float   minIntensity = 0.3f;
        [SerializeField] private float   maxIntensity = 1.1f;
        [SerializeField] private float   pulseSpeed   = 1.2f;
        [SerializeField] private float   noiseScale   = 0.8f;   

        [Header("Color")]
        [SerializeField] private Color baseColor    = new Color(0f, 1f, 0.53f, 1f);
        [SerializeField] private Color shimmerColor = new Color(0.4f, 1f, 0.7f,  1f);
        [SerializeField] private float colorShimmerSpeed = 0.4f;

        [Header("Sprite")]
        [SerializeField] private SpriteRenderer mossSR;
        [SerializeField] private float          spriteAlphaMin = 0.55f;
        [SerializeField] private float          spriteAlphaMax = 1f;

        
        private float seed;

        private void Awake()
        {
            seed = Random.Range(0f, 100f);
            if (mossLight != null) mossLight.color = baseColor;
        }

        private void Update()
        {
            float t = Time.time * pulseSpeed + seed;

            float noise = Mathf.PerlinNoise(t * noiseScale, seed);
            float intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);

            if (mossLight != null)
            {
                mossLight.intensity = intensity;
      
                float shimmerT = (Mathf.Sin(t * colorShimmerSpeed) + 1f) * 0.5f;
                mossLight.color = Color.Lerp(baseColor, shimmerColor, shimmerT * 0.3f);
            }

    
            if (mossSR != null)
            {
                float alpha = Mathf.Lerp(spriteAlphaMin, spriteAlphaMax, noise);
                Color c = mossSR.color;
                c.a = alpha;
                mossSR.color = c;
            }
        }

 
        public void Flicker(float duration = 0.5f)
        {
            StartCoroutine(FlickerRoutine(duration));
        }

        private IEnumerator FlickerRoutine(float duration)
        {
            if (mossLight == null) yield break;

            float original = mossLight.intensity;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
             
                mossLight.intensity = Random.Range(0f, original * 0.4f);
                yield return new WaitForSeconds(0.04f);
            }
            mossLight.intensity = original;
        }
    }
}
