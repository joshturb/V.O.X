using AYellowpaper.SerializedCollections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System;
using Unity.Burst.Intrinsics;
using Unity.Netcode.Components;

public enum MiniGame
{
	None,
	Telephone,
	RaceToHeaven,
	PushToShove,
}

[Serializable]
public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
{
	public ulong clientId;
	public ulong steamId;
	public FixedString64Bytes playerName;
	public NetworkObjectReference networkReference;

	public readonly bool Equals(PlayerData other)
	{
		return clientId == other.clientId;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref clientId);
		serializer.SerializeValue(ref steamId);
		serializer.SerializeValue(ref playerName);
		serializer.SerializeValue(ref networkReference);
	}
}

[Serializable]
public struct PlayerScore
{
    public ulong clientId;
    public int score;
}

public class GameManager : NetworkSingleton<GameManager>
{
	public static NetworkList<PlayerData> PlayerDatas;
	public static Dictionary<ulong, int> PlayerScores = new();
	public static NetworkVariable<bool> IsGameRunning = new(false);
	public static event Action<ulong, int> OnScoreUpdated_S;
	public static event Action<MiniGame> OnMinigameUnloaded_S;
	public static bool CanPlayersJoin = true;

	[Header("Scene Configuration")]
	public SerializedDictionary<MiniGame, string> minigameScenes;
	public NetworkVariable<MiniGame> PreviousMinigame = new(MiniGame.None);
	public NetworkVariable<MiniGame> CurrentMinigame = new(MiniGame.None);
	public string theBlankSceneName = "_TheBlank";
	[Range(0.01f, 10)] public float sceneTransitionDelay = 1f;


	[Header("Scoring")]
	public int pointsPerRound = 10;
	public Vector2Int pointsPerScore = new(10, 1);

	public BaseMinigameManager currentMinigameManager;
	private Coroutine startCoroutine;

	protected override void Awake()
	{
		base.Awake();
		PlayerDatas = new NetworkList<PlayerData>();
	}

	void Start()
	{
		if (!IsServer)
			return;

		startCoroutine = StartCoroutine(Transition(theBlankSceneName));

		NetworkManager.Singleton.SceneManager.OnUnloadComplete += OnSceneUnloaded;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (!IsServer)
			return;

		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
			return;

		NetworkManager.Singleton.SceneManager.OnUnloadComplete -= OnSceneUnloaded;
	}

	public void EndMinigame(List<ulong> playersByScore)
	{
		if (!IsServer)
			return;

		if (playersByScore != null)
		{
			foreach (var playerId in playersByScore)
			{
				int index = playersByScore.IndexOf(playerId);
				int totalPlayers = playersByScore.Count;
				int score = (totalPlayers == 1)
					? pointsPerScore.x
					: Mathf.RoundToInt(Mathf.Lerp(pointsPerScore.x, pointsPerScore.y, index / (float)(totalPlayers - 1)));

				if (PlayerManager.IsPlayerAlive(playerId))
				{
					score += pointsPerRound;
				}

				AddScore(playerId, score);
			}
		}

		if (PlayerManager.GetAlivePlayerCount() <= 1)
		{
			IsGameRunning.Value = false;
		}

		OnMinigameUnloaded_S?.Invoke(CurrentMinigame.Value);
		Scene sceneToUnload = SceneManager.GetSceneByName(minigameScenes[CurrentMinigame.Value]);
		NetworkManager.Singleton.SceneManager.UnloadScene(sceneToUnload);
	}

	private void OnSceneUnloaded(ulong clientId, string sceneName)
	{
		MiniGame nextMinigame = IsGameRunning.Value ? GetRandomMinigame() : MiniGame.None;
		string sceneNameToLoad = nextMinigame != MiniGame.None ? minigameScenes[nextMinigame] : theBlankSceneName;

		if (CurrentMinigame.Value != MiniGame.None)
		{
			PreviousMinigame.Value = CurrentMinigame.Value;
		}
		CurrentMinigame.Value = nextMinigame;

		if (startCoroutine != null)
		{
			StopCoroutine(startCoroutine);
		}
		startCoroutine = StartCoroutine(Transition(sceneNameToLoad));
	}

	private IEnumerator Transition(string sceneName)
	{
		yield return new WaitForSeconds(sceneTransitionDelay);
		NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
	}

	private MiniGame GetRandomMinigame()
	{
		var availableGames = Enum.GetValues(typeof(MiniGame));
		var validGames = new List<MiniGame>();

		// Collect all valid minigames (excluding Blank, the previous game, and ensuring a scene exists in the dictionary)
		foreach (MiniGame game in availableGames)
		{
			if (game != MiniGame.None && game != PreviousMinigame.Value && minigameScenes.ContainsKey(game))
				validGames.Add(game);
		}

		if (validGames.Count == 0)
		{
			Debug.LogWarning("No valid minigames available, returning first non-Blank game with a scene.");
			// Ensure a non-Blank game is returned that has an associated scene
			foreach (MiniGame game in availableGames)
			{
				if (game != MiniGame.None && minigameScenes.ContainsKey(game))
					return game;
			}
		}

		return validGames[UnityEngine.Random.Range(0, validGames.Count)];
	}

	public void AddScore(ulong clientId, int points)
	{
		if (!PlayerScores.ContainsKey(clientId))
		{
			PlayerScores[clientId] = 0;
		}

		PlayerScores[clientId] += points;
		OnScoreUpdated_S?.Invoke(clientId, PlayerScores[clientId]);
	}

	public int GetScore(ulong clientId)
	{
		return PlayerScores.TryGetValue(clientId, out var score) ? score : 0;
	}

	public static bool GetPlayerData(ulong clientId, out PlayerData playerData)
	{
		foreach (var data in PlayerDatas)
		{
			if (data.clientId == clientId)
			{
				playerData = data;
				return true;
			}
		}
		playerData = default;
		return false;
	}

	[Rpc(SendTo.Server)]
	public void SetPlayerDataRpc(PlayerData playerData)
	{
		if (PlayerDatas.Contains(playerData))
		{
			PlayerDatas[PlayerDatas.IndexOf(playerData)] = playerData;
			return;
		}
		else
		{
			PlayerDatas.Add(playerData);
		}
	}

	public void RemovePlayerData(ulong clientId)
	{
		if (!IsServer)
			return;

		if (GetPlayerData(clientId, out var data))
		{
			PlayerDatas.Remove(data);
		}
	}

	public bool TryGetModuleLocker(ulong id, out ModuleLocker moduleLocker)
	{
		moduleLocker = null;

		if (!IsServer)
		{
			Debug.Log("GetModuleLocker: Not the server.");
			return false;
		}

		if (!Referencer.TryGetReferencer(id, out var referencer))
		{
			Debug.Log("GetModuleLocker: No referencer found for player id " + id);
			return false;
		}

		if (!referencer.TryGetCachedComponent(out moduleLocker))
		{
			Debug.Log("GetModuleLocker: ModuleLocker component not found in referencer for player id " + id);
			return false;
		}

		return true;
	}

	public void FreezePlayer(ulong clientId, bool freezeCamera = false)
	{
		if (!IsServer)
		{
			Debug.Log("FreezePlayer: Not the server.");
			return;
		}

		if (!TryGetModuleLocker(clientId, out var moduleLocker))
		{
			Debug.Log($"FreezePlayer: No ModuleLocker found for ClientId {clientId}.");
			return;
		}

		moduleLocker.LockAllModulesRpc(freezeCamera);
	}

	public void UnFreezePlayer(ulong clientId)
	{
		if (!IsServer)
		{
			Debug.Log("UnFreezePlayer: Not the server.");
			return;
		}

		if (!TryGetModuleLocker(clientId, out var moduleLocker))
		{
			Debug.Log($"UnFreezePlayer: No ModuleLocker found for ClientId {clientId}.");
			return;
		}

		moduleLocker.UnlockAllModulesRpc();
	}

	public void TeleportPlayer(ulong clientId, Vector3 position, Quaternion rotation)
	{
		if (!IsServer)
		{
			Debug.Log("TeleportPlayer: Not the server.");
			return;
		}

		TeleportPlayerClientRpc(position, rotation, new RpcParams
		{
			Send = new RpcSendParams
			{
				Target = RpcTarget.Single(clientId, RpcTargetUse.Temp)
			}
		});
	}

	[Rpc(SendTo.SpecifiedInParams)]
	public void TeleportPlayerClientRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
	{
		var networkObject = NetworkManager.Singleton.LocalClient.PlayerObject;
		if (networkObject != null)
		{
			Debug.Log($"Teleporting {NetworkManager.LocalClientId} to {position}");
			if (!networkObject.TryGetComponent(out CharacterController controller))
			{
				Debug.Log("TeleportPlayerClientRpc: No CharacterController found.");
				return;
			}

			controller.enabled = false;

			if (networkObject.TryGetComponent<NetworkTransform>(out var networkTransform))
			{
				networkTransform.Teleport(position, rotation, Vector3.one);
			}

			controller.enabled = true;
		}
	}
}
