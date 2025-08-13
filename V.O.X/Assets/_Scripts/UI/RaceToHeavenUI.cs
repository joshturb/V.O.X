using System.Collections;

public class RaceToHeavenUI : BaseMinigameUI
{
	private RaceToHeavenManager raceManager;

	public override void Initialize(BaseMinigameManager manager)
	{
		base.Initialize(manager);
		raceManager = manager as RaceToHeavenManager;
	}

	public override IEnumerator UICoroutine()
	{
		yield return base.UICoroutine();
	}
}