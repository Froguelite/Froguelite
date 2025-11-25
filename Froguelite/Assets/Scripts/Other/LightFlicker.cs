using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightFlicker : MonoBehaviour
{
    [Header("Light Reference")]
    [SerializeField] private Light2D light2D;

    [Header("Intensity Flickering")]
    [SerializeField] private bool flickerIntensity = true;
    [SerializeField] private float minIntensityScale = 0.5f;
    [SerializeField] private float maxIntensityScale = 1.0f;
    [SerializeField] private float intensitySpeed = 1.0f;

    [Header("Outer Radius Flickering")]
    [SerializeField] private bool flickerOuterRadius = true;
    [SerializeField] private float minOuterRadiusScale = 0.8f;
    [SerializeField] private float maxOuterRadiusScale = 1.2f;
    [SerializeField] private float radiusSpeed = 0.8f;

    [Header("Advanced Settings")]
    [SerializeField] private float noiseScale = 1.0f;
    [SerializeField] private bool useMultipleNoiseOctaves = true;

    [Header("Rainbow Mode")]
    [SerializeField] private bool enableRainbowMode = false;
    [SerializeField] private float rainbowSpeed = 1.0f;

    // Random offsets to ensure each instance has unique flickering
    private float intensityNoiseOffset;
    private float radiusNoiseOffset;
    private float secondaryNoiseOffset;
    private float rainbowOffset;

    private float baseIntensity;
    private float baseOuterRadius;
    private Color baseColor;

    void Start()
    {
        // Try to get Light2D if not assigned
        if (light2D == null)
        {
            light2D = GetComponent<Light2D>();
            if (light2D == null)
            {
                Debug.LogError("LightFlicker: No Light2D component found! Please assign a Light2D.", this);
                enabled = false;
                return;
            }
        }

        // Store base values
        baseIntensity = light2D.intensity;
        baseOuterRadius = light2D.pointLightOuterRadius;
        baseColor = light2D.color;

        // Generate random offsets for this instance (large range to ensure uniqueness)
        intensityNoiseOffset = Random.Range(0f, 1000f);
        radiusNoiseOffset = Random.Range(0f, 1000f);
        secondaryNoiseOffset = Random.Range(0f, 1000f);
        rainbowOffset = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (light2D == null) return;

        float time = Time.time;

        // Flicker intensity using layered Perlin noise for organic randomness
        if (flickerIntensity)
        {
            float noise = GetFlickerValue(time * intensitySpeed, intensityNoiseOffset);
            float minIntensity = baseIntensity * minIntensityScale;
            float maxIntensity = baseIntensity * maxIntensityScale;
            light2D.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
        }

        // Flicker outer radius
        if (flickerOuterRadius)
        {
            float noise = GetFlickerValue(time * radiusSpeed, radiusNoiseOffset);
            float minOuterRadius = baseOuterRadius * minOuterRadiusScale;
            float maxOuterRadius = baseOuterRadius * maxOuterRadiusScale;
            light2D.pointLightOuterRadius = Mathf.Lerp(minOuterRadius, maxOuterRadius, noise);
        }

        // Rainbow mode
        if (enableRainbowMode)
        {
            float hue = Mathf.Repeat((time * rainbowSpeed + rainbowOffset) * 0.1f, 1f);
            light2D.color = Color.HSVToRGB(hue, 1f, 1f);
        }
        else
        {
            // Reset to base color if rainbow mode is disabled
            if (light2D.color != baseColor)
            {
                light2D.color = baseColor;
            }
        }
    }

    /// <summary>
    /// Generates a natural-looking flicker value using Perlin noise.
    /// Uses multiple octaves for more complex, non-uniform patterns.
    /// </summary>
    private float GetFlickerValue(float time, float offset)
    {
        float primaryNoise = Mathf.PerlinNoise((time + offset) * noiseScale, 0f);

        if (useMultipleNoiseOctaves)
        {
            // Add secondary noise at different frequency for complexity
            float secondaryNoise = Mathf.PerlinNoise((time * 2.3f + offset + secondaryNoiseOffset) * noiseScale, 1f) * 0.3f;
            
            // Add tertiary noise for subtle variations
            float tertiaryNoise = Mathf.PerlinNoise((time * 4.7f + offset) * noiseScale, 2f) * 0.15f;

            // Combine the noise layers
            float combined = primaryNoise * 0.55f + secondaryNoise + tertiaryNoise;
            
            return Mathf.Clamp01(combined);
        }

        return primaryNoise;
    }

    // Optional: Reset to base values
    public void ResetToBaseValues()
    {
        if (light2D != null)
        {
            light2D.intensity = baseIntensity;
            light2D.pointLightOuterRadius = baseOuterRadius;
            light2D.color = baseColor;
        }
    }

    void OnValidate()
    {
        // Ensure min scale values don't exceed max scale values
        if (minIntensityScale > maxIntensityScale)
            minIntensityScale = maxIntensityScale;
        
        if (minOuterRadiusScale > maxOuterRadiusScale)
            minOuterRadiusScale = maxOuterRadiusScale;
    }
}
