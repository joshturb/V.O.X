using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

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

		foreach (var item in GameManager.PlayerDatas)
		{
			if (Referencer.TryGetReferencer(item.clientId, out referencer))
			{
				if (referencer.TryGetCachedComponent<PlayerStatsUI>(out var playerStatsUI))
				{
					playerStatsUI.playerNameText.text = item.playerName.ToString();
				}
			}
		}

		if (!referencer.TryGetCachedComponent(out PlayerStats playerStats))
		{
			Debug.LogError("PlayerStats component not found!");
		}

		playerStats.IsTalking.OnValueChanged += OnIsTalkingChanged;
		PlayerManager.OnPlayerDeath_E += (id) => UpdateHealthUI(id, true);
		PlayerManager.OnPlayerRevive_E += (id) => UpdateHealthUI(id, false);
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
		PlayerManager.OnPlayerDeath_E -= (id) => UpdateHealthUI(id, true);
		PlayerManager.OnPlayerRevive_E -= (id) => UpdateHealthUI(id, false);
	}

	private void OnIsTalkingChanged(bool previousValue, bool newValue)
	{
		isTalkingImage.enabled = newValue;
	}

	private void UpdateHealthUI(ulong id, bool alive)
	{
		if (id != OwnerClientId)
			return;

		UpdateLivesUIRpc(alive);
	}

	[Rpc(SendTo.Everyone)]
	private void UpdateLivesUIRpc(bool alive)
	{
		healthImage.sprite = alive ? GlobalUIManager.Instance.aliveImage : GlobalUIManager.Instance.deadImage;
	}
}
