using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using UnityEngine;

public class Spectating : NetworkSingleton<Spectating>
{
	public static event System.Action<ulong> OnSpectatorAdded_S;
	public static event System.Action<ulong> OnSpectatorRemoved_S;
	public static NetworkList<ulong> spectatingIds;
	public SerializedDictionary<ulong, NetworkObject> spectatingObjects = new();
	public NetworkObject spectatingPrefab;

	protected override void Awake()
	{
		base.Awake();
		spectatingIds = new NetworkList<ulong>();
	}

	private void Start()
	{
		if (!IsServer)
			return;
		
		PlayerManager.OnPlayerRevive_E += (id) =>
		{
			RemoveSpectator(id);
		};

		PlayerManager.OnPlayerDeath_E += (id) =>
		{
			print($"spectating {id}");
			AddSpectator(id);
		};
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		spectatingIds.Clear();
		spectatingObjects.Clear();

		PlayerManager.OnPlayerRevive_E -= (id) =>
		{
			RemoveSpectator(id);
		};

		PlayerManager.OnPlayerDeath_E -= (id) =>
		{
			AddSpectator(id);
		};
	}

	private void AddSpectator(ulong clientId)
	{
		if (spectatingIds.Contains(clientId))
		{
			print($"Spectator {clientId} is already added.");
			return;
		}

		spectatingIds.Add(clientId);
		var spectatingObject = Instantiate(spectatingPrefab);
		spectatingObject.SpawnAsPlayerObject(clientId);
		spectatingObject.DestroyWithScene = false;
		spectatingObjects[clientId] = spectatingObject;
		OnSpectatorAdded_S?.Invoke(clientId);
		print($"Spectator {clientId} added.");
	}

	private void RemoveSpectator(ulong clientId)
	{
		if (!spectatingIds.Contains(clientId))
		{
			print($"Spectator {clientId} is not added.");
			return;
		}
	
		spectatingIds.Remove(clientId);
		if (spectatingObjects.TryGetValue(clientId, out var spectatingObject))
		{
			spectatingObject.Despawn();
		}
		spectatingObjects.Remove(clientId);
		OnSpectatorRemoved_S?.Invoke(clientId);
		print($"Spectator {clientId} removed.");
	}

	public static bool IsSpectating(ulong clientId)
	{
		return spectatingIds.Contains(clientId);
	}

	public static NetworkObject[] GetAlivePlayersNetworkObjects()
	{
		NetworkObject[] aliveNetworkObjects = new NetworkObject[PlayerManager.Instance.AlivePlayers.Count];
		for (int i = 0; i < aliveNetworkObjects.Length; i++)
		{
			var id = PlayerManager.Instance.AlivePlayers[i];
			if (GameManager.GetPlayerData(id, out var playerData)) 
			{
				if (playerData.networkReference.TryGet(out NetworkObject networkObject))
				{
					aliveNetworkObjects[i] = networkObject;
				}
				else
				{
					Debug.LogWarning($"Player {id} does not have a valid NetworkObject.");
				}
			}
			else
			{
				Debug.LogWarning($"Player data for {id} not found.");
			}
		}
		return aliveNetworkObjects;
	}
}
