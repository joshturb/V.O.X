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

	private FPCModule fpcModule;
	private MicrophoneModule microphoneModule;

	private float currentPitch = 0f;
    private float currentLoudness = -80f;
	private bool isLocked;
    public override bool IsLocked { get => isLocked; set => isLocked = value; }
    private bool isInitialized;
    public override bool IsInitialized { get => isInitialized; set => isInitialized = value; }
	
    [SerializeField] private bool debugMovingForward;
    [SerializeField] private bool debugMovingUp;

	public override void InitializeModule(FPCModule fPCModule)
	{
		IsLocked = false;
		fpcModule = fPCModule;

		if (!fPCModule.IsOwner)
		{
			Debug.LogWarning($"VoiceMovementModule: Not owner for client ID {fpcModule.OwnerClientId}");
			return;
		}

		if (!Slot.TryGetSlotById(fpcModule.OwnerClientId, out Slot slot))
		{
			Debug.LogError($"VoiceMovementModule: Slot not found for client ID {fpcModule.OwnerClientId}");
			return;
		}

		if (!slot.TryGetModule(out MicrophoneModule microphoneModule))
		{
			Debug.LogError($"VoiceMovementModule: MicrophoneModule not found for client ID {fpcModule.OwnerClientId}");
			return;
		}

		this.microphoneModule = microphoneModule;
		microphoneModule.OnVoiceMetricsUpdated_E += HandleVoiceMetricsUpdated;
	}

	public override void OnModuleRemoved(FPCModule fPCModule)
	{
		if (microphoneModule != null)
		{
			microphoneModule.OnVoiceMetricsUpdated_E -= HandleVoiceMetricsUpdated;
			microphoneModule = null;
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

		if (microphoneModule == null)
		{
			Debug.LogWarning("VoiceMovementModule.UpdateModule skipped: MicrophoneModule is null");
			return;
		}

		Vector3 movement = fPCModule.clientMovement;
		debugMovingForward = false;
		debugMovingUp = false;

		// Amplitude gating to ignore subtle sounds
		if (currentLoudness >= movementDbThreshold)
		{
			float forwardSpeedEff = forwardSpeedByDb != null && forwardSpeedByDb.keys.Length > 0 ? forwardSpeedByDb.Evaluate(currentLoudness) : 0f;
			float upSpeedEff = upSpeedByDb != null && upSpeedByDb.keys.Length > 0 ? upSpeedByDb.Evaluate(currentLoudness) : 0f;
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
	}

	private void HandleVoiceMetricsUpdated(ulong clientId, VoiceMetrics metrics)
	{
		if (clientId != fpcModule.OwnerClientId)
			return;

		currentPitch = metrics.Pitch;
		currentLoudness = metrics.LoudnessDb;
	}

	private void OnEnable()
	{
		if (microphoneModule != null)
		{
			microphoneModule.OnVoiceMetricsUpdated_E += HandleVoiceMetricsUpdated;
		}
	}

	private void OnDisable()
	{
		if (microphoneModule != null)
		{
			microphoneModule.OnVoiceMetricsUpdated_E -= HandleVoiceMetricsUpdated;
		}
	}
}
