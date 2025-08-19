using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public abstract class BaseMinigameUI : NetworkBehaviour
{
	protected BaseMinigameManager MinigameManager { get; private set; }
	public GameObject countdownUI;
	public TMP_Text countdownTimerText;
	public bool countdownEnded = false;

	public virtual void Initialize(BaseMinigameManager manager)
	{
		countdownUI.SetActive(false);
		MinigameManager = manager;

		BaseMinigameManager.OnCountdownCompleted += HandleCountdownCompleted;
		if (IsServer)
		{
			BaseMinigameManager.Timer.OnValueChanged += HandleTimerValueChanged;
			StartCoroutine(UICoroutine());
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		BaseMinigameManager.OnCountdownCompleted -= HandleCountdownCompleted;

		if (!IsServer)
			return;

		BaseMinigameManager.Timer.OnValueChanged -= HandleTimerValueChanged;
	}

	private void HandleTimerValueChanged(float previousValue, float newValue)
	{
		if (countdownEnded)
			return;

		string formattedTime = newValue.ToString("F0");
		UpdateTimerRpc(new FixedString32Bytes(formattedTime));
	}

	private void HandleCountdownCompleted(BaseMinigameManager manager)
	{
		countdownEnded = true;
		countdownUI.SetActive(false);
		countdownTimerText.text = string.Empty;
	}

	[Rpc(SendTo.Everyone)]
	private void UpdateTimerRpc(FixedString32Bytes timeAsString)
	{
		countdownTimerText.text = timeAsString.ToString();
	}

	public virtual IEnumerator UICoroutine()
	{
		yield return new WaitUntil(() => countdownEnded);
	}
}