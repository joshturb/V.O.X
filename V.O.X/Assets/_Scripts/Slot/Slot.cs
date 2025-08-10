using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class Slot : NetworkBehaviour
{
	public NetworkVariable<ulong> OccupantId = new(99);
	public List<BaseSlotModule> slotModules = new();
	public static event Action<Slot> OnSlotInitialized_E;

	void Awake()
	{
		List<BaseSlotModule> slotModules = new(GetComponents<BaseSlotModule>());
		SetModules(slotModules);

		for (int i = 0; i < slotModules.Count; i++)
		{
			slotModules[i].Initialize(this);
		}

		OnSlotInitialized_E?.Invoke(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		foreach (var module in slotModules)
		{
			module.UnsubscribeFromEvents();
		}
	}

	public void SetOccupant(ulong clientId)
	{
		if (!IsServer)
			return;

		OccupantId.Value = clientId;
	}

	public void SetModules(List<BaseSlotModule> modules)
	{
		slotModules = modules;
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
			if (s.OccupantId.Value == slotId)
			{
				slot = s;
				return true;
			}
		}
		slot = null;
		return false;
	}
}
