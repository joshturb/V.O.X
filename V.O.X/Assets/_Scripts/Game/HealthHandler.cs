using System;
using Unity.Netcode;
using UnityEngine;

public class HealthHandler : NetworkSingleton<HealthHandler>
{
	void Start()
	{
		if (!IsServer)
			return;

		PlayerManager.OnPlayerLivesChanged += HandlePlayerLivesChanged;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		PlayerManager.OnPlayerLivesChanged -= HandlePlayerLivesChanged;
	}

	private void HandlePlayerLivesChanged(ulong id, int lives)
	{
		if (lives != 0)
			return;

		if (!GameManager.GetPlayerData(id, out var playerData))
		{
			Debug.LogError("Player data not found for id: " + id);
			return;
		}

		if (!playerData.networkReference.TryGet(out var networkObject))
		{
			Debug.LogError("Network object not found for player: " + playerData.playerName);
			return;
		}

		KillPlayer(networkObject);
	}

	private void KillPlayer(NetworkObject networkObject)
	{
		networkObject.gameObject.SetActive(false);
		Spectating.Instance.AddSpectator(networkObject.OwnerClientId);
	}
	
	public static void RevivePlayer(ulong clientId)
	{
		if (!NetworkManager.Singleton.IsServer)
			return;

		if (!GameManager.GetPlayerData(clientId, out var playerData))
		{
			Debug.LogError("Player data not found for id: " + clientId);
			return;
		}

		if (!playerData.networkReference.TryGet(out var networkObject))
		{
			Debug.LogError("Network object not found for player: " + playerData.playerName);
			return;
		}

		networkObject.gameObject.SetActive(true);
		Spectating.Instance.RemoveSpectator(clientId);
	}
}
