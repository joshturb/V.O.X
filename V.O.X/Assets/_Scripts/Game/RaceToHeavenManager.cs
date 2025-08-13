using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceToHeavenManager : BaseMinigameManager
{
	[SerializeField] private List<ulong> finishedPlayers = new();

	private void Start()
	{
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate += (id, gateType) =>
		{
			if (gateType == Gate.GateType.Finish)
			{
				SetPlayerFinished(id);
				// spectate
				Debug.Log($"Player {id} finished the race!");
			}
		};

		StartCoroutine(nameof(MinigameCoroutine));
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (!IsServer)
			return;

		Gate.OnPlayerEnteredGate -= (id, gateType) =>
		{
			if (gateType == Gate.GateType.Finish)
			{
				SetPlayerFinished(id);
				// spectate
				Debug.Log($"Player {id} finished the race!");
			}
		};
	}

	private IEnumerator MinigameCoroutine()
	{
		yield return null;
	}

	private void SetPlayerFinished(ulong id)
	{
		finishedPlayers.Add(id);
	}

	public override List<ulong> CalculateScores()
	{
		return finishedPlayers;
	}
}
