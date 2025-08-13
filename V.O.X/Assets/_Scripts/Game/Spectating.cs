using Unity.Netcode;
using UnityEngine;
using System;

public class Spectating : NetworkSingleton<Spectating>
{
	public NetworkList<ulong> spectatingIds;

	protected override void Awake()
	{
		base.Awake();
		spectatingIds = new NetworkList<ulong>();
	}

	void Start()
	{
		spectatingIds.OnListChanged += HandleSpectatingIdsChanged;
	}

	private void HandleSpectatingIdsChanged(NetworkListEvent<ulong> changeEvent)
	{
		if (changeEvent.Value != NetworkManager.LocalClientId)
			return;

		// local player
	}

	private void Update()
	{
		if (spectatingIds.Count == 0)
			return;

		if (spectatingIds.Contains(NetworkManager.LocalClientId))
		{
			if (InputHandler.Instance.playerActions.Next.WasPressedThisFrame())
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}
	}

	public void AddSpectator(ulong clientId)
	{
		if (!IsServer)
			return;

		if (!spectatingIds.Contains(clientId))
		{
			spectatingIds.Add(clientId);
		}
	}

	public void RemoveSpectator(ulong clientId)
	{
		if (!IsServer)
			return;

		if (spectatingIds.Contains(clientId))
		{
			spectatingIds.Remove(clientId);
		}
	}
}
