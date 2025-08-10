using UnityEngine;
using NAudio.Wave;
using System;

[CreateAssetMenu(menuName = "Player Movement/Voice Movement Module")]
public class VoiceMovementModule : PlayerModule
{
    [Header("Voice Movement Settings")]
	[Tooltip("Speed vs dBFS curve for forward movement (x=dBFS, y=speed)")]
	[SerializeField] private AnimationCurve forwardSpeedByDb;
	[Tooltip("Speed vs dBFS curve for upward movement (x=dBFS, y=vertical speed)")]
	[SerializeField] private AnimationCurve upSpeedByDb;
    [SerializeField] private float pitchSmoothingFactor = 0.1f;
    [Tooltip("Absolute pitch decision threshold in Hz: below -> forward, above -> up")]
    [SerializeField] private float pitchDecisionThresholdHz = 100f;
	// Pitch detection now handled by MicrophoneModule
    [Header("Movement Gating")]
    [Tooltip("Minimum loudness required (dBFS) to allow movement")] 
    [SerializeField] private float movementDbThreshold = -50f;
	[SerializeField] private Vector3 movement;
	// Removed loudnessSpeedBoost in favor of AnimationCurves

    [Header("Debug (Runtime)")]
    [SerializeField] private float debugPitchHz;
    [SerializeField] private float debugAveragePitchHz;
    [SerializeField] private float debugConfidence;
    [SerializeField] private bool debugMovingForward;
    [SerializeField] private bool debugMovingUp;

    private WaveFormat waveFormat;
	private float currentPitch = 0f; // mirror from MicrophoneModule
	private float averagePitch = 0f; // mirror from MicrophoneModule
	private FPCModule _fpcModule;
	private MicrophoneModule _microphoneModule;

    private float _lastDb = -80f;
    private float _lastConfidence = 0f;

	private bool isLocked;
    public override bool IsLocked { get => isLocked; set => isLocked = value; }

    private bool isInitialized;
    public override bool IsInitialized { get => isInitialized; set => isInitialized = value; }

	public override void InitializeModule(FPCModule fPCModule)
	{
		IsLocked = false;
		_fpcModule = fPCModule;

	EnsureCurves();

		if (!fPCModule.IsOwner)
		{
			Debug.LogWarning($"VoiceMovementModule: Not owner for client ID {_fpcModule.OwnerClientId}");
			return;
		}

		if (!Slot.TryGetSlotById(_fpcModule.OwnerClientId, out Slot slot))
		{
			Debug.LogError($"VoiceMovementModule: Slot not found for client ID {_fpcModule.OwnerClientId}");
			return;
		}

		if (!slot.TryGetModule(out MicrophoneModule microphoneModule))
		{
			Debug.LogError($"VoiceMovementModule: MicrophoneModule not found for client ID {_fpcModule.OwnerClientId}");
			return;
		}

		_microphoneModule = microphoneModule;
		microphoneModule.OnAudioDataReceived_E += HandleAudioDataReceived;
		microphoneModule.OnAudioStreamReset_E += HandleAudioStreamReset;
		microphoneModule.OnVoiceMetricsUpdated_E += HandleVoiceMetricsUpdated;
    }

	public override void OnModuleRemoved(FPCModule fPCModule)
	{
		if (_microphoneModule != null)
		{
			_microphoneModule.OnAudioDataReceived_E -= HandleAudioDataReceived;
			_microphoneModule.OnAudioStreamReset_E -= HandleAudioStreamReset;
			_microphoneModule.OnVoiceMetricsUpdated_E -= HandleVoiceMetricsUpdated;
			_microphoneModule = null;
		}
	}

	public override void HandleInput(FPCModule fPCModule) { }

	public override void UpdateModule(FPCModule fPCModule)
	{
		if (!fPCModule.IsOwner || IsLocked)
		{
			Debug.Log($"VoiceMovementModule.UpdateModule skipped: IsOwner={fPCModule.IsOwner}, IsLocked={IsLocked}");
			return;
		}

		if (_microphoneModule == null)
		{
			Debug.LogWarning("VoiceMovementModule.UpdateModule skipped: MicrophoneModule is null");
			return;
		}

		Vector3 movement = fPCModule.clientMovement;
		debugMovingForward = false;
		debugMovingUp = false;

		// Amplitude gating to ignore subtle sounds
		if (_lastDb >= movementDbThreshold)
		{
			float forwardSpeedEff = forwardSpeedByDb != null && forwardSpeedByDb.keys.Length > 0 ? forwardSpeedByDb.Evaluate(_lastDb) : 0f;
			float upSpeedEff = upSpeedByDb != null && upSpeedByDb.keys.Length > 0 ? upSpeedByDb.Evaluate(_lastDb) : 0f;
			// Simple absolute decision: below threshold -> forward, above/equal -> up
			if (currentPitch > 0f && currentPitch < pitchDecisionThresholdHz)
			{
				Vector3 forwardMovement = fPCModule.transform.forward * forwardSpeedEff;
				movement.y = 0; // Preserve existing vertical movement (e.g., gravity)
				movement.x = forwardMovement.x;
				movement.z = forwardMovement.z;
				debugMovingForward = true;
			}
			// High pitch (above threshold) - move up
			else if (currentPitch >= pitchDecisionThresholdHz)
			{
				movement.y = upSpeedEff;
				debugMovingUp = true;
			}
			// Otherwise, no movement
		}
		else
		{
			// Below loudness gate: only horizontal movement is gated, vertical (gravity) is handled elsewhere
			movement.x = 0f;
			movement.z = 0f;
		}

		fPCModule.clientMovement = movement;
		this.movement = movement;

		// Update debug values
		debugPitchHz = currentPitch;
		debugAveragePitchHz = averagePitch;
	}

    private void HandleAudioDataReceived(ulong clientId, ArraySegment<float> data)
    {
        if (IsLocked)
			return;

	// Now unused: pitch is computed in MicrophoneModule; we just mirror metrics there via subscription below.
    }

    private void HandleAudioStreamReset(ulong clientId, WaveFormat waveFormat)
    {
        this.waveFormat = waveFormat;
    }

	private void HandleVoiceMetricsUpdated(ulong clientId, float pitch, float avgPitch, float db, float confidence)
	{
		if (clientId != _fpcModule.OwnerClientId)
			return;
		currentPitch = pitch;
		averagePitch = avgPitch;
		_lastDb = db;
		_lastConfidence = confidence;
		debugConfidence = confidence;
	}

	private void OnEnable()
	{
		if (_microphoneModule != null)
		{
			_microphoneModule.OnVoiceMetricsUpdated_E += HandleVoiceMetricsUpdated;
		}
	}

	private void OnDisable()
	{
		if (_microphoneModule != null)
		{
			_microphoneModule.OnVoiceMetricsUpdated_E -= HandleVoiceMetricsUpdated;
		}
	}

	private void EnsureCurves()
	{
		// Initialize default curves if empty so designers have a starting point.
		// X axis in dBFS: from movementDbThreshold to 0 dB.
		if (forwardSpeedByDb == null || forwardSpeedByDb.keys.Length == 0)
		{
			forwardSpeedByDb = new AnimationCurve(
				new Keyframe(movementDbThreshold, 0f, 0f, 0f),
				new Keyframe(Mathf.Lerp(movementDbThreshold, 0f, 0.5f), 2f),
				new Keyframe(0f, 4f, 0f, 0f)
			);
		}
		if (upSpeedByDb == null || upSpeedByDb.keys.Length == 0)
		{
			upSpeedByDb = new AnimationCurve(
				new Keyframe(movementDbThreshold, 0f, 0f, 0f),
				new Keyframe(Mathf.Lerp(movementDbThreshold, 0f, 0.5f), 3f),
				new Keyframe(0f, 6f, 0f, 0f)
			);
		}
	}
}
