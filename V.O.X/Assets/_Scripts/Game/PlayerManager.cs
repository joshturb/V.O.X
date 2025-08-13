using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using UnityEngine;

public class PlayerManager : NetworkSingleton<PlayerManager>
{
	[SerializeField] private SerializedDictionary<ulong, int> _healthById = new();
	public static event System.Action<ulong, int> OnPlayerLivesChanged;

	void Start()
	{
		_healthById[NetworkManager.LocalClientId] = 3;
		OnPlayerLivesChanged?.Invoke(NetworkManager.LocalClientId, 3);

		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate += (id, gateType) =>
		{
			if (gateType == Gate.GateType.Death)
			{
				SetLives(id, 0);
				print("Player " + id + " has died.");
			}
		};

		NetworkManager.Singleton.OnClientConnectedCallback += (id) => { SetLives(id, 3); };
		NetworkManager.Singleton.OnClientDisconnectCallback += (id) => { _healthById.Remove(id); };
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate -= (id, gateType) =>
		{
			if (gateType == Gate.GateType.Death)
			{
				SetLives(id, 0);
			}
			
		};

		NetworkManager.Singleton.OnClientConnectedCallback -= (id) => { SetLives(id, 3); };
		NetworkManager.Singleton.OnClientDisconnectCallback -= (id) => { _healthById.Remove(id); };
	}

	private static int GetPlayersCountAlive()
	{
		int count = 0;
		foreach (var lives in Instance._healthById.Values)
		{
			if (lives > 0) count++;
		}
		return count;
	}

	public static int GetPlayerLives(ulong id)
	{
		return Instance._healthById.TryGetValue(id, out var lives) ? lives : -1;
	}

	private void SetLives(ulong id, int lives)
	{
		if (!IsServer)
			return;

		// add some sort of verification here

		SetPlayerLivesRpc(id, lives);
	}

	[Rpc(SendTo.Everyone)]
	private void SetPlayerLivesRpc(ulong id, int lives)
	{
		_healthById[id] = lives;
		OnPlayerLivesChanged?.Invoke(id, lives);
	}
}
