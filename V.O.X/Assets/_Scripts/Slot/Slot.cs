using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Slot : NetworkBehaviour
{
	public List<BaseSlotModule> slotModules = new();

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		if (!IsOwner)
			return;

		slotModules = new(GetComponents<BaseSlotModule>());
		for (int i = 0; i < slotModules.Count; i++)
		{
			slotModules[i].Initialize(this);
		}

		PlayerManager.OnPlayerDeath_E += HandlePlayerDeathRpc;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsOwner)
			return;
		
		PlayerManager.OnPlayerDeath_E -= HandlePlayerDeathRpc;
	}

	void Update()
	{
		if (!IsOwner)
			return;

		foreach (var module in slotModules)
		{
			module.UpdateModule();
		}
	}

	[Rpc(SendTo.Server)]
	private void HandlePlayerDeathRpc(ulong id)
	{
		if (OwnerClientId == id)
			NetworkObject.Despawn(true);
	}

	public bool TryGetModule<T>(out T module) where T : BaseSlotModule
	{
		foreach (var m in slotModules)
		{
			if (m is T typedModule)
			{
				module = typedModule;
				return true;
			}
		}
		module = null;
		return false;
	}

	public static bool TryGetSlotById(ulong slotId, out Slot slot)
	{
		foreach (var s in FindObjectsByType<Slot>(FindObjectsSortMode.None))
		{
			if (s.OwnerClientId == slotId)
			{
				slot = s;
				return true;
			}
		}
		slot = null;
		return false;
	}
}
