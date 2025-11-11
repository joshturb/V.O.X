using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(FPCModule))]
public class ModuleLocker : NetworkBehaviour
{
    private FPCModule fpcModule;

    void Awake()
    {
        fpcModule = GetComponent<FPCModule>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        UnlockAllModulesRpc();
    }

    public void LockModule<T>() where T : PlayerModule
    {
        if (fpcModule.TryGetModule<T>(out var module))
        {
            module.IsLocked = true;
        }
    }

    public void UnlockModule<T>() where T : PlayerModule
    {
        if (fpcModule.TryGetModule<T>(out var module))
        {
            module.IsLocked = false;
        }
    }

    [Rpc(SendTo.Owner)]
    public void LockMovementModuleRpc()
    {
        LockModule<MovementModule>();
    }

    [Rpc(SendTo.Owner)]
    public void LockCameraModuleRpc()
    {
        LockModule<CameraModule>();
    }

    [Rpc(SendTo.Owner)]
    public void UnlockMovementModuleRpc()
    {
        UnlockModule<MovementModule>();
    }

    [Rpc(SendTo.Owner)]
    public void UnlockCameraModuleRpc()
    {
        UnlockModule<CameraModule>();
    }

    [Rpc(SendTo.Owner)]
    public void LockAllModulesRpc(bool includeCamera = false)
    {
        foreach (var module in fpcModule.modules)
        {
            if (includeCamera || module is not CameraModule)
                module.IsLocked = true;
        }
    }

    [Rpc(SendTo.Owner)]
    public void UnlockAllModulesRpc()
    {
        foreach (var module in fpcModule.modules)
        {
            module.IsLocked = false;
        }
    }
}
