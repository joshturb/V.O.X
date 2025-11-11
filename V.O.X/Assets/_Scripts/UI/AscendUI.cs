using System.Collections;

public class AscendUI : BaseMinigameUI
{
	private AscendManager raceManager;

	public override void Initialize(BaseMinigameManager manager)
	{
		base.Initialize(manager);
		raceManager = manager as AscendManager;
	}

	public override IEnumerator UICoroutine()
	{
		yield return base.UICoroutine();
	}
}