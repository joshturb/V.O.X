using Unity.Netcode;
using UnityEngine;
using Steamworks;
using System;

public class PlayerSpawner : NetworkSingleton<PlayerSpawner>
{
	public static event Action<ulong> OnPlayerSpawned_E;
	[SerializeField] private NetworkObject playerObj;
	[SerializeField] private Transform[] spawnPoints;

	private void Start()
	{
		if (!IsServer)
			return;

		NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
		NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
		{
			SpawnPlayer(clientId);
		};

		SpawnPlayer(NetworkManager.ServerClientId);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
		NetworkManager.Singleton.OnClientConnectedCallback -= (clientId) =>
		{
			SpawnPlayer(clientId);
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

	private void SpawnPlayer(ulong clientId)
	{
		int index = UnityEngine.Random.Range(0, spawnPoints.Length);
		NetworkObject playerObject = Instantiate(playerObj, spawnPoints[index].position, spawnPoints[index].rotation);
		playerObject.name = $"Player_{clientId}";
		playerObject.SpawnAsPlayerObject(clientId);

		HandlePlayerSpawnedRpc(new(playerObject), new RpcParams
		{
			Send = new RpcSendParams
			{
				Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
			}
		});

		Debug.Log($"Spawned player: {playerObject.name} at {spawnPoints[index].position}");
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

		GameManager.Instance.AddPlayerDataRpc(playerData);
	}
}
