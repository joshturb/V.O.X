using System.Collections.Generic;
using UnityEngine.Rendering;
using Unity.Netcode;
using UnityEngine;
using System;
using System.Collections;
using System.Linq;

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
	public float linearStart;
	public float linearEnd;
}

public abstract class BaseMinigameManager : NetworkBehaviour
{
	public static event Action<BaseMinigameManager> OnMinigameInitialized;
	public static event Action<BaseMinigameManager> OnCountdownCompleted;
	public static NetworkVariable<float> Timer = new();
	public BaseMinigameUI MinigameUI;
	public CustomRenderSettings renderSettings;
	public List<ulong> PlayersInRound = new();
	public PlayerModule[] requiredModules;
	public Transform[] playerPositions;
	public float countdownDuration = 5f;
	public float minigameDuration = 60f;
	private float minigameStartTime;
	private bool isInitialized;

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
		RenderSettings.fogStartDistance = renderSettings.linearStart;
		RenderSettings.fogEndDistance = renderSettings.linearEnd;
		DynamicGI.UpdateEnvironment();
		#endregion
	}

	void Start()
	{
		if (!IsServer)
			return;

		OnMinigameLoaded_S(GameManager.Instance.CurrentMinigame.Value, 	NetworkManager.Singleton.ConnectedClientsIds.ToList());
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
		if (isInitialized)
			return;

		isInitialized = true;
		PlayersInRound = list;

		if (playerPositions.Length > 0)
		{
			for (int i = playerPositions.Length - 1; i > 0; i--)
			{
				int j = UnityEngine.Random.Range(0, i + 1);
				(playerPositions[j], playerPositions[i]) = (playerPositions[i], playerPositions[j]);
			}

			print(PlayersInRound.Count);
			for (int i = 0; i < PlayersInRound.Count; i++)
			{
				if (i < playerPositions.Length)
				{
					GameManager.Instance.TeleportPlayer(PlayersInRound[i], playerPositions[i].position, playerPositions[i].rotation);
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
		GameManager.Instance.currentMinigameManager = this;
		MinigameUI.Initialize(this);
		OnMinigameInitialized?.Invoke(this);
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
		yield return null;

		foreach (var item in PlayersInRound)
		{
			GameManager.Instance.FreezePlayer(item);
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
			GameManager.Instance.UnFreezePlayer(item);
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

	[Rpc(SendTo.Everyone)]
	private void OnCountdownCompleteRpc()
	{
		OnCountdownCompleted?.Invoke(this);
	}

	public abstract List<ulong> CalculateScores();
}

