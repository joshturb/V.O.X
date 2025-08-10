using Unity.Netcode;

public class InputHandler : NetworkBehaviour
{
	public static InputHandler Instance { get; private set; }

	public InputActions inputActions;
	public InputActions.PlayerActions playerActions;
	public InputActions.UIActions uiActions;

	public override void OnNetworkSpawn()
	{
		if (!IsLocalPlayer)
		{
			enabled = false;
		}

		Instance = this;

		inputActions = new();
		inputActions.Enable();

		playerActions = inputActions.Player;
		uiActions = inputActions.UI;
	}

	public override void OnNetworkDespawn()
	{
		if (!IsLocalPlayer)
			return;

		inputActions.Disable();
		inputActions.Dispose();
	}
}
