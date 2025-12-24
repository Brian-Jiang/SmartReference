# Loading Assets Synchronously
Synchronous loading blocks execution until the referenced asset is fully loaded and available.  

This is the simplest loading method, but it should be used carefully, especially at runtime.

---

## Basic Sync Load

```csharp
using UnityEngine;
using SmartReference.Runtime;

public class SyncLoadExample : MonoBehaviour
{
    [SerializeField] private SmartReference<Texture2D> icon;
    [SerializeField] private SmartReference<GameObject> prefab;

    void Start()
    {
        var texture = icon.Load();

        var instance = Instantiate(prefab);
    }
}
```

When Load() is called, Smart Reference delegates the request to the active loader and blocks until the asset is available.

## When to Use Synchronous Loading
Synchronous loading is appropriate when loading cost is negligible or when blocking is acceptable. Typical use cases include early initialization code, and small assets such as icons or configuration data.

For larger assets or frequently executed code paths, asynchronous loading is strongly recommended to avoid frame stalls and maintain responsiveness.

## Loader Behavior
The exact behavior of synchronous loading depends on the active loader:
- The Resources loader performs a direct `Resources.Load`.
- The Addressables loader calls `handle.WaitForCompletion()` internally to block while waiting for an async operation.
- Custom loaders define their own synchronous behavior.

## WebGL Considerations
WebGL runs on a single-threaded execution model.
When using the Addressables loader, synchronous loading is not supported on WebGL platforms and will return an empty load result.

For WebGL builds, always use asynchronous loading.
