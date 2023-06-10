# Smart Reference
#### version 0.7.0

## Summary
Smart Reference is a Unity plugin that allows you to lazy load references to other objects in ScriptableObject and MonoBehaviour.
We use ScriptableObject to store data, but when you reference other objects, they will be treated as dependencies and will be loaded when the ScriptableObject is loaded.
This could be slow if you have a lot of references. Smart Reference allows you to load references only when you need them at runtime with same workflow in editor.

## Quick Start
1. Use SmartReference instead of Object. See how they look exactly same in the inspector.
```csharp
    public class MonsterData : ScriptableObject {
        public SmartReference<GameObject> prefab;
        public SmartReference<Sprite> icon;
        public string description;
    }
```

2. In your start up script, call SmartReference.Init() to initialize the SmartReference system with your load and async load functions.
```csharp
    SmartReference.Runtime.SmartReference.Init((path, type) => {
        return MyLoadFunction.Load(path, type);
    }, (path, type, callback) => {
        MyLoadFunction.LoadAsync(path, type, obj => {
            callback(obj);
        });
    });
```

## Supports
If you have any questions, please comment at [Asset Store](https://u3d.as/35Sh)  
Or email me directly at: [bjjx1999@live.com](mailto:bjjx1999@live.com)  
Thank you for your support!