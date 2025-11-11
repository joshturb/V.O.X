using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class GlobalUIManager : NetworkSingleton<GlobalUIManager>
{
	[SerializeField] private Image glitchImage;
	[SerializeField] private TMP_Text timerText;
	[SerializeField] private TMP_Text playerJoinText;
	private Coroutine playerJoinCoroutine;

	protected override void Awake()
	{
		base.Awake();
		NetworkManager.Singleton.SceneManager.OnUnload += OnSceneUnload;
		NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
		BaseMinigameManager.Timer.OnValueChanged += UpdateTimerUI;
	}

	void Start()
	{
		GameManager.PlayerDatas.OnListChanged += UpdatePlayerListUI;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
			return;

		NetworkManager.Singleton.SceneManager.OnUnload -= OnSceneUnload;
		NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
		GameManager.PlayerDatas.OnListChanged -= UpdatePlayerListUI;
		BaseMinigameManager.Timer.OnValueChanged -= UpdateTimerUI;
	}

	private void UpdatePlayerListUI(NetworkListEvent<PlayerData> changeEvent)
	{
		if (GameManager.Instance == null)
			return;

		// dont display join message to player joining
		if (NetworkManager.LocalClientId == changeEvent.Value.clientId)
			return;

		string text = string.Empty;
		if (changeEvent.Type == NetworkListEvent<PlayerData>.EventType.Add)
		{
			text = $"{changeEvent.Value.playerName} has joined.";
		}
		else if (changeEvent.Type == NetworkListEvent<PlayerData>.EventType.Remove)
		{
			text = $"{changeEvent.Value.playerName} has left.";
		}

		playerJoinText.transform.parent.gameObject.SetActive(true);
		playerJoinText.text = text;

		if (playerJoinCoroutine != null)
			StopCoroutine(playerJoinCoroutine);

		playerJoinCoroutine = StartCoroutine(ClearPlayerJoinTextCoroutine());
	}

	private IEnumerator ClearPlayerJoinTextCoroutine()
	{
		yield return new WaitForSeconds(3f);
		playerJoinText.transform.parent.gameObject.SetActive(false);
		playerJoinText.text = string.Empty;
		playerJoinCoroutine = null;
	}

	private void UpdateTimerUI(float previousValue, float newValue)
	{
		if (GameManager.Instance.currentMinigameManager == null)
			return;

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
