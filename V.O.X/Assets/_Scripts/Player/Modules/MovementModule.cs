using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Movement Module")]
public class MovementModule : PlayerModule
{
    public float walkingSpeed = 2f;
    public float sprintingSpeed = 5f;  
    public float walkingStaminaMultiplier = 0f;
    public float sprintingStaminaMultiplier = 2f;

	private Vector2 input;
    private bool isSprinting;
	private bool sprintToggleState = false;
	public bool toggleSprint = false;

    public bool isLocked;
    public override bool IsLocked { get => isLocked; set => isLocked = value; }

    private bool isInitialized;
    public override bool IsInitialized { get => isInitialized; set => isInitialized = value; }

    public override void InitializeModule(FPCModule fPCModule)
    {
		IsLocked = false;
    }

    public override void OnModuleRemoved(FPCModule fPCModule) { }

    public override void HandleInput(FPCModule fPCModule)
    {
        input = InputHandler.Instance.playerActions.Move.ReadValue<Vector2>().normalized;
		
        bool sprintInput = InputHandler.Instance.playerActions.Sprint.IsPressed();

        if (toggleSprint)
        {
            if (sprintInput && !sprintToggleState)
            {
                isSprinting = !isSprinting; 
                sprintToggleState = true;
            }
            else if (!sprintInput)
            {
                sprintToggleState = false;
            }
        }
        else isSprinting = sprintInput && fPCModule.currentStamina > 5;

		if (input == Vector2.zero)
		{
			fPCModule.staminaReductionRate = 0;
			return;
		}
		
		if (isSprinting)
		{
			fPCModule.staminaReductionRate = sprintingStaminaMultiplier;
		}
		else
		{
			fPCModule.staminaReductionRate = walkingStaminaMultiplier;
		}
        
    }	

    public override void UpdateModule(FPCModule fPCModule)
    {
        if (!fPCModule._owner.IsOwner)
			return;
    
        Vector3 movement = input.y * fPCModule.transform.forward + input.x * fPCModule.transform.right;
        movement.y = 0;

		float speed = isSprinting && fPCModule.currentStamina > 5 ? sprintingSpeed : walkingSpeed;

		movement *= speed;

		fPCModule.clientMovement = new Vector3(movement.x, fPCModule.clientMovement.y, movement.z);
    }
}