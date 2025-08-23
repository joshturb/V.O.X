using UnityEngine.Animations;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using System;

// Spectating listens to playermanager, and spectating will spawn a spectator client (this is the playerObject as the player prefab is despawned)
public class Spectator : NetworkBehaviour
{
	public PositionConstraint positionConstraint;
	public CinemachineCamera spectatingCamera;
	[SerializeField] private float verticalOffset = 1;
	[SerializeField] private float rotationSpeed = 5f;
	private Vector2 currentRotation = Vector2.zero;

	void Start()
	{
		if (!IsOwner)
			return;

		NextPlayerRpc();
		spectatingCamera.Priority = -1;
	}

	private void Update()
	{
		if (!IsOwner)
			return;

		if (InputHandler.Instance.playerActions.Next.WasPressedThisFrame())
		{
			NextPlayerRpc();
		}
		else if (InputHandler.Instance.playerActions.Previous.WasPressedThisFrame())
		{
			PreviousPlayerRpc();
		}
	}

	private void LateUpdate()
	{
		if (!IsOwner)
			return;

		Vector2 mouseInput = InputHandler.Instance.playerActions.Look.ReadValue<Vector2>();

		// accumulate input
		currentRotation.x += mouseInput.x * rotationSpeed * Time.deltaTime;
		currentRotation.y += -mouseInput.y * rotationSpeed * Time.deltaTime;

		// optional clamp for vertical rotation
		currentRotation.y = Mathf.Clamp(currentRotation.y, -90f, 90f);

		transform.localRotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0);
	}

	[Rpc(SendTo.Server)]
	public void NextPlayerRpc()
	{
		var allAlivePlayers = Spectating.GetAlivePlayersNetworkObjects();
		int currentIndex = Array.IndexOf(allAlivePlayers, transform);
		int nextIndex = currentIndex >= 0 ? (currentIndex + 1) % allAlivePlayers.Length : 0;

		SetConstraintsRpc(allAlivePlayers[nextIndex]);
	}

	[Rpc(SendTo.Server)]
	public void PreviousPlayerRpc()
	{
		var allAlivePlayers = Spectating.GetAlivePlayersNetworkObjects();
		int currentIndex = Array.IndexOf(allAlivePlayers, transform);
		int previousIndex = currentIndex >= 0
			? (currentIndex - 1 + allAlivePlayers.Length) % allAlivePlayers.Length
			: allAlivePlayers.Length - 1;

		SetConstraintsRpc(allAlivePlayers[previousIndex]);
	}

	[Rpc(SendTo.Everyone)]
	private void SetConstraintsRpc(NetworkObjectReference networkObjectReference)
	{
		if (!networkObjectReference.TryGet(out var targetObject))
			return;

		if (positionConstraint.sourceCount > 0)
		{
			positionConstraint.RemoveSource(0);
		}
		positionConstraint.AddSource(new ConstraintSource
		{
			weight = 1f,
			sourceTransform = targetObject.transform
		});
		positionConstraint.translationOffset = new Vector3(0, verticalOffset, 0);
	}
}
