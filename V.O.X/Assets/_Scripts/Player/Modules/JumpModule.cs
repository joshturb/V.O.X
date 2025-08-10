using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Jump Module")]
public class JumpModule : PlayerModule
{
    public float jumpHeight = 1f;
    public float staminaDecrease = 5f;
    private bool isJumping;

    private bool isLocked;
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
        isJumping = InputHandler.Instance.playerActions.Jump.WasPressedThisFrame() && fPCModule.isGrounded && fPCModule.currentStamina > staminaDecrease;
    }

    public override void UpdateModule(FPCModule fPCModule)
    {
        if (isJumping)
        {
            Jump(fPCModule);
        }
    }

    private void Jump(FPCModule fPCModule)
    {
        fPCModule.clientMovement.y = 0;

        fPCModule.clientMovement.y += Mathf.Sqrt(jumpHeight * -2.0f * Physics.gravity.y);

        fPCModule.SetStamina(fPCModule.currentStamina - staminaDecrease);
    }
}