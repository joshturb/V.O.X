using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine.Rendering;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;

[Serializable]
public struct CustomRenderSettings
{
	[Header("Skybox")]
	public Material skyboxMaterial;
	
	[Header("Shadows")]
	public Color realtimeShadowColor;
	
	[Header("Environment Lighting")]
	public bool useSkyboxLighting;
	public Color ambientColor;
	public float ambientIntensityMultiplier;
	
	[Header("Environment Reflections")]
	public float reflectionIntensityMultiplier;
	
	[Header("Fog")]
	public bool enableFog;
	public Color fogColor;
	public FogMode fogMode;
	public float fogDensity;
}

public abstract class BaseMinigameManager : NetworkBehaviour
{
	public static event Action<BaseMinigameManager> OnMinigameInitialized;
	public static event Action<BaseMinigameManager> OnCountdownCompleted;
	public static NetworkVariable<float> Timer = new();
	public MiniGame Type;
	public BaseMinigameUI MinigameUI;
	public CustomRenderSettings renderSettings;
	public List<ulong> PlayersInRound = new();
	public PlayerModule[] requiredModules;
	public Transform[] playerPositions;
	public NetworkObject playerSlotPrefab;
	public NetworkObject slotParent;
	public float countdownDuration = 5f;
	public float minigameDuration = 60f;
	private float minigameStartTime;

	protected virtual void Awake()
	{
		#region RenderSettings Initialization
		RenderSettings.skybox = renderSettings.skyboxMaterial;
		RenderSettings.subtractiveShadowColor = renderSettings.realtimeShadowColor;
		if (renderSettings.useSkyboxLighting)
		{
			RenderSettings.ambientMode = AmbientMode.Skybox;
		}
		else
		{
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = renderSettings.ambientColor;
		}
		RenderSettings.ambientIntensity = renderSettings.ambientIntensityMultiplier;
		RenderSettings.reflectionIntensity = renderSettings.reflectionIntensityMultiplier;
		RenderSettings.fog = renderSettings.enableFog;
		RenderSettings.fogMode = renderSettings.fogMode;
		RenderSettings.fogColor = renderSettings.fogColor;
		RenderSettings.fogDensity = renderSettings.fogDensity;
		DynamicGI.UpdateEnvironment();
		#endregion
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;
			
		StopCoroutine(MinigameCoroutine());
	}

	public virtual void OnMinigameLoaded_S(MiniGame game, List<ulong> list)
	{
		if (game is MiniGame.Blank)
			return;

		PlayersInRound = list;

		if (playerPositions.Length > 0)
		{
			for (int i = playerPositions.Length - 1; i > 0; i--)
			{
				int j = UnityEngine.Random.Range(0, i + 1);
				(playerPositions[j], playerPositions[i]) = (playerPositions[i], playerPositions[j]);
			}

			for (int i = 0; i < PlayersInRound.Count; i++)
			{
				SpawnSlot(PlayersInRound[i]);

				if (i < playerPositions.Length)
				{
					TeleportPlayer(PlayersInRound[i], playerPositions[i].position, playerPositions[i].rotation);
				}
			}
		}

		OnMinigameInitializedRpc(NetworkManager.Singleton.ServerTime.TimeAsFloat);
		StartMinigame();
	}

	[Rpc(SendTo.Everyone)]
	private void OnMinigameInitializedRpc(float minigameStartTime)
	{
		this.minigameStartTime = minigameStartTime;
		MinigameUI.Initialize(this);
		OnMinigameInitialized?.Invoke(this);
	}

	private void SpawnSlot(ulong clientId)
	{
		var obj = Instantiate(playerSlotPrefab, slotParent.transform);
		if (!obj.TryGetComponent(out Slot slotComponent))
		{
			Debug.LogError("Failed to instantiate player slot.");
			return;
		}
		obj.Spawn();

		if (!obj.TrySetParent(slotParent.transform))
		{
			Debug.LogError("Failed to set parent for player slot.");
			return;
		}

		slotComponent.SetOccupant(clientId);
	}

	public virtual void StartMinigame()
	{
		StartCoroutine(MinigameCoroutine());
	}

	public virtual void EndMinigame()
	{
		if (!IsServer)
			return;

		var playerByScore = CalculateScores();
		GameManager.Instance.EndMinigame(playerByScore);
		PlayersInRound.Clear();
	}

	private IEnumerator MinigameCoroutine()
	{
		foreach (var item in PlayersInRound)
		{
			FreezePlayer(item);
		}

		while (NetworkManager.Singleton.ServerTime.TimeAsFloat - minigameStartTime < countdownDuration)
		{
			Timer.Value = countdownDuration - (NetworkManager.Singleton.ServerTime.TimeAsFloat - minigameStartTime);
			yield return null;
		}

		Timer.Value = 0f;
		OnCountdownCompleteRpc();

		foreach (var item in PlayersInRound)
		{
			UnFreezePlayer(item);
		}

		float startTime = NetworkManager.Singleton.ServerTime.TimeAsFloat;
		while (NetworkManager.Singleton.ServerTime.TimeAsFloat - startTime < minigameDuration && PlayerManager.GetAlivePlayerCount() > 1)
		{
			Timer.Value = minigameDuration - (NetworkManager.Singleton.ServerTime.TimeAsFloat - startTime);
			yield return null;
		}

		if (PlayerManager.GetAlivePlayerCount() == 1)
		{
			yield return new WaitForSeconds(5f);
		}

		EndMinigame();
	}

	protected bool TryGetModuleLocker(ulong id, out ModuleLocker moduleLocker)
	{
		moduleLocker = null;

		if (!IsServer)
		{
			Debug.Log("GetModuleLocker: Not the server.");
			return false;
		}

		if (!PlayersInRound.Contains(id))
		{
			Debug.Log("GetModuleLocker: Player id " + id + " not in PlayersInRound.");
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

		Debug.Log("GetModuleLocker: Found ModuleLocker for player id " + id);
		return true;
	}

	protected void FreezePlayer(ulong clientId, bool freezeCamera = false)
	{
		if (!IsServer)
		{
			Debug.Log("FreezePlayer: Not the server.");
			return;
		}

		if (!PlayersInRound.Contains(clientId))
		{
			Debug.Log($"FreezePlayer: ClientId {clientId} not in PlayersInRound.");
			return;
		}

		if (!TryGetModuleLocker(clientId, out var moduleLocker))
		{
			Debug.Log($"FreezePlayer: No ModuleLocker found for ClientId {clientId}.");
			return;
		}

		moduleLocker.LockAllModulesRpc(freezeCamera);
	}

	protected void UnFreezePlayer(ulong clientId)
	{
		if (!IsServer)
		{
			Debug.Log("UnFreezePlayer: Not the server.");
			return;
		}

		if (!PlayersInRound.Contains(clientId))
		{
			Debug.Log($"UnFreezePlayer: ClientId {clientId} not in PlayersInRound.");
			return;
		}

		if (!TryGetModuleLocker(clientId, out var moduleLocker))
		{
			Debug.Log($"UnFreezePlayer: No ModuleLocker found for ClientId {clientId}.");
			return;
		}

		moduleLocker.UnlockAllModulesRpc();
	}

	protected void TeleportPlayer(ulong clientId, Vector3 position, Quaternion rotation)
	{
		if (!IsServer)
		{
			Debug.Log("TeleportPlayer: Not the server.");
			return;
		}

		if (!PlayersInRound.Contains(clientId))
		{
			Debug.Log($"TeleportPlayer: ClientId {clientId} not in PlayersInRound.");
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
	private void TeleportPlayerClientRpc(Vector3 position, Quaternion rotation, RpcParams rpcParams = default)
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

	[Rpc(SendTo.Everyone)]
	private void OnCountdownCompleteRpc()
	{
		OnCountdownCompleted?.Invoke(this);
	}

	public abstract List<ulong> CalculateScores();
}

