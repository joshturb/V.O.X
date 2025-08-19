using UnityEngine;

public class CanaryCryModule : PlayerModule
{
	private bool isLocked;
    public override bool IsLocked { get => isLocked; set => isLocked = value; }
    private bool isInitialized;
    public override bool IsInitialized { get => isInitialized; set => isInitialized = value; }

	public float initialDistance = 5f;
	public float maxDistance = 20f;
	public Vector2 decibelRange = new(0f, 100f); // x is lowest decible it detects y is the max db it will detect (from initial to maxDistance)

	private FPCModule fpcModule;
	private MicrophoneModule microphoneModule;
	private float currentPitch = 0f;
    private float currentLoudness = -80f;

	public override void InitializeModule(FPCModule fPCModule)
	{
		IsLocked = false;
		fpcModule = fPCModule;

		if (!fPCModule.IsOwner)
		{
			Debug.LogWarning($"CanaryModule: Not owner for client ID {fpcModule.OwnerClientId}");
			return;
		}

		if (!Slot.TryGetSlotById(fpcModule.OwnerClientId, out Slot slot))
		{
			Debug.LogError($"CanaryModule: Slot not found for client ID {fpcModule.OwnerClientId}");
			return;
		}

		if (!slot.TryGetModule(out MicrophoneModule microphoneModule))
		{
			Debug.LogError($"CanaryModule: MicrophoneModule not found for client ID {fpcModule.OwnerClientId}");
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

	private void HandleVoiceMetricsUpdated(ulong clientId, VoiceMetrics metrics)
	{
		if (clientId != fpcModule.OwnerClientId)
			return;
			
		currentPitch = metrics.Pitch;
		currentLoudness = metrics.LoudnessDb;
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

		
	}
}
