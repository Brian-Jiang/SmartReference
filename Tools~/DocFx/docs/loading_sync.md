# Loading Assets Synchronously
Synchronous loading blocks execution until the asset is available.

---

## Basic Sync Load

```csharp
SmartReference<Texture2D> icon;

var texture = icon.Load();
```