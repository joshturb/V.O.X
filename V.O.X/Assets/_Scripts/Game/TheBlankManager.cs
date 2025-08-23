using UnityEngine.Rendering;
using Unity.Netcode;
using UnityEngine;

public class TheBlankManager : NetworkSingleton<TheBlankManager>
{
	public CustomRenderSettings renderSettings;
	public PlayerModule[] requiredModules;
	public Transform[] playerPositions;

	protected override void Awake()
	{
		base.Awake();
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

	private void Start()
	{
		int index = Random.Range(0, playerPositions.Length);
		GameManager.Instance.TeleportPlayerClientRpc(playerPositions[index].position, playerPositions[index].rotation, new RpcParams
		{
			Send = new RpcSendParams
			{
				Target = RpcTarget.Single(NetworkManager.LocalClientId, RpcTargetUse.Temp)
			}
		});
	}

	public void StartGame()
	{
		if (!IsServer)
			return;

		GameManager.IsGameRunning.Value = true;
		NetworkManager.Singleton.SceneManager.UnloadScene(gameObject.scene);
	}
}
