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
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate_S += OnPlayerEnteredGate_SHandler;
		GameManager.IsGameRunning.OnValueChanged += OnGameStateChanged;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate_S -= OnPlayerEnteredGate_SHandler;
		GameManager.IsGameRunning.OnValueChanged -= OnGameStateChanged;
	}

	private async void OnPlayerEnteredGate_SHandler(ulong id, Gate.GateType gateType)
	{
		if (gateType == Gate.GateType.Death)
		{
			await KillPlayer(id);
		}
	}

	private void OnGameStateChanged(bool previousValue, bool newValue)
	{
		if (newValue)
		{
			foreach (var item in GameManager.PlayerDatas)
			{
				if (AlivePlayers.Contains(item.clientId))
					continue;

				AlivePlayers.Add(item.clientId);
			}
		}
		else
		{
			foreach (var item in GameManager.PlayerDatas)
			{
				if (AlivePlayers.Contains(item.clientId))
					continue;

				RepawnPlayer(item.clientId);
			}
		}
	}

	private async Awaitable DestroyPlayerObject(ulong id)
	{
		if (!IsServer)
			return;

		if (!AlivePlayers.Contains(id))
			return;

		if (!GameManager.GetPlayerData(id, out var playerData))
			return;

		if (!playerData.networkReference.TryGet(out NetworkObject networkObject))
			return;

		networkObject.Despawn(true);
		await Awaitable.NextFrameAsync();
	}

	private async Awaitable SpawnPlayerObject(ulong id)
	{
		if (!IsServer)
			return;

		if (AlivePlayers.Contains(id))
			return;

		BaseMinigameManager currentMinigameManager = GameManager.Instance.currentMinigameManager;
		int index = UnityEngine.Random.Range(0, currentMinigameManager.playerPositions.Length);

		currentMinigameManager.playerPositions[index].GetPositionAndRotation(out Vector3 spawnPosition, out Quaternion spawnRotation);
		PlayerSpawner.Instance.SpawnPlayer(id, spawnPosition, spawnRotation);
		await Awaitable.NextFrameAsync();
	}

	private async Awaitable KillPlayer(ulong id)
	{
		if (!IsServer)
			return;

		await DestroyPlayerObject(id);
		AlivePlayers.Remove(id);
		OnDeathRpc(id);
	}

	public async void RepawnPlayer(ulong id)
	{
		if (!IsServer)
			return;

		await SpawnPlayerObject(id);
		AlivePlayers.Add(id);
		OnReviveRpc(id);
	}

	[Rpc(SendTo.Everyone)]
	private void OnDeathRpc(ulong id)
	{
		OnPlayerDeath_E?.Invoke(id);
	}

	[Rpc(SendTo.Everyone)]
	private void OnReviveRpc(ulong id)
	{
		OnPlayerRevive_E?.Invoke(id);
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
