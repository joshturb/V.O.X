using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GlobalUIManager : NetworkSingleton<GlobalUIManager>
{
	[SerializeField] private Image glitchImage;
	[SerializeField] private TMP_Text timerText;

	protected override void Awake()
	{
		base.Awake();
		NetworkManager.Singleton.SceneManager.OnUnload += OnSceneUnload;
		NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
		BaseMinigameManager.Timer.OnValueChanged += UpdateTimerUI;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
			return;

		NetworkManager.Singleton.SceneManager.OnUnload -= OnSceneUnload;
		NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
		BaseMinigameManager.Timer.OnValueChanged -= UpdateTimerUI;
	}

	private void UpdateTimerUI(float previousValue, float newValue)
	{
		if (!GameManager.Instance.currentMinigameManager.MinigameUI.countdownEnded)
			return;		

		timerText.text = newValue.ToString("F0");
	}

	private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
	{
		glitchImage.enabled = false;
	}

	private void OnSceneUnload(ulong clientId, string sceneName, AsyncOperation asyncOperation)
	{
		glitchImage.enabled = true;
		timerText.text = string.Empty;
	}
}
