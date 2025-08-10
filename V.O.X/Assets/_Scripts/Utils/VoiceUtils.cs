using System;
using UnityEngine;

public static class VoiceUtils
{
    // Standalone YIN pitch detector with explicit parameters; returns Hz, and outputs confidence and dBFS
    public static float CalculatePitchYin(
        ArraySegment<float> audioData,
        int sampleRate,
        float minPitchHz,
        float maxPitchHz,
        float yinThreshold,
        float minDbFs,
        out float confidence,
        out float dbFs)
    {
        confidence = 0f;
        dbFs = -80f;
        if (audioData.Count == 0 || sampleRate <= 0)
            return 0f;

        int n = audioData.Count;
        var x = audioData.ToArray();

        // DC offset removal and RMS
        double sum = 0;
        for (int i = 0; i < n; i++) sum += x[i];
        float mean = (float)(sum / Math.Max(1, n));
        float rms = 0f;
        for (int i = 0; i < n; i++)
        {
            float v = x[i] - mean;
            x[i] = v;
            rms += v * v;
        }
        rms = Mathf.Sqrt(rms / Mathf.Max(1, n));
        dbFs = 20f * Mathf.Log10(Mathf.Max(1e-7f, rms)); // approximately [-inf,0]

        if (dbFs < minDbFs)
            return 0f;

        // Apply Hann window to stabilize analysis
        for (int i = 0; i < n; i++)
        {
            float w = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * i / (n - 1)));
            x[i] *= w;
        }

        // Determine tau search range from min/max pitch
        int maxTau = Mathf.Min(n - 2, Mathf.FloorToInt(sampleRate / Mathf.Max(1f, minPitchHz)));
        int minTau = Mathf.Max(2, Mathf.CeilToInt(sampleRate / Mathf.Max(1f, maxPitchHz)));
        if (maxTau <= minTau + 2)
            return 0f;

        // Difference function d(tau)
        var d = new float[maxTau + 1];
        for (int tau = 1; tau <= maxTau; tau++)
        {
            double s = 0;
            int m = n - tau;
            for (int i = 0; i < m; i++)
            {
                float diff = x[i] - x[i + tau];
                s += diff * diff;
            }
            d[tau] = (float)s;
        }

        // Cumulative mean normalized difference function (CMNDF)
        var cmndf = new float[maxTau + 1];
        cmndf[0] = 1f; cmndf[1] = 1f;
        double runningSum = 0;
        for (int tau = 1; tau <= maxTau; tau++)
        {
            runningSum += d[tau];
            cmndf[tau] = d[tau] * (float)tau / (float)(runningSum == 0 ? 1 : runningSum);
        }

        // Find first minimum below threshold in [minTau, maxTau]
        int bestTau = -1;
        for (int tau = minTau + 1; tau < maxTau - 1; tau++)
        {
            if (cmndf[tau] < yinThreshold && cmndf[tau] < cmndf[tau - 1] && cmndf[tau] <= cmndf[tau + 1])
            {
                bestTau = tau;
                break;
            }
        }

        // If none under threshold, take global minimum in range
        if (bestTau == -1)
        {
            float minVal = float.MaxValue;
            for (int tau = minTau; tau <= maxTau; tau++)
            {
                if (cmndf[tau] < minVal)
                {
                    minVal = cmndf[tau];
                    bestTau = tau;
                }
            }
        }

        if (bestTau <= 0)
            return 0f;

        // Parabolic interpolation for sub-sample precision on CMNDF
        int tau_m1 = Mathf.Clamp(bestTau - 1, 1, maxTau);
        int tau_p1 = Mathf.Clamp(bestTau + 1, 1, maxTau);
        float s0 = cmndf[tau_m1];
        float s1 = cmndf[bestTau];
        float s2 = cmndf[tau_p1];
        float denom = (2f * s1 - s2 - s0);
        float shift = denom == 0 ? 0 : 0.5f * (s2 - s0) / denom;
        float refinedTau = Mathf.Clamp(bestTau + shift, minTau, maxTau);

        float freq = sampleRate / refinedTau;
        confidence = Mathf.Clamp01(1f - s1); // higher when CMNDF minimum is deep
        return freq;
    }
}
