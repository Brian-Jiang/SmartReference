namespace SmartReference.Runtime
{
    /// <summary>
    /// This is inherited by different handle types used by SmartReference. It stores current state of the reference.
    /// It should be returned by the `LoadAsync` method and passed to the `Release` and `Cancel` methods so the loader knows what assets to operate on.
    /// </summary>
    public interface ISmartReferenceHandle
    {
        
    }
}