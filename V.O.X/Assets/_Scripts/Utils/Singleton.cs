using Unity.Netcode;
using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
	public static T Instance { get; private set; }
	protected virtual void Awake()
	{
		if (Instance == null)
		{
			Instance = this as T;
		}
		else
		{
			Destroy(gameObject);
		}
	}
}

public abstract class NetworkSingleton<T> : NetworkBehaviour where T : NetworkBehaviour
{
	public static T Instance { get; private set; }
	protected virtual void Awake()
	{
		if (Instance == null)
		{
			Instance = this as T;
		}
		else
		{
			Destroy(gameObject);
		}
	}
}

public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}
}

public abstract class PersistentNetworkSingleton<T> : NetworkSingleton<T> where T : NetworkBehaviour
{
	protected override void Awake()
	{
		base.Awake();
		DontDestroyOnLoad(gameObject);
	}
}
