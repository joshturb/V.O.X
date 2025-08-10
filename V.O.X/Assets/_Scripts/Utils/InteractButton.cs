using UnityEngine.Events;
using Unity.Netcode;
using UnityEngine;

public class InteractButton : NetworkBehaviour, IInteractable, IHoverText
{
	public string hoverText = "Interact";
	public UnityEvent onInteract;

	public string GetHoverText(ulong id)
	{
		return hoverText;
	}

	public void Interact(Referencer referencer, RaycastHit hitInfo)
	{
		if (onInteract != null)
		{
			onInteract.Invoke();
		}
		else
		{
			Debug.LogWarning("No interaction defined for this button.");
		}
	}
}
