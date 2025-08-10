using Unity.Netcode;
using TMPro;

public class PlayerStatsUI : NetworkBehaviour
{
	public TMP_Text playerNameText;
	private Referencer referencer;

	public override void OnNetworkSpawn()
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
	}
}
