# Using Custom Loader

## Write a Custom Loader
You can provide a custom loader to integrate Smart Reference with any asset source or loading pipeline.

### Define the loader
```csharp
using SmartReference.Runtime;

public class MyHandler : ISmartReferenceHandle
{
    // Implement the ISmartReferenceHandle interface members here
}

public class MyLoader : ISmartReferenceLoader
{
    public ISmartReferenceHandle Load(string path, Type type, out Object loadedObject)
    {
        // Your custom synchronous load logic here
    }

    public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback)
    {
        // Your custom asynchronous load logic here
    }

    public void Release(ISmartReferenceHandle handle)
    {
        // Your custom release logic here
    }

    public void Cancel(ISmartReferenceHandle handle)
    {
        // Your custom cancel logic here
    }
}
```

### Register the Loader
```csharp
using SmartReference.Runtime;

SmartReference.InitWithCustomLoader(new MyLoader());
```

There are 4 functions to implement:
- `Load`: Synchronously loads an asset from the given path.
- `LoadAsync`: Asynchronously loads an asset from the given path and invokes the callback when done.
- `Release`: Releases the loaded asset.
- `Cancel`: Cancels an ongoing asynchronous load operation.

You also need to define a class that implements `ISmartReferenceHandle` to store your custom loading state.  
The handle is only used to storing your data and passing to `Release` and `Cancel` functions that you implement.