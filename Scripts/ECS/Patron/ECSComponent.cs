public abstract class ECSComponent
{
	private uint entityOwnerID = 0;

	public uint EntityOwnerID { get => entityOwnerID; set => entityOwnerID = value; }

	protected ECSComponent() { }

	public virtual void Dispose() { }
}
