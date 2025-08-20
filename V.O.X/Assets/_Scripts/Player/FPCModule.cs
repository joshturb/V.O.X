using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FPCModule : NetworkBehaviour
{
	public CharacterController characterController;
    internal Referencer _owner; 

    [Tooltip("List of player modules attached to this character. | Movement should exist first, as some modules such as: 'Jump' rely on the movement the be updated first!")]
    public Vector3 clientMovement;
    public List<PlayerModule> modules = new();
    public event Action<FPCModule, PlayerModule> OnModuleAdded;
    public event Action<FPCModule, PlayerModule> OnModuleRemoved;
    public float staminaReductionRate;
    public float staminaIncreaseRate = 5;
    public float staminaIncreaseDelay = 5f;
    public float currentStamina = 100f;
    public bool isGrounded;

	public GameObject thirdPersonObject;
	public GameObject firstPersonObject;
    private float _timeSinceLastStaminaChange = 0f;

    public void Start()
    {
        _owner = GetComponent<Referencer>();

        if (!IsOwner)
        {
			if (thirdPersonObject != null)
			{
				firstPersonObject.SetActive(false);
			}
			return;
        }

		NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoadComplete;
		BaseMinigameManager.OnMinigameInitialized += OnMinigameInitialized;
		_owner.GetCachedComponent<CinemachineCamera>().Priority = 0;
		if (thirdPersonObject != null)
		{
			thirdPersonObject.SetActive(false);
		}

        foreach (PlayerModule module in modules)
        {
            module.IsInitialized = true;
            module.InitializeModule(this); 
        }   
    }

	public override void OnDestroy()
	{
		base.OnDestroy();
		NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
		BaseMinigameManager.OnMinigameInitialized -= OnMinigameInitialized;
	}

	private void OnSceneLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
	{
		if (loadSceneMode != LoadSceneMode.Additive)
			return;

		if (sceneName != GameManager.Instance.theBlankSceneName)
			return;

		for (int i = modules.Count - 1; i >= 0; i--)
		{
			RemoveModule(modules[i]);
		}

		foreach (var module in TheBlankManager.Instance.requiredModules)
		{
			AddModule(module);
		}
	}

	private void OnMinigameInitialized(BaseMinigameManager manager)
	{
		for (int i = modules.Count - 1; i >= 0; i--)
		{
			RemoveModule(modules[i]);
		}

		foreach (var item in manager.requiredModules)
		{
			AddModule(item);
		}
	}

	public void Update()
	{
		if (!IsOwner)
			return;

		for (int i = 0; i < modules.Count; i++)
		{
			if (modules[i].IsLocked)
				continue;

			modules[i].HandleInput(this);
			modules[i].UpdateModule(this);
		}
		HandleStamina();

		characterController.Move(clientMovement * Time.deltaTime);
	}

    private void HandleStamina()
    {
        if (staminaReductionRate > 0)
        {
            currentStamina -= staminaReductionRate * Time.deltaTime;
            _timeSinceLastStaminaChange = 0f;
        }
        else
        {
            _timeSinceLastStaminaChange += Time.deltaTime;
            if (_timeSinceLastStaminaChange >= staminaIncreaseDelay)
            {
                currentStamina += staminaIncreaseRate * Time.deltaTime;
            }
        }
        
        currentStamina = Mathf.Clamp(currentStamina, 0, 100);
    }

    public void SetStamina(float stamina)
    {
        currentStamina = stamina;
    }

    public void AddModule(PlayerModule module)
    {
        if (!modules.Contains(module))
        {
            modules.Add(module);
            module.InitializeModule(this);
            OnModuleAdded?.Invoke(this, module);
        }
    }

    public void RemoveModule(PlayerModule module)
    {
        if (modules.Contains(module))
        {
            module.OnModuleRemoved(this);
            OnModuleRemoved?.Invoke(this, module);
            modules.Remove(module);
        }
    }

    public bool ContainsModule<T>() where T : PlayerModule
    {
        foreach (var module in modules)
        {
            if (module is T)
            {
                return true;
            }
        }
        return false;
    }
    public bool TryGetModule<T>(out T moduleOut) where T : PlayerModule
    {
        foreach (var module in modules)
        {
            if (module is T typedModule)
            {
                moduleOut = typedModule;
                return true;
            }
        }
        moduleOut = null;
        return false;
    }
}
