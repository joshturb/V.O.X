using AYellowpaper.SerializedCollections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using System;

public enum MiniGame
{
	Blank,
	Replicate,
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

    public static event Action<ulong, int> OnScoreUpdated;
	public static event Action<ulong, int> OnPlayerLivesChanged;
	public static event Action<MiniGame, List<ulong>> OnMinigameLoaded;
	public static event Action<MiniGame> OnMinigameUnloaded;
	public static bool CanPlayersJoin = true;

	[Header("Scene Configuration")]
	public SerializedDictionary<MiniGame, string> minigameScenes;
	public NetworkVariable<MiniGame> PreviousMinigame = new(MiniGame.Blank);
	public NetworkVariable<MiniGame> CurrentMinigame = new(MiniGame.Blank);
	[Range(0.01f, 10)] public float sceneTransitionDelay = 1f;


	[Header("Scoring")]
	public int pointsPerRound = 10;
	public Vector2Int pointsPerScore = new(10, 1);

	private BaseMinigameManager _currentMinigameManager;
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

		startCoroutine = StartCoroutine(MinigameCoroutine(MiniGame.Blank));

		NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;
		NetworkManager.Singleton.SceneManager.OnUnloadComplete += OnSceneUnloaded;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (!IsServer)
			return;

		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
			return;
			
		NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;
		NetworkManager.Singleton.SceneManager.OnUnloadComplete -= OnSceneUnloaded;
	}

	public void EndMinigame(List<ulong> playersByScore)
	{
		if (!IsServer)
			return;

		if (playersByScore != null && CurrentMinigame.Value != MiniGame.Blank)
		{
			foreach (var playerId in playersByScore)
			{
				int index = playersByScore.IndexOf(playerId);
				int totalPlayers = playersByScore.Count;
				int score = (totalPlayers == 1)
					? pointsPerScore.x
					: Mathf.RoundToInt(Mathf.Lerp(pointsPerScore.x, pointsPerScore.y, index / (float)(totalPlayers - 1)));

				if (PlayerManager.GetPlayerLives(playerId) > 0)
				{
					score += pointsPerRound;
				}

				AddScore(playerId, score);
			}
		}

		OnMinigameUnloaded?.Invoke(CurrentMinigame.Value);
		Scene sceneToUnload = SceneManager.GetSceneByName(minigameScenes[CurrentMinigame.Value]);
		NetworkManager.Singleton.SceneManager.UnloadScene(sceneToUnload);
	}

	private void OnSceneUnloaded(ulong clientId, string sceneName)
	{
		MiniGame nextMinigame = GetRandomMinigame();

		if (startCoroutine != null)
		{
			StopCoroutine(startCoroutine);
		}

		startCoroutine = StartCoroutine(MinigameCoroutine(nextMinigame));
	}

	private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
	{
		List<ulong> connectedClients = NetworkManager.Singleton.ConnectedClientsIds.ToList();
		OnMinigameLoaded?.Invoke(CurrentMinigame.Value, connectedClients);

		_currentMinigameManager = FindFirstObjectByType<BaseMinigameManager>();
		if (_currentMinigameManager != null)
		{
			_currentMinigameManager.OnMinigameLoaded(CurrentMinigame.Value, connectedClients);
		}

		print($"Current Minigame Manager: {_currentMinigameManager?.GetType().Name ?? "None"}");
	}

	private IEnumerator MinigameCoroutine(MiniGame miniGame)
	{
		if (CurrentMinigame.Value != MiniGame.Blank)
		{
			PreviousMinigame.Value = CurrentMinigame.Value;
		}
		CurrentMinigame.Value = miniGame;

		yield return new WaitForSeconds(sceneTransitionDelay);
		NetworkManager.Singleton.SceneManager.LoadScene(minigameScenes[miniGame], LoadSceneMode.Additive);
	}

	private MiniGame GetRandomMinigame()
	{
		var availableGames = Enum.GetValues(typeof(MiniGame));
		var validGames = new List<MiniGame>();

		// Collect all valid minigames (excluding Blank, the previous game, and ensuring a scene exists in the dictionary)
		foreach (MiniGame game in availableGames)
		{
			if (game != MiniGame.Blank && game != PreviousMinigame.Value && minigameScenes.ContainsKey(game))
				validGames.Add(game);
		}

		if (validGames.Count == 0)
		{
			Debug.LogWarning("No valid minigames available, returning first non-Blank game with a scene.");
			// Ensure a non-Blank game is returned that has an associated scene
			foreach (MiniGame game in availableGames)
			{
				if (game != MiniGame.Blank && minigameScenes.ContainsKey(game))
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
        OnScoreUpdated?.Invoke(clientId, PlayerScores[clientId]);
    }

    public int GetScore(ulong clientId)
    {
        return PlayerScores.TryGetValue(clientId, out var score) ? score : 0;
    }

	[Rpc(SendTo.Everyone)]
	private void NotifyLivesChangeRpc(ulong id, int lives)
	{
		OnPlayerLivesChanged?.Invoke(id, lives);
	}
}
