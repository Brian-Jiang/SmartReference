# Initialization
Smart Reference requires a global initialization step to define how assets are resolved and loaded at runtime.

During initialization, you need to register a loader implementation.
Smart Reference will delegate all synchronous loads, asynchronous loads, releases, and cancellations to this loader.

Initialization must happen before any `SmartReference` is used.
---

## Init with Resources Loader
Initializes Smart Reference using Unity’s built-in Resources system.

```csharp
using SmartReference.Runtime;

SmartReference.InitWithResourcesLoader();
```

Use this option only when all referenced assets are located inside a Resources folder.

Note that all Resources assets are treated as a single bundle, meaning the entire Resources folder is loaded into memory at runtime.
Because of the limitation, this loader is best suited for small projects, tools, or prototypes.

---

## Init with Addressables Loader
Initializes Smart Reference using Unity Addressables.

```csharp
using SmartReference.Runtime;

SmartReference.InitWithAddressablesLoader();
```

This option is only available if you have the [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@2.7/manual/index.html) package installed in your project.  
This is the recommended option for most production projects.
It supports asynchronous, non-blocking loading and integrates cleanly with Addressables’ reference counting and memory management.
It also enables advanced workflows such as remote content delivery, DLC, and asset bundles.

---

## Init with Custom Loader
You can supply a custom loader to integrate Smart Reference with any asset source or loading pipeline.

This is useful when you already have an existing asset management system or need specialized loading behavior.

For implementation details, see [Using Custom Loader](custom_loader.md).

---

## Where to Initialize
Initialization must be performed:
- Once per application lifetime
- Before any SmartReference is accessed

If initialization is skipped, calling Load or LoadAsync will result in runtime errors.

### Recommended: Runtime Initialization
```csharp
using UnityEngine;

public static class SmartReferenceBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SmartReference.InitWithAddressablesLoader();
    }
}
```
This ensures Smart Reference is ready before any scene or script attempts to load assets.

### Alternative: Bootstrap MonoBehaviour
```csharp
public class Bootstrap : MonoBehaviour
{
    void Awake()
    {
        SmartReference.InitWithAddressablesLoader();
    }
}
```
This approach is acceptable if you already have a guaranteed first-loaded scene.

## Reinitialization
Reinitialization is not supported because the smart reference handle used internally is different between loaders.
