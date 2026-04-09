using UnityEngine;

public class WaveHeightProvider : MonoBehaviour
{
    [System.Serializable]
    public class WaveBand
    {
        public bool enabled = true;
        public float amplitude = 0.3f;
        public float wavelength = 2f;
        public float speed = 1f;
    }

    [Header("Swell 涌浪 - Band 1")]
    public WaveBand swellBand1 = new WaveBand { amplitude = 0.5f, wavelength = 5f, speed = 0.8f };

    [Header("Swell 涌浪 - Band 2")]
    public WaveBand swellBand2 = new WaveBand { amplitude = 0.3f, wavelength = 3f, speed = 1.2f };

    [Header("Ripples 涟漪")]
    public WaveBand ripples = new WaveBand { amplitude = 0.1f, wavelength = 1.2f, speed = 2f };

    public float GetWaveHeight(float x, float z)
    {
        float height = 0f;
        float t = Time.time;

        if (swellBand1.enabled)
            height += EvaluateWave(x, z, t, swellBand1);
        if (swellBand2.enabled)
            height += EvaluateWave(x, z, t, swellBand2);
        if (ripples.enabled)
            height += EvaluateWave(x, z, t, ripples);

        return height;
    }

    private float EvaluateWave(float x, float z, float time, WaveBand wave)
    {
        float argX = (x / wave.wavelength) + time * wave.speed;
        float argZ = (z / wave.wavelength) + time * wave.speed;
        return wave.amplitude * Mathf.Sin(argX) * Mathf.Cos(argZ);
    }
}