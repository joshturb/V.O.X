using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GlobalUIManager : NetworkSingleton<GlobalUIManager>
{
	[SerializeField] private TMP_Text TimerText;
	[SerializeField] private Image glitchImage;
	[SerializeField] private Image healthImage;
	[SerializeField] private Sprite[] healthImages;

	protected override void Awake()
	{
		base.Awake();
		NetworkManager.Singleton.SceneManager.OnUnload += OnSceneUnload;
		NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
		PlayerManager.OnPlayerLivesChanged += UpdateHealthUI;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
			return;

		NetworkManager.Singleton.SceneManager.OnUnload -= OnSceneUnload;
		NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
		PlayerManager.OnPlayerLivesChanged -= UpdateHealthUI;
	}

	private void UpdateHealthUI(ulong id, int lives)
	{
		if (id == NetworkManager.LocalClientId)
		{
			healthImage.sprite = healthImages[Mathf.Clamp(lives, 0, healthImages.Length - 1)];
		}
	}

	private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
	{
		glitchImage.enabled = false;
	}

	private void OnSceneUnload(ulong clientId, string sceneName, AsyncOperation asyncOperation)
	{
		glitchImage.enabled = true;
	}
}
