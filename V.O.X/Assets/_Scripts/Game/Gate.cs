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
	public static event Action<ulong, GateType> OnPlayerEnteredGate_S;

	void OnTriggerEnter(Collider collider)
	{
		if (!IsServer)
			return;

		if (!collider.CompareTag("Player"))
		{
			Debug.LogWarning("Non-player object entered gate.");
			return;
		}

		if (!collider.TryGetComponent(out NetworkObject networkObject))
		{
			Debug.LogError("Collider does not have a NetworkObject component.");
			return;
		}

		OnPlayerEnteredGate_S?.Invoke(networkObject.OwnerClientId, gateType);
	}
}
