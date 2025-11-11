using Unity.Netcode;

public abstract class BaseSlotModule : NetworkBehaviour
{
	protected Slot slot;

	public virtual void Initialize(Slot slot)
	{
		this.slot = slot;
		SubscribeToEvents();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		UnsubscribeFromEvents();
	}

	public abstract void UpdateModule();
	public virtual void SubscribeToEvents() { }
	public virtual void UnsubscribeFromEvents() { }

	public bool OwnsComponent(ulong playerId)
	{
		return OwnerClientId == playerId;
	}

	public ulong GetOccupantId()
	{
		return OwnerClientId;
	}
}
