using UnityEngine.Rendering;
using Unity.Netcode;
using UnityEngine;

public class TheBlankManager : NetworkSingleton<TheBlankManager>
{
	public CustomRenderSettings renderSettings;
	public PlayerModule[] requiredModules;
	public Transform[] playerPositions;
	public Transform[] podiumPositions;

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
		RenderSettings.fog = renderSettings.fogSettings.enableFog;
		RenderSettings.fogMode = renderSettings.fogSettings.fogMode;
		RenderSettings.fogColor = renderSettings.fogSettings.fogColor;
		RenderSettings.fogDensity = renderSettings.fogSettings.fogDensity;
		RenderSettings.fogStartDistance = renderSettings.fogSettings.linearStart;
		RenderSettings.fogEndDistance = renderSettings.fogSettings.linearEnd;
		DynamicGI.UpdateEnvironment();
		#endregion
	}

	private void Start()
	{
		int index;
		Transform[] array;
		
		int placement = GameManager.Instance.GetPlacement(NetworkManager.LocalClientId);
		if (placement == -1 || placement >= 3)
		{
			index = Random.Range(0, playerPositions.Length);
			array = playerPositions;
		}
		else
		{
			index = placement;
			array = podiumPositions;
		}

		GameManager.Instance.TeleportPlayerClientRpc(array[index].position, array[index].rotation, new RpcParams
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
