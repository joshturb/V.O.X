using TMPro;
using Unity.Netcode;

public class InteractionUI : NetworkBehaviour
{
	public TMP_Text hoverText;

	void Start()
	{
		Interaction.OnHoverTextChanged += OnHoverTextChanged;
		HideHoverText();
	}

	public override void OnDestroy()
	{
		Interaction.OnHoverTextChanged -= OnHoverTextChanged;
	}

	private void OnHoverTextChanged(ulong id, string text)
	{
		if (id == NetworkManager.LocalClientId)
		{
			if (text == string.Empty)
			{
				HideHoverText();
				return;
			}

			ShowHoverText(text);
		}
	}

	public void ShowHoverText(string text)
	{
		hoverText.text = text;
		hoverText.gameObject.SetActive(true);
	}

	public void HideHoverText()
	{
		hoverText.text = "";
		hoverText.gameObject.SetActive(false);
	}

}
