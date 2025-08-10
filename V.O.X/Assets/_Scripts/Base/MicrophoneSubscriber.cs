using System;
using Dissonance;
using NAudio.Wave;

public class MicrophoneSubscriber : BaseMicrophoneSubscriber
{
	public static event Action<ArraySegment<float>> OnAudioDataReceived;
	public static event Action<WaveFormat> OnAudioStreamReset;
	
	void Start()
	{
		UnityEngine.Object.FindFirstObjectByType<DissonanceComms>().SubscribeToRecordedAudio(this);
	}

	protected override void ProcessAudio(ArraySegment<float> data)
	{
		OnAudioDataReceived?.Invoke(data);
	}

	protected override void ResetAudioStream(WaveFormat waveFormat)
	{
		OnAudioStreamReset?.Invoke(waveFormat);
	}
}
