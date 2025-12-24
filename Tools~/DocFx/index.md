# Smart Reference
#### version 2.0.0

[![DocFX Build and Publish](https://github.com/Brian-Jiang/SmartReference/actions/workflows/docfx-build-publish.yml/badge.svg?branch=main)](https://github.com/Brian-Jiang/SmartReference/actions/workflows/docfx-build-publish.yml)
[![GitHub release (latest by date)](https://img.shields.io/github/v/release/Brian-Jiang/SmartReference?label=github&style=flat-square)](https://github.com/Brian-Jiang/SmartReference/releases)
[![openupm](https://img.shields.io/npm/v/com.xiaojiang.smartreference?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.xiaojiang.smartreference/)
[![Unity Asset Store](https://img.shields.io/badge/Unity%20Asset%20Store-Smart%20Reference-blue.svg)](https://u3d.as/35Sh)

---

## Summary
Smart Reference is a Unity plugin that enables lazy loading asset references in `ScriptableObject` and `MonoBehaviour`.
In Unity, referencing assets directly creates implicit dependencies.
When the `MonoBehaviour` or `ScriptableObject` is loaded, all referenced assets are loaded immediately, which can lead to slow startup times and unnecessary memory usage, especially in data driven projects.

Smart Reference solves this by allowing you to reference assets without loading them upfront, while keeping the same editor workflow.
Assets are only loaded when you use them at runtime.

## Quick Start
### 1. Replace direct Unity references with `SmartReference<T>`  
In the Inspector, SmartReference<T> looks and behaves like a normal object reference.
```csharp
using UnityEngine;
using SmartReference.Runtime;

public class MonsterData : ScriptableObject
{
    public SmartReference<GameObject> prefab;
    public SmartReference<Sprite> icon;
    public string description;
}

```

### 2. Initialize Smart Reference at startup  
Choose the loader that matches your project setup.
#### Addressables (recommended)
```csharp
using SmartReference.Runtime;

SmartReference.InitWithAddressablesLoader();
```

#### Resources
```csharp
using SmartReference.Runtime;

SmartReference.InitWithResourcesLoader();
```

#### Custom Loader
```csharp
SmartReference.InitWithCustomLoader(new MyCustomLoader());
```

Initialization must happen once, before any `SmartReference` is accessed.

### 3. Load assets when needed
#### Asynchronous Loading with UniTask (recommended)
```csharp
var prefab = await monsterData.prefab.LoadAsyncUniTask();
Instantiate(prefab);
```

#### Asynchronous Loading with C# Async/Await
```csharp
var prefab = await monsterData.prefab.LoadAsyncTask();
Instantiate(prefab);
```

#### Asynchronous Loading with Callbacks
```csharp
monsterData.prefab.OnAsyncLoadComplete += go =>
{
    Instantiate(go);
};
monsterData.prefab.LoadAsync();
```

#### Synchronous Loading
```csharp
var prefab = monsterData.prefab.Load();
Instantiate(prefab);
```

## Supports
If you have questions or feedback:
- Leave an issue at [GitHub Issues](https://github.com/Brian-Jiang/SmartReference/issues)
- Leave a comment at [Asset Store](https://u3d.as/35Sh)
- Email: [bjjx1999@live.com](mailto:bjjx1999@live.com)

Thank you for your support!