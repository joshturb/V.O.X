using Unity.Netcode;
using UnityEngine;
using System;

public class Gate : NetworkBehaviour
{
	[Serializable]
	public enum GateType
	{
		Finish,
		Death
	}
	public GateType gateType;
	public static event Action<ulong, GateType> OnPlayerEnteredGate;

	void OnTriggerEnter(Collider collider)
	{
		if (!IsServer)
			return;

		if (!collider.CompareTag("Player"))
			return;

		if (!collider.TryGetComponent(out NetworkObject networkObject))
		{
			Debug.LogError("Collider does not have a NetworkObject component.");
			return;
		}

		Debug.Log($"Player {networkObject.OwnerClientId} entered {gateType} gate.");
		OnPlayerEnteredGate?.Invoke(networkObject.OwnerClientId, gateType);
	}
}
