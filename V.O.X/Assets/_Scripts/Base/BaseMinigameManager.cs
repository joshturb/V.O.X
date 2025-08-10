using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine.Rendering;
using Unity.Netcode;
using UnityEngine;
using System;

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
	public CustomRenderSettings renderSettings;
	public List<ulong> PlayersInRound = new();
	public PlayerModule[] requiredModules;
	public Transform[] playerPositions;
	public NetworkObject playerSlotPrefab;
	public NetworkObject slotParent;

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

	public virtual void OnMinigameLoaded(MiniGame game, List<ulong> list)
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

		OnMinigameInitializedRpc();
		StartMinigame();
	}


	[Rpc(SendTo.Everyone)]
	private void OnMinigameInitializedRpc()
	{
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

	public virtual void StartMinigame() { }
	
	public virtual void EndMinigame()
	{
		if (!IsServer)
			return;

		PlayersInRound.Clear();
		var playerByScore = CalculateScores();
		GameManager.Instance.EndMinigame(playerByScore);
	}

	protected ModuleLocker GetModuleLocker(ulong id)
	{
		if (!IsServer)
		{
			Debug.Log("GetModuleLocker: Not the server.");
			return null;
		}

		if (!PlayersInRound.Contains(id))
		{
			Debug.Log("GetModuleLocker: Player id " + id + " not in PlayersInRound.");
			return null;
		}

		if (!Referencer.TryGetReferencer(id, out var referencer))
		{
			Debug.Log("GetModuleLocker: No referencer found for player id " + id);
			return null;
		}

		if (!referencer.TryGetCachedComponent<ModuleLocker>(out var moduleLocker))
		{
			Debug.Log("GetModuleLocker: ModuleLocker component not found in referencer for player id " + id);
			return null;
		}

		Debug.Log("GetModuleLocker: Found ModuleLocker for player id " + id);
		return moduleLocker;
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

	public abstract List<ulong> CalculateScores();
}

