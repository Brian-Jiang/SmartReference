# Loading Assets Asynchronously

Smart Reference supports multiple async styles to fit different workflows.

---

## With Event Callback

```csharp
reference.LoadAsync();
```

---

## With C# Async/Await

```csharp
var asset = await reference.LoadAsyncTask();
```

---

## With UniTask

```csharp
using Cysharp.Threading.Tasks;

var asset = await reference.LoadAsyncUniTask();
```