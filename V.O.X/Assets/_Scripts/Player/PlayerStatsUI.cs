using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System;

public class PlayerStatsUI : NetworkBehaviour
{
	public Image isTalkingImage;
	public Image healthImage;
	public TMP_Text playerNameText;
	private Referencer referencer;

	private void Start()
	{
		if (!IsOwner)
			return;

		foreach (var item in PlayerStats.PlayerNamesById)
		{
			if (Referencer.TryGetReferencer(item.Key, out referencer))
			{
				if (referencer.TryGetCachedComponent<PlayerStatsUI>(out var playerStatsUI))
				{
					playerStatsUI.playerNameText.text = item.Value.ToString();
				}
			}
		}

		if (!referencer.TryGetCachedComponent(out PlayerStats playerStats))
		{
			Debug.LogError("PlayerStats component not found!");
		}

		playerStats.IsTalking.OnValueChanged += OnIsTalkingChanged;
		PlayerManager.OnPlayerLivesChanged += UpdateLivesUI;
	}

	public override void OnNetworkDespawn()
	{
		if (!IsOwner)
			return;

		if (!referencer.TryGetCachedComponent(out PlayerStats playerStats))
		{
			Debug.LogError("PlayerStats component not found!");
		}

		playerStats.IsTalking.OnValueChanged -= OnIsTalkingChanged;
		PlayerManager.OnPlayerLivesChanged -= UpdateLivesUI;
	}

	private void OnIsTalkingChanged(bool previousValue, bool newValue)
	{
		isTalkingImage.enabled = newValue;
	}

	private void UpdateLivesUI(ulong id, int lives)
	{
		if (id != OwnerClientId)
			return;

		UpdateLivesUIRpc(lives);
	}

	[Rpc(SendTo.Everyone)]
	private void UpdateLivesUIRpc(int lives)
	{
		healthImage.sprite = GlobalUIManager.Instance.healthImages[Mathf.Clamp(lives, 0, GlobalUIManager.Instance.healthImages.Length - 1)];
	}
}
