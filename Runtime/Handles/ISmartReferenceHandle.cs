namespace SmartReference.Runtime
{
    /// <summary>
    /// This is inherited by different handle types used by SmartReference. It stores current state of the reference.
    /// It should be returned by `LoadAsync` method and passed to `Release` and `Cancel` methods of so the loader knows what assets to operate on.
    /// </summary>
    public interface ISmartReferenceHandle
    {
        
    }
}