using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System;

public class Referencer : NetworkBehaviour, IEquatable<Referencer>
{
	public static HashSet<Referencer> AllReferencers { get; private set; } = new HashSet<Referencer>();
	private static readonly Dictionary<ulong, Referencer> ReferencersByIds = new();
	private readonly Dictionary<Type, Component> _cachedComponents = new();

	private void Awake()
	{
		CacheComponents();
	}

	public override void OnNetworkSpawn()
	{
		base.OnNetworkSpawn();
		AllReferencers.Add(this);
		ReferencersByIds.Add(OwnerClientId, this);
	}
	
	public override void OnNetworkDespawn()
	{
		RemoveReferencerRpc(OwnerClientId);
		base.OnNetworkDespawn();
	}

	[Rpc(SendTo.Everyone)]
	private void RemoveReferencerRpc(ulong id)
	{
		if (ReferencersByIds.TryGetValue(id, out var referencer))
		{
			AllReferencers.Remove(referencer);
			ReferencersByIds.Remove(id);
		}
	}

	private void CacheComponents()
	{
		Component[] components = GetComponents<Component>();
		foreach (var component in components)
		{
			_cachedComponents[component.GetType()] = component;
		}
	}

	// Use this for any non neccessary components.
    public bool TryGetCachedComponent<T>(out T component) where T : Component
    {
        if (_cachedComponents.TryGetValue(typeof(T), out var cachedComponent) && cachedComponent is T typedComponent)
        {
            component = typedComponent;
            return true;
        }

        component = null;
        return false;
    }

	// Use when you know for sure a component is on the object
	public T GetCachedComponent<T>() where T : Component
	{
		// Check the cached dictionary first
		if (_cachedComponents.TryGetValue(typeof(T), out var cachedComponent) && cachedComponent is T typedComponent)
		{
			return typedComponent;
		}

		// If not found in cache, look in children
		T foundComponent = GetComponentInChildren<T>(true);
		if (foundComponent != null)
		{
			// Add to cache for future use
			_cachedComponents[typeof(T)] = foundComponent;
			return foundComponent;
		}

		throw new InvalidOperationException($"Component of type {typeof(T).Name} not found on GameObject '{name}' or its children.");
	}

	public GameObject FindChildWithTag(string tag)
	{
		Transform[] childTransforms = GetComponentsInChildren<Transform>();

		foreach (var child in childTransforms)
		{
			if (child.CompareTag(tag))
			{
				return child.gameObject;
			}
		}

		return null; // Return null if no GameObject with the tag is found
	}

	public GameObject FindChildWithName(string name)
	{
		Transform[] childTransforms = GetComponentsInChildren<Transform>();

		foreach (var child in childTransforms)
		{
			if (child.name == name)
			{
				return child.gameObject;
			}
		}

		return null; // Return null if no GameObject with the tag is found
	}

	public static bool TryGetReferencer(ulong playerId, out Referencer referencer)
	{
		if (ReferencersByIds.TryGetValue(playerId, out referencer))
			return true;

		foreach (Referencer reference in AllReferencers)
		{
			if (reference.OwnerClientId != playerId)
				continue;

			ReferencersByIds[playerId] = reference;
			referencer = reference;
			return true;
		}
		
		referencer = null;
		return false;
	}

	#region IEquatable
	public bool Equals(Referencer other)
	{
		return this == other;
	}

	public override bool Equals(object obj)
	{
		return obj is Referencer other && this == other;
	}

	public override int GetHashCode()
	{
		return gameObject.GetHashCode();
	}

	public static bool operator ==(Referencer left, Referencer right)
	{
		return (MonoBehaviour) left == right;
	}

	public static bool operator !=(Referencer left, Referencer right)
	{
		return (MonoBehaviour) left != right;
	}
	#endregion
}
