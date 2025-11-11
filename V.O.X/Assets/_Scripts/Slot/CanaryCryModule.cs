using Unity.Netcode;
using UnityEngine;

public class CanaryCryModule : BaseSlotModule
{
	public float initialDistance = 5f;
	public float maxDistance = 20f;
	public Vector2 decibelRange = new(0f, 100f); // x is lowest decible it detects y is the max db it will detect (from initial to maxDistance)
	[SerializeField] private float minLifetime = 1f;
	[SerializeField] private float maxLifetime = 3f;
	[SerializeField] private Vector3 offset;
	public ParticleSystem visualEffect;
	private NetworkObject playerObject;
	private MicrophoneModule microphoneModule;

	public override void Initialize(Slot slot)
	{
		base.Initialize(slot);

		if (!slot.TryGetModule(out MicrophoneModule microphoneModule))
		{
			Debug.LogError($"CanaryModule: MicrophoneModule not found for client ID {slot.OwnerClientId}");
			return;
		}

		if (!GameManager.GetPlayerData(OwnerClientId, out var playerData))
			return;

		playerObject = playerData.networkReference.TryGet(out var netObj) ? netObj : null;
		if (playerObject == null)
		{
			Debug.LogError($"CanaryModule: Player object not found for client ID {slot.OwnerClientId}");
			return;
		}

		this.microphoneModule = microphoneModule;
		microphoneModule.OnVoiceMetricsUpdated_E += HandleVoiceMetricsUpdated;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (microphoneModule != null)
		{
			microphoneModule.OnVoiceMetricsUpdated_E -= HandleVoiceMetricsUpdated;
			microphoneModule = null;
		}
	}

	public override void UpdateModule() { }

	private void HandleVoiceMetricsUpdated(ulong id, VoiceMetrics metrics)
	{
		if (id != microphoneModule.OwnerClientId)
			return;

		if (InputHandler.Instance.playerActions.Attack.IsPressed())
		{
			// Map loudness -> distance (clamped)
			float loudNorm = Mathf.InverseLerp(decibelRange.x, decibelRange.y, metrics.LoudnessDb);
			float distance = Mathf.Lerp(initialDistance, maxDistance, loudNorm);
			distance = Mathf.Clamp(distance, initialDistance, maxDistance);

			if (playerObject != null)
			{
				// Position/rotation + forward-only scale (Z)
				visualEffect.transform.SetPositionAndRotation(playerObject.transform.position + offset, Quaternion.LookRotation(playerObject.transform.forward));
				// Distance -> lifetime (linear between min/max), size = 0.5 * lifetime
				float t = Mathf.InverseLerp(initialDistance, maxDistance, distance);
				float lifetime = Mathf.Lerp(minLifetime, maxLifetime, t);
				float size = lifetime * 0.5f;

				var main = visualEffect.main; // ParticleSystem.MainModule
				main.startLifetime = lifetime; // MinMaxCurve implicit from float
				main.startSize = size;         // MinMaxCurve implicit from float

				if (!visualEffect.isPlaying)
					visualEffect.Play();
			}
		}
	}
}
