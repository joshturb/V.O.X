using Unity.Netcode;
using UnityEngine;
using Steamworks;
using System;

public class PlayerSpawner : NetworkSingleton<PlayerSpawner>
{
	public static event Action<ulong> OnPlayerSpawned_E;
	public NetworkObject slotPrefab;
	[SerializeField] private NetworkObject playerObj;

	private void Start()
	{
		if (!IsServer)
			return;

		NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
		NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
		{
			SpawnPlayer(clientId, Vector3.zero, Quaternion.identity);
		};

		SpawnPlayer(NetworkManager.ServerClientId, Vector3.zero, Quaternion.identity);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
		NetworkManager.Singleton.OnClientConnectedCallback -= (clientId) =>
		{
			SpawnPlayer(clientId, Vector3.zero, Quaternion.identity);
		};
	}

	private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
	{
		if (GameManager.CanPlayersJoin == false)
		{
			response.Approved = false;
			response.Reason = "A minigame is currently running.";
			return;
		}

		response.Approved = true;
		response.CreatePlayerObject = false;
	}

	public void SpawnPlayer(ulong clientId, Vector3 position, Quaternion rotation)
	{
		if (!IsServer)
			return;

		NetworkObject playerObject = Instantiate(playerObj, position, rotation);
		playerObject.name = $"Player_{clientId}";
		playerObject.SpawnAsPlayerObject(clientId);
		NetworkObject slotObject = Instantiate(slotPrefab);
		slotObject.name = $"Slot_{clientId}";
		slotObject.SpawnWithOwnership(clientId);

		HandlePlayerSpawnedRpc(new(playerObject), new RpcParams
		{
			Send = new RpcSendParams
			{
				Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
			}
		});

		Debug.Log($"Spawned player: {playerObject.name} at {position}");
	}

	[Rpc(SendTo.SpecifiedInParams)]
	private void HandlePlayerSpawnedRpc(NetworkObjectReference playerReference, RpcParams rpcParams = default)
	{
		var playerData = new PlayerData
		{
			clientId = NetworkManager.LocalClientId,
			steamId = SteamClient.SteamId,
			playerName = SteamClient.Name,
			networkReference = playerReference
		};

		GameManager.Instance.SetPlayerDataRpc(playerData);
	}
}
