using Unity.Netcode;
using UnityEngine;

public class ModuleLocker : MonoBehaviour
{
	public static ModuleLocker Instance;
	private FPCModule fpcModule;

	void Awake()
	{
		Instance = this;
		fpcModule = GetComponent<FPCModule>();
	}

	private void OnDestroy()
	{
		UnlockAllModulesRpc();
	}

	[Rpc(SendTo.Owner)]
	public void LockModuleRpc<T>() where T : PlayerModule
	{
		if (fpcModule.TryGetModule<T>(out var module))
		{
			module.IsLocked = true;
		}
	}

	[Rpc(SendTo.Owner)]
	public void UnlockModuleRpc<T>() where T : PlayerModule
	{
		if (fpcModule.TryGetModule<T>(out var module))
		{
			module.IsLocked = false;
		}
	}

	[Rpc(SendTo.Owner)]
	public void LockAllModulesRpc()
	{
		foreach (var module in fpcModule.modules)
		{
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
