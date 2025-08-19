using System.Collections.Generic;

public class TheBlankManager : BaseMinigameManager
{
	public override List<ulong> CalculateScores()
	{
		return null;
	}

	public override void StartMinigame()
	{
		if (!IsServer)
			return;

		GameManager.IsGameRunning.Value = true;
		GameManager.Instance.EndMinigame(null);
	}
}
