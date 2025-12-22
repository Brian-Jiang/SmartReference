# Initialization
Smart Reference requires a global initialization step to define how assets are loaded.

Initialization should be performed:
- Once per application
- As early as possible (before any asset access)

---

## Init with Resources Loader

```csharp
using SmartReference.Runtime;

SmartReference.InitWithResourcesLoader();
```

---

## Init with Addressables Loader

```csharp
using SmartReference.Runtime;

SmartReference.InitWithAddressablesLoader();
```

---

## Init with Custom Loader

```csharp
using SmartReference.Runtime;

public class MyLoader : ISmartReferenceLoader
{
    public Object Load(string path, Type type)
    {
        // Your custom synchronous load logic here
    }

    public ISmartReferenceHandle LoadAsync(string path, Type type, Action<Object> callback)
    {
        // Your custom asynchronous load logic here
    }

    public void Release(ISmartReferenceHandle handle, Object asset)
    {
        // Your custom release logic here
    }

    public void Cancel(ISmartReferenceHandle handle)
    {
        // Your custom cancel logic here
    }
}

SmartReference.InitWithCustomLoader(new MyLoader());
```

---

## Where to Initialize