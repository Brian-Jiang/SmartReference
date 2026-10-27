# Loading Assets Asynchronously

Asynchronous loading allows assets to be loaded without blocking execution, helping maintain smooth frame rates and responsive gameplay.

Smart Reference supports multiple async styles to fit different coding patterns and project needs.

---

## With Event Callback
This approach uses an event-style callback and is suitable for fire-and-forget loading, such as UI elements or effects.

```csharp
using UnityEngine;
using SmartReference.Runtime;

public class CallbackExample : MonoBehaviour
{
    [SerializeField] private SmartReference<GameObject> reference;

    void Start()
    {
        reference.OnAsyncLoadComplete += OnLoaded;
        reference.LoadAsync();
    }

    void OnLoaded(GameObject go)
    {
        Debug.Log("Reference loaded: " + go.name);
        Instantiate(go);
    }
}
```
This style does not require `async/await` and works well when the load result is handled immediately.

---

## With C# Async/Await
This approach integrates with standard C# `async/await` workflows and is suitable when composing multiple async operations using `Task`.

```csharp
using UnityEngine;
using SmartReference.Runtime;
using System.Threading.Tasks;

public class TaskExample : MonoBehaviour
{
    [SerializeField] private SmartReference<GameObject> prefab;

    private async Task<GameObject> LoadPrefabAsync()
    {
        var go = await prefab.LoadAsyncTask();
        return go;
    }
}
```
Avoid calling `.Result` or `.Wait()` on async methods, as this will block the main thread and defeats the purpose of asynchronous loading.

---

## With UniTask
If your project uses UniTask, Smart Reference provides a zero-allocation async API optimized for performance-critical paths.

```csharp
using UnityEngine;
using SmartReference.Runtime;
using Cysharp.Threading.Tasks;

public class UniTaskExample : MonoBehaviour
{
    [SerializeField] private SmartReference<GameObject> reference;

    public async UniTaskVoid Spawn()
    {
        var asset = await reference.LoadAsyncUniTask();
        Instantiate(asset);
    }
}
```
This is the recommended async approach for gameplay code when UniTask is available.

---

## With Awaitable
On Unity 2023.2 or newer (including Unity 6), Smart Reference offers an API built on Unity's own `Awaitable<T>`. It is pooled and low-allocation like UniTask, and needs no extra package.

```csharp
using UnityEngine;
using SmartReference.Runtime;

public class AwaitableExample : MonoBehaviour
{
    [SerializeField] private SmartReference<GameObject> reference;

    public async Awaitable Spawn()
    {
        var asset = await reference.LoadAsyncAwaitable(destroyCancellationToken);
        Instantiate(asset);
    }
}
```

Unity pools `Awaitable` instances, so **an `Awaitable` must only be awaited once**. Await it right away or store the result, not the `Awaitable`. If you need to wait for the asset again, call `LoadAsyncAwaitable()` again. Callers that wait at the same time each get their own `Awaitable` but share one underlying load.

Cancellation applies to each caller separately. Cancelling a token makes only that caller's `await` throw `OperationCanceledException`. The load and the other callers keep going. To stop the load itself, call `Release()`; every pending caller then receives `null`. Cancel tokens from the main thread.

---

## Choosing an Async Style
- Event callback: Simple, lightweight, no `async/await`
- Task-based async: Integrates with standard .NET async flows
- UniTask: Best performance and lowest allocation cost
- Awaitable: Low allocation with no extra dependency (Unity 2023.2+)

All async methods delegate to the active loader. Cancellation and release behavior depend on the loader implementation.