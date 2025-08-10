
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Camera Module")]
public class CameraModule : PlayerModule
{
    public float sensitivityX = 10f;
    public float sensitivityY = 10f;
    public float smoothTime = 20f;
    public float clampLookY = 85f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float accMouseX = 0f;
    private float accMouseY = 0f;
    private Vector2 currentMouseDelta;
    private Transform cameraTransform;

    public bool isLocked;
    public override bool IsLocked { get => isLocked; set => isLocked = value; }

    private bool isInitialized;
    public override bool IsInitialized { get => isInitialized; set => isInitialized = value; }

    public override void InitializeModule(FPCModule fPCModule)
    {        
        IsLocked = false;
        cameraTransform = fPCModule._owner.FindChildWithTag("Camera Holder").transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;        
    }

    public override void OnModuleRemoved(FPCModule fPCModule)
    {

    }

    public override void HandleInput(FPCModule fPCModule)
    {
        currentMouseDelta = InputHandler.Instance.playerActions.Look.ReadValue<Vector2>();
    }

    public override void UpdateModule(FPCModule fPCModule)
    {
        if (Cursor.visible)
            return;

        // Smooth the mouse input using Lerp
        accMouseX = Mathf.Lerp(accMouseX, currentMouseDelta.x, smoothTime * Time.deltaTime);
        accMouseY = Mathf.Lerp(accMouseY, currentMouseDelta.y, smoothTime * Time.deltaTime);

        // Apply sensitivity to smoothed mouse movement
        float mouseX = accMouseX * sensitivityX;
        float mouseY = accMouseY * sensitivityY;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -clampLookY, clampLookY); // Clamp the vertical rotation

        yRotation += mouseX;

        // Apply rotations
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        fPCModule.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }
}