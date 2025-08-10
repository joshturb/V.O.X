using Unity.Netcode;
using UnityEngine;
using System;

public class PlayerSpawner : NetworkSingleton<PlayerSpawner>
{
	public static event Action<ulong, Vector3> OnPlayerSpawned_E;
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

		OnPlayerSpawnedRpc(clientId, playerObject.transform.position);
		Debug.Log($"Spawned player: {playerObject.name} at {spawnPoints[index].position}");
	}

	[Rpc(SendTo.Everyone)]
	public void OnPlayerSpawnedRpc(ulong clientId, Vector3 position)
	{
		OnPlayerSpawned_E?.Invoke(clientId, position);
	}
}
