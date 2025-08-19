using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class PlayerManager : NetworkSingleton<PlayerManager>
{
	public NetworkList<ulong> AlivePlayers = new();
	public static event Action<ulong> OnPlayerDeath_E;
	public static event Action<ulong> OnPlayerRevive_E;

	void Start()
	{
		AlivePlayers.OnListChanged += (changeEvent) =>
		{
			if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Add)
			{
				OnPlayerRevive_E?.Invoke(changeEvent.Value);
			}
			else if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Remove)
			{
				OnPlayerDeath_E?.Invoke(changeEvent.Value);
				print("OnPlayerDeath_E invoked");
			}
		};

		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate_S += async (id, gateType) =>
		{
			if (gateType == Gate.GateType.Death)
			{
				await KillPlayer(id);
			}
		};

		GameManager.IsGameRunning.OnValueChanged += SetupAlivePlayers;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		AlivePlayers.OnListChanged -= (changeEvent) =>
		{
			if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Add)
			{
				OnPlayerRevive_E?.Invoke(changeEvent.Value);
			}
			else if (changeEvent.Type == NetworkListEvent<ulong>.EventType.Remove)
			{
				OnPlayerDeath_E?.Invoke(changeEvent.Value);
			}
		};

		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate_S -= async (id, gateType) =>
		{
			if (gateType == Gate.GateType.Death)
			{
				await KillPlayer(id);
			}

		};

		GameManager.IsGameRunning.OnValueChanged -= SetupAlivePlayers;
	}

	private void SetupAlivePlayers(bool previousValue, bool newValue)
	{
		if (newValue)
		{
			foreach (var item in GameManager.PlayerDatas)
			{
				AlivePlayers.Add(item.clientId);
			}
		}
		else
		{
			AlivePlayers.Clear();
		}
	}

	private async Awaitable KillPlayer(ulong id)
	{
		if (!IsServer)
			return;

		// add some verification here

		if (!AlivePlayers.Contains(id))
			return;

		if (!GameManager.GetPlayerData(id, out var playerData))
			return;

		if (!playerData.networkReference.TryGet(out NetworkObject networkObject))
			return;

		networkObject.Despawn();
		await Awaitable.NextFrameAsync();

		AlivePlayers.Remove(id);
		print("Player " + id + " has died.");
	}

	public void RevivePlayer(ulong id)
	{
		if (!IsServer)
			return;

		// add some verification here

		if (AlivePlayers.Contains(id))
		{
			print("Player " + id + " is already alive.");
			return;
		}

		BaseMinigameManager currentMinigameManager = GameManager.Instance.currentMinigameManager;
		int index = UnityEngine.Random.Range(0, currentMinigameManager.playerPositions.Length);

		AlivePlayers.Add(id);
		currentMinigameManager.playerPositions[index].GetPositionAndRotation(out Vector3 spawnPosition, out Quaternion spawnRotation);
		PlayerSpawner.Instance.SpawnPlayer(id, spawnPosition, spawnRotation);
		print("Player " + id + " has been revived.");
	}

	public static int GetAlivePlayerCount()
	{
		return Instance.AlivePlayers.Count;
	}

	public static bool IsPlayerAlive(ulong id)
	{
		return Instance.AlivePlayers.Contains(id);
	}

	public List<Transform> GetAlivePlayerTransforms()
	{
		List<Transform> aliveTransforms = new List<Transform>();
		foreach (var playerId in AlivePlayers)
		{
			if (GameManager.GetPlayerData(playerId, out var data))
			{
				if (data.networkReference.TryGet(out NetworkObject networkObject))
				{
					aliveTransforms.Add(networkObject.transform);
				}
				else
				{
					Debug.LogWarning($"Player {playerId} does not have a valid NetworkObject.");
				}
			}
		}
		return aliveTransforms;
	}
}
