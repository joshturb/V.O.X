using Unity.Netcode;
using UnityEngine;
using System;

public class Interaction : NetworkBehaviour
{
	public static event Action<ulong, string> OnHoverTextChanged;
	public float interactionDistance = 3f;
	public LayerMask interactionLayerMask;
	public RaycastHit raycastHitResults;
	private string currentHoverText;
	private Referencer referencer;

	void Start()
	{
		if (!Referencer.TryGetReferencer(OwnerClientId, out referencer))
		{
			Debug.LogError("Referencer not found for the local client.");
			return;
		}
	}

	void Update()
	{
		if (!IsOwner)
			return;
		
		if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition),
			out raycastHitResults, interactionDistance, interactionLayerMask, QueryTriggerInteraction.Collide)
			|| raycastHitResults.collider == null)
			{
				if (currentHoverText != string.Empty)
				{
					currentHoverText = string.Empty;
					OnHoverTextChanged?.Invoke(OwnerClientId, string.Empty);
				}
				return;
			}

		var newHoverText = string.Empty;
		
		if (raycastHitResults.collider.TryGetComponent<IHoverText>(out var hoverText))
		{
			newHoverText = hoverText.GetHoverText(OwnerClientId);
		}
		else
		{
			var parentHoverText = raycastHitResults.collider.GetComponentInParent<IHoverText>();
			if (parentHoverText != null)
			{
				newHoverText = parentHoverText.GetHoverText(OwnerClientId);
			}
		}

		if (newHoverText != currentHoverText)
		{
			currentHoverText = newHoverText;
			OnHoverTextChanged?.Invoke(OwnerClientId, currentHoverText);
		}

		if (InputHandler.Instance.playerActions.Interact.WasPressedThisFrame())
		{
			if (!raycastHitResults.collider.TryGetComponent(out IInteractable interactable))
			{
				interactable = raycastHitResults.collider.GetComponentInParent<IInteractable>();
			}

			interactable?.Interact(referencer, raycastHitResults);
		}
	}
}
