using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Gravity Module")]
public class GravityModule : PlayerModule
{
    public LayerMask groundMask;
    public float detectionLength = 0.5f;
    public float gravityMultiplier = 1f;
    private float gravity;

    public bool isLocked;
    public override bool IsLocked { get => isLocked; set => isLocked = value; }

    private bool _isInitialized;

    public override bool IsInitialized { get => _isInitialized; set => _isInitialized = value; }

    public override void InitializeModule(FPCModule fPCModule)
    {
        IsLocked = false;
    }

    public override void OnModuleRemoved(FPCModule fPCModule) {	}

    public override void HandleInput(FPCModule fPCModule)
    {
        fPCModule.isGrounded = GroundedCheck(fPCModule);
        gravity = Physics.gravity.y * gravityMultiplier;
    }

    public bool GroundedCheck(FPCModule fPCModule)
    {
        Debug.DrawRay(fPCModule.transform.position, Vector3.down * detectionLength, Color.red);
        return Physics.Raycast(fPCModule.transform.position, Vector3.down, detectionLength, groundMask, QueryTriggerInteraction.Ignore);
    }


    public override void UpdateModule(FPCModule fPCModule)
    {
        if (fPCModule.isGrounded)
        {
            fPCModule.clientMovement.y = 0;
        }
        else
        {
            fPCModule.clientMovement.y += gravity * Time.deltaTime;
            fPCModule.clientMovement.y = fPCModule.clientMovement.y < gravity ? gravity : fPCModule.clientMovement.y;
        }
    }
}
