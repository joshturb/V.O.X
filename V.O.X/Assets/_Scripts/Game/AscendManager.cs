using System.Collections.Generic;
using UnityEngine;

public class AscendManager : BaseMinigameManager
{
	[SerializeField] private List<ulong> finishedPlayers = new();

	protected override void Start()
	{
		base.Start();
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate_S += OnPlayerEnteredGate_SHandler;
	}

	public override void OnNetworkDespawn()
	{
		base.OnNetworkDespawn();
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate_S -= OnPlayerEnteredGate_SHandler;
	}

	private void OnPlayerEnteredGate_SHandler(ulong arg1, Gate.GateType type)
	{
		if (type == Gate.GateType.Finish)
		{
			finishedPlayers.Add(arg1);
		}
	}

	public override List<ulong> CalculateScores()
	{
		return finishedPlayers;
	}
}
