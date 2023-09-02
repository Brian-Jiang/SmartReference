# Smart Reference
#### version 0.9.0

## Summary
Smart Reference is a Unity plugin that allows you to lazy load references to other objects in ScriptableObject and MonoBehaviour.
You may be familiar to use ScriptableObject store data, but when you reference other objects, 
they will be treated as dependencies and will be loaded when the ScriptableObject is loaded. This could be slow if you have a lot of references.
For details, see [this article](https://medium.com/@bjjx1999/3-ways-to-reduce-load-time-in-runtime-for-unity-15d33003eb79).  
Smart Reference allows you to load references only when you need them at runtime with same workflow in editor.

## Quick Start
1. Use SmartReference instead of Object. See how they look exactly same in the inspector.
    ```csharp
        public class MonsterData : ScriptableObject {
            public SmartReference<GameObject> prefab;
            public SmartReference<Sprite> icon;
            public string description;
        }
    ```

2. Initialize smart reference when your game start
    - If you use Unity Addressables, you need to add USE_UNITY_ADDRESSABLES symbol to your player settings, then call
        ```csharp
        SmartReference.Runtime.SmartReference.InitWithAddressablesLoader();
        ```
    - If you use your own custom loader, call 
        ```csharp
        SmartReference.Runtime.SmartReference.Init((path, type) => {
            return MyLoadFunction.Load(path, type);
        }, (path, type, callback) => {
            MyLoadFunction.LoadAsync(path, type, obj => {
                callback(obj);
            });
        });
        ```
        or
        ```csharp
        var loader = new CustomLoader {
            loader = MyLoadFunction,
            loaderAsync = MyAsyncLoadFunction,
        };
        SmartReference.Runtime.SmartReference.Init(loader);
        ```
    - If you use Unity Resources(not recommended, see [why](https://medium.com/@bjjx1999/3-ways-to-reduce-load-time-in-runtime-for-unity-15d33003eb79)), call
        ```csharp
        SmartReference.Runtime.SmartReference.InitWithResourcesLoader();
        ```

## Supports
If you have any questions, please leave an issue at [GitHub](https://github.com/Brian-Jiang/SmartReference/issues).
Thank you for your support!