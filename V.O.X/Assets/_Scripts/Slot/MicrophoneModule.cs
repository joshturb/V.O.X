using Dissonance.Datastructures;
using Dissonance.Audio.Capture;
using Unity.Netcode;
using UnityEngine;
using NAudio.Wave;
using Dissonance;
using System;

[Serializable]
public struct VoiceMetrics
{
	public float LoudnessDb { get; set; }
	public float Pitch { get; set; }
	public float AveragePitch { get; set; }
	public float Confidence { get; set; }
}

public class MicrophoneModule : BaseSlotModule, IMicrophoneSubscriber
{
	public event Action<ulong, ArraySegment<float>> OnAudioDataReceived_E;
	public event Action<ulong, WaveFormat> OnAudioStreamReset_E;
	public event Action<ulong> OnRecordingRequested_S;
	public event Action<ulong, VoiceMetrics> OnVoiceMetricsUpdated_E;

	private WaveFormat _format;
	private readonly TransferBuffer<float> _transfer = new(capacity: 4096);
	private bool _resetPending;
	private int _lostSamples;
	private readonly float[] _temporary = new float[800];
	private ulong _occupantId;
	private Slot ownerSlot;

	[Header("Voice Analysis Settings")]
	[SerializeField] private float minPitchHz = 80f;
	[SerializeField] private float maxPitchHz = 800f;
	[SerializeField] private float yinThreshold = 0.15f;
	[SerializeField] private float minDbFs = -60f;
	[SerializeField] private float pitchSmoothingFactor = 0.1f;
	[SerializeField] private float averagePitchSmoothing = 0.01f;
	[SerializeField] private float amplitudeWeightPower = 1.0f;

	[Header("Voice Analysis Runtime (ReadOnly)")]
	[SerializeField] private float currentPitch;
	[SerializeField] private float averagePitch;
	[SerializeField] private float loudnessDb = -80f;
	[SerializeField] private float confidence;

	public float CurrentPitch => currentPitch;
	public float AveragePitch => averagePitch;
	public float LoudnessDb => loudnessDb;
	public float Confidence => confidence;

	public override void Initialize(Slot slot)
	{
		base.Initialize(slot);
		ownerSlot = slot;
		slot.OccupantId.OnValueChanged += OnSlotOccupied;
		FindFirstObjectByType<DissonanceComms>().SubscribeToRecordedAudio(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		ownerSlot.OccupantId.OnValueChanged -= OnSlotOccupied;
		ownerSlot = null;
		FindFirstObjectByType<DissonanceComms>().UnsubscribeFromRecordedAudio(this);
	}

	private void OnSlotOccupied(ulong previousValue, ulong newValue)
	{
		_occupantId = newValue;
	}

	private void ProcessAudio(ArraySegment<float> arraySegment)
	{
		// Perform pitch & loudness analysis before forwarding raw audio
		if (_format != null && arraySegment.Count > 0)
		{
			float pitch = VoiceUtils.CalculatePitchYin(
				arraySegment,
				_format.SampleRate,
				minPitchHz,
				maxPitchHz,
				yinThreshold,
				minDbFs,
				out float localConfidence,
				out float localDb);

			// Loudness gating for smoothing weight
			float loudnessWeight = Mathf.Clamp01((localDb - (-80f)) / (0f - (-80f)));
			if (localDb < minDbFs)
			{
				pitch = 0f;
				localConfidence = 0f;
			}
			float weight = Mathf.Pow(Mathf.Clamp01(localConfidence * loudnessWeight), amplitudeWeightPower);
			currentPitch = Mathf.Lerp(currentPitch, pitch, pitchSmoothingFactor * Mathf.Clamp01(0.15f + weight * 0.85f));
			averagePitch = Mathf.Lerp(averagePitch, currentPitch, averagePitchSmoothing);
			confidence = localConfidence;
			loudnessDb = localDb;
			OnVoiceMetricsUpdated_E?.Invoke(_occupantId, new VoiceMetrics
			{
				Pitch = currentPitch,
				AveragePitch = averagePitch,
				LoudnessDb = loudnessDb,
				Confidence = confidence
			});
		}

		OnAudioDataReceived_E?.Invoke(_occupantId, arraySegment);
	}

	private void ResetAudioStream(WaveFormat format)
	{
		OnAudioStreamReset_E?.Invoke(_occupantId, format);
	}

	void IMicrophoneSubscriber.ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format)
	{
		if (!OwnsComponent(_occupantId))
			return;

		if (_format == null)
		{
			_format = format;
			_resetPending = true;
		}

		// If the format has changed, clear the buffer
		if (!_format.Equals(format))
		{
			_format = format;
			_resetPending = true;
			_transfer.Clear();
			_lostSamples = 0;
			return;
		}

		// Write as much data as possible to the buffer
		var written = _transfer.WriteSome(buffer);
		_lostSamples += buffer.Count - written;
	}

	void IMicrophoneSubscriber.Reset()
	{
		if (!OwnsComponent(_occupantId))
			return;

		_transfer.Clear();
		_resetPending = true;
	}

	public virtual void Update()
	{
		if (!OwnsComponent(_occupantId))
			return;

		if (_resetPending)
		{
			if (_format == null)
				return;
			_resetPending = false;
			ResetAudioStream(_format);
		}

		// Keep reading as much data as possible
		var loop = true;
		while (loop)
		{
			// Clear the temporary array, ready to write more data into it
			Array.Clear(_temporary, 0, _temporary.Length);

			// If there are any lost samples shrink the read array by that amount to inject silence (no more than 50% of the data may be silence)
			var silence = Math.Min(_temporary.Length / 2, _lostSamples);
			var read = new ArraySegment<float>(_temporary, 0, _temporary.Length - silence);

			// Read the reduced size array segment from the buffer, but then submit the entire array to the user.
			// This means the unread section is filled with silence
			if (_transfer.Read(read))
			{
				_lostSamples -= silence;
				ProcessAudio(new ArraySegment<float>(_temporary));
			}
			else
				loop = false;
		}

	}
	public virtual void LateUpdate()
	{
		if (!OwnsComponent(NetworkManager.LocalClientId))
			return;

		if (InputHandler.Instance == null)
			return;

		if (InputHandler.Instance.playerActions.Jump.WasPressedThisFrame())
		{
			RequestRecordingRpc(NetworkManager.LocalClientId);
		}
	}

	[Rpc(SendTo.Server)]
	private void RequestRecordingRpc(ulong playerId)
	{
		OnRecordingRequested_S?.Invoke(playerId);
	}
}
