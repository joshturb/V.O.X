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

	public virtual void SubscribeToEvents() { }
	public virtual void UnsubscribeFromEvents() { }

	public bool OwnsComponent(ulong playerId)
	{
		if (slot.OccupantId.Value == 99)
			return false;

		return slot.OccupantId.Value == playerId;
	}

	public ulong GetOccupantId()
	{
		return slot.OccupantId.Value;
	}
}
