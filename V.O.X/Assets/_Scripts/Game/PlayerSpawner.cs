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
			int index = UnityEngine.Random.Range(0, spawnPoints.Length);
			SpawnPlayer(clientId, spawnPoints[index].position, spawnPoints[index].rotation);
		};

		int index = UnityEngine.Random.Range(0, spawnPoints.Length);
		SpawnPlayer(NetworkManager.ServerClientId, spawnPoints[index].position, spawnPoints[index].rotation);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
		NetworkManager.Singleton.OnClientConnectedCallback -= (clientId) =>
		{
			int index = UnityEngine.Random.Range(0, spawnPoints.Length);
			SpawnPlayer(clientId, spawnPoints[index].position, spawnPoints[index].rotation);
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
