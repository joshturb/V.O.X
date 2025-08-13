using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Steamworks;
using Dissonance;

public class PlayerStats : NetworkBehaviour
{
	public static Dictionary<ulong, FixedString64Bytes> PlayerNamesById = new();
	public NetworkVariable<FixedString64Bytes> Name = new(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<ulong> SteamId = new(999, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<ulong> PlayerId = new(99, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	public NetworkVariable<bool> IsTalking = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
	private VoiceProximityBroadcastTrigger voiceProximityBroadcastTrigger;

	void Start()
	{
		if (!IsOwner)
			return;

		SetPlayerNameRpc(SteamClient.Name);
		SteamId.Value = SteamClient.SteamId;
		PlayerId.Value = NetworkManager.Singleton.LocalClientId;

		voiceProximityBroadcastTrigger = FindFirstObjectByType<VoiceProximityBroadcastTrigger>();
	}

	void LateUpdate()
	{
		if (!IsOwner)
			return;

		IsTalking.Value = voiceProximityBroadcastTrigger != null && voiceProximityBroadcastTrigger.IsTransmitting;
	}

	[Rpc(SendTo.Everyone)]
	public void SetPlayerNameRpc(FixedString64Bytes name)
	{
		PlayerNamesById[NetworkManager.Singleton.LocalClientId] = name;
	}
}
