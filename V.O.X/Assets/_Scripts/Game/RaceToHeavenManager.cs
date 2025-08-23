using System.Collections.Generic;
using UnityEngine;

public class RaceToHeavenManager : BaseMinigameManager
{
	[SerializeField] private List<ulong> finishedPlayers = new();

	public override List<ulong> CalculateScores()
	{
		return finishedPlayers;
	}
}
