# Releasing Assets
Assets loaded through Smart Reference should be explicitly released when they are no longer needed.
Releasing allows the active loader to correctly manage memory, reference counts, and underlying resources.

Failing to release assets may lead to increased memory usage and memory leaks.

## Example
```csharp
private void UnloadCurrent()
{
    prefabReference.Release();
}
```
Calling `Release()` notifies the loader that the asset is no longer in use.
The next time you use this asset reference, it will trigger a new load operation.

## When to Release
You should release assets when they are no longer required, such as:
- When unloading or switching scenes
- When destroying an object that owns the reference
- During explicit cleanup or teardown logic

In general, every load should have a corresponding release at the appropriate lifecycle point, except for assets that are intended to remain loaded for the entire lifetime of the game.

## Loader Specific Behavior
The effect of `Release()` depends on the active loader:
- Resources loader: Internally it calls `Resources.UnloadAsset(asset)`.
- Addressables loader: If the handle is valid it will call `Addressables.Release(handle)`.
- Custom loader: Release behavior is fully defined by your implementation

Smart Reference itself does not decide when assets are unloaded, you need to explicitly call `Release()` to trigger the process.