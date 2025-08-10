using System.Collections.Generic;

public class TheBlankManager : BaseMinigameManager
{
	public override List<ulong> CalculateScores()
	{
		return null;
	}

	public override void StartMinigame()
	{
		GameManager.Instance.EndMinigame(null);
	}
}
