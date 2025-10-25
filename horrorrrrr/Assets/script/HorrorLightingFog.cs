using System.Collections;
using UnityEngine;

[AddComponentMenu("Horror/Lighting & Fog Controller")]
public class HorrorLightingFog : MonoBehaviour
{
    [Header("Light (Directional)")]
    public Light sunLight;                      // assign a Directional Light
    public float dayIntensity = 1f;
    public Color dayColor = Color.white;
    public float nightIntensity = 0.15f;
    public Color nightColor = new Color(0.6f, 0.5f, 0.7f);

    [Header("Ambient")]
    public Color ambientDay = Color.white * 0.6f;
    public Color ambientNight = new Color(0.05f, 0.02f, 0.06f);

    [Header("Fog")]
    public bool useFog = true;
    public Color fogDayColor = new Color(0.8f, 0.85f, 0.9f);
    public float fogDayDensity = 0.002f;
    public Color fogNightColor = new Color(0.04f, 0.02f, 0.03f);
    public float fogNightDensity = 0.03f;
    public FogMode fogMode = FogMode.Exponential; // Exponential or Linear

    [Header("Transition")]
    public float transitionDuration = 2.5f;

    [Header("Flicker (optional)")]
    public bool enableFlicker = false;
    public float flickerMin = 0.0f;
    public float flickerMax = 1.5f;
    public float flickerSpeed = 0.05f;
    public int flickerCount = 10;

    [Header("Trigger (optional)")]
    public Transform player;
    public float triggerRadius = 0f; // if > 0, entering triggers night mode
    public bool revertOnExit = false;

    // internal
    Coroutine currentTransition;

    void Reset()
    {
        // sensible defaults
        transitionDuration = 2.5f;
        fogDayDensity = 0.002f;
        fogNightDensity = 0.03f;
        fogMode = FogMode.Exponential;
    }

    void Awake()
    {
        if (sunLight == null)
        {
            // try RenderSettings.sun or find any directional light
            if (RenderSettings.sun != null) sunLight = RenderSettings.sun;
            else
            {
                var l = FindObjectOfType<Light>();
                if (l != null && l.type == LightType.Directional) sunLight = l;
            }
        }

        // apply initial settings to match "day" defaults
        ApplyImmediateDay();
        RenderSettings.fog = useFog;
        RenderSettings.fogMode = fogMode;
    }

    void Update()
    {
        if (player != null && triggerRadius > 0f)
        {
            float d = Vector3.Distance(player.position, transform.position);
            if (d <= triggerRadius)
            {
                ToNight();
            }
            else if (revertOnExit)
            {
                ToDay();
            }
        }
    }

    // Public API
    public void ToNight()
    {
        StartTransition(nightIntensity, nightColor, ambientNight, fogNightColor, fogNightDensity);
    }

    public void ToDay()
    {
        StartTransition(dayIntensity, dayColor, ambientDay, fogDayColor, fogDayDensity);
    }

    public void SetStaticNight()
    {
        StopCurrent();
        ApplyImmediate(nightIntensity, nightColor, ambientNight, fogNightColor, fogNightDensity);
    }

    public void SetStaticDay()
    {
        StopCurrent();
        ApplyImmediate(dayIntensity, dayColor, ambientDay, fogDayColor, fogDayDensity);
    }

    public void StartFlickerOnce()
    {
        if (sunLight != null) StartCoroutine(FlickerRoutine());
    }

    // internal helpers
    void StartTransition(float targetIntensity, Color targetColor, Color targetAmbient, Color targetFogColor, float targetFogDensity)
    {
        StopCurrent();
        currentTransition = StartCoroutine(TransitionRoutine(targetIntensity, targetColor, targetAmbient, targetFogColor, targetFogDensity, transitionDuration));
    }

    void StopCurrent()
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = null;
    }

    IEnumerator TransitionRoutine(float targetIntensity, Color targetColor, Color targetAmbient, Color targetFogColor, float targetFogDensity, float duration)
    {
        float t = 0f;
        float startIntensity = sunLight != null ? sunLight.intensity : targetIntensity;
        Color startColor = sunLight != null ? sunLight.color : targetColor;
        Color startAmbient = RenderSettings.ambientLight;
        Color startFogColor = RenderSettings.fogColor;
        float startFogDensity = RenderSettings.fogDensity;

        RenderSettings.fog = useFog;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            if (sunLight != null)
            {
                sunLight.intensity = Mathf.Lerp(startIntensity, targetIntensity, k);
                sunLight.color = Color.Lerp(startColor, targetColor, k);
            }

            RenderSettings.ambientLight = Color.Lerp(startAmbient, targetAmbient, k);

            if (useFog)
            {
                RenderSettings.fogColor = Color.Lerp(startFogColor, targetFogColor, k);
                RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, targetFogDensity, k);
                // For linear fog you might prefer to lerp start/end distances; this uses density for exponential modes.
            }

            yield return null;
        }

        // ensure final values
        if (sunLight != null)
        {
            sunLight.intensity = targetIntensity;
            sunLight.color = targetColor;
        }
        RenderSettings.ambientLight = targetAmbient;
        if (useFog)
        {
            RenderSettings.fogColor = targetFogColor;
            RenderSettings.fogDensity = targetFogDensity;
        }
        currentTransition = null;
    }

    IEnumerator FlickerRoutine()
    {
        if (sunLight == null) yield break;

        float original = sunLight.intensity;
        int count = Mathf.Max(1, flickerCount);
        for (int i = 0; i < count; i++)
        {
            float next = Random.Range(flickerMin, flickerMax);
            float elapsed = 0f;
            while (elapsed < flickerSpeed)
            {
                elapsed += Time.deltaTime;
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, next, elapsed / flickerSpeed);
                yield return null;
            }
            yield return new WaitForSeconds(Random.Range(0.02f, 0.25f));
        }
        // restore
        float restoreTime = Mathf.Clamp(flickerSpeed * 2f, 0.05f, 1f);
        float e = 0f;
        float start = sunLight.intensity;
        while (e < restoreTime)
        {
            e += Time.deltaTime;
            sunLight.intensity = Mathf.Lerp(start, original, e / restoreTime);
            yield return null;
        }
        sunLight.intensity = original;
    }

    void ApplyImmediate(float intensity, Color color, Color ambient, Color fogColor, float fogDensity)
    {
        if (sunLight != null)
        {
            sunLight.intensity = intensity;
            sunLight.color = color;
        }
        RenderSettings.ambientLight = ambient;
        if (useFog)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fog = true;
        }
    }

    void ApplyImmediateDay() => ApplyImmediate(dayIntensity, dayColor, ambientDay, fogDayColor, fogDayDensity);

    // editor gizmo for trigger radius
    void OnDrawGizmosSelected()
    {
        if (triggerRadius > 0f)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
        }
    }
}