using Unity.Netcode;
using Dissonance;

public class PlayerStats : NetworkBehaviour
{
	public NetworkVariable<bool> IsTalking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private VoiceProximityBroadcastTrigger voiceProximityBroadcastTrigger;

	void Start()
	{
		if (!IsOwner)
			return;

		voiceProximityBroadcastTrigger = FindFirstObjectByType<VoiceProximityBroadcastTrigger>();
	}

	void LateUpdate()
	{
		if (!IsOwner)
			return;

		IsTalking.Value = voiceProximityBroadcastTrigger != null && voiceProximityBroadcastTrigger.IsTransmitting;
	}
}
