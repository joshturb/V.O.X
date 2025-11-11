using System.Collections.Generic;

public class KnockoffManager : BaseMinigameManager
{
	public override List<ulong> CalculateScores()
	{
		return new List<ulong>(GameManager.PlayerScores.Keys);
	}
}
