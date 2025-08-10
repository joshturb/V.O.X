using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class WaveformDrawer
{
	private readonly Texture2D texture;
	private readonly int textureWidth;
	private readonly int textureHeight;
	private readonly Color waveformColor;
	private readonly Color[] pixels;
	private readonly Color[] clearPixels;
	private readonly List<float> accumulatedSamples = new();
	private int lastDrawnX = -1;
	private bool isDrawing;
	private float startTime;
	private float duration;
	private int sampleRate = 48000; // Default, can be set

	public void SetSampleRate(int rate)
	{
		sampleRate = rate;
	}

	public WaveformDrawer(Texture2D texture, int width, int height, Color waveformColor, Color clearPixelColor)
	{
		this.texture = texture;
		textureWidth = width;
		textureHeight = height;
		this.waveformColor = waveformColor;
		pixels = new Color[width * height];
		clearPixels = new Color[width * height];
		for (int i = 0; i < clearPixels.Length; i++)
			clearPixels[i] = clearPixelColor;
	}

	public void StartDrawing(float startTime, float duration)
	{
		this.startTime = startTime;
		this.duration = duration;
		isDrawing = true;
		accumulatedSamples.Clear();
		lastDrawnX = -1;
		ClearTexture();
	}

	public void UpdateDrawing(float[] realtimeData, float serverTime)
	{
		if (!isDrawing || texture == null || realtimeData == null)
			return;

		accumulatedSamples.AddRange(realtimeData);

		float scanProgress = (serverTime - startTime) / (duration + 0.05f);
		if (scanProgress >= 1f)
		{
			EndDrawing();
			return;
		}
		DrawWaveformIncremental(scanProgress);
	}

	public void EndDrawing(bool clear = true)
	{
		isDrawing = false;
		accumulatedSamples.Clear();
		lastDrawnX = -1;

		if (clear)
			ClearTexture();
		else
			texture.Apply();
	}

	public void ClearTexture()
	{
		Array.Copy(clearPixels, pixels, pixels.Length);
		texture.SetPixels(pixels);
		texture.Apply();
	}

	private void DrawWaveformIncremental(float scanProgress)
	{
		if (accumulatedSamples.Count == 0)
			return;

		int currentX = Mathf.FloorToInt(scanProgress * textureWidth);
		if (currentX <= lastDrawnX)
			return;

		int startX = Mathf.Max(0, lastDrawnX + 1);
		int sampleCount = accumulatedSamples.Count;


	int expectedSampleCount = Mathf.CeilToInt(sampleRate * duration);
	for (int x = startX; x <= currentX && x < textureWidth; x++)
		DrawWaveformColumn(x, expectedSampleCount);

		texture.SetPixels(pixels);
		texture.Apply();
		lastDrawnX = currentX;
	}



private void DrawWaveformColumn(int x, int expectedSampleCount)
{
	float xProgress = (float)x / textureWidth;
	int startSample = Mathf.FloorToInt(xProgress * expectedSampleCount);
	int endSample = Mathf.FloorToInt((x + 1f) / textureWidth * expectedSampleCount);

	int availableSamples = accumulatedSamples.Count;
	if (startSample >= availableSamples) {
		// Not enough samples yet, draw flat line at center
		int centerY = textureHeight / 2;
		pixels[centerY * textureWidth + x] = waveformColor;
		return;
	}

	startSample = Mathf.Clamp(startSample, 0, availableSamples - 1);
	endSample = Mathf.Clamp(endSample, startSample, availableSamples);

	float minAmplitude = 0f, maxAmplitude = 0f;
	for (int i = startSample; i < endSample; i++)
	{
		float sample = accumulatedSamples[i];
		if (sample < minAmplitude) minAmplitude = sample;
		if (sample > maxAmplitude) maxAmplitude = sample;
	}

	int centerY2 = textureHeight / 2;
	int minY = Mathf.Clamp(centerY2 + Mathf.RoundToInt(minAmplitude * textureHeight * 0.4f), 0, textureHeight - 1);
	int maxY = Mathf.Clamp(centerY2 + Mathf.RoundToInt(maxAmplitude * textureHeight * 0.4f), 0, textureHeight - 1);

	int yStart = Mathf.Min(minY, maxY);
	int yEnd = Mathf.Max(minY, maxY);

	for (int y = yStart; y <= yEnd; y++)
		pixels[y * textureWidth + x] = waveformColor;

	if (minY == maxY)
		pixels[centerY2 * textureWidth + x] = waveformColor;
}

	public int GetColumnPosition()
	{
		if (!isDrawing)
			return -1;

		float scanProgress = (NetworkManager.Singleton.ServerTime.TimeAsFloat - startTime) / (duration + 0.05f);
		int currentX = Mathf.FloorToInt(scanProgress * textureWidth);
		return Mathf.Clamp(currentX, 0, textureWidth - 1);
	}
}