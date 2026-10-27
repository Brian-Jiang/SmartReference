# Changelog

## 2.1.0
- Add `LoadAsyncAwaitable` for Unity 2023.2+, with per-caller cancellation
- Inspector drawers now use UI Toolkit (IMGUI fallback), with prefab override and multi-object editing support, derived type assignment, and a missing asset warning
- Update All References now covers nested fields, lists, arrays, private base class fields, and prefab and scene components
- Add SmartReference Packed Mode Addressables build script that updates references before content builds
- Releasing during an async load now cancels it, and pending callers receive `null`
- Fix loading after a release during an async load, early `OnAsyncLoadComplete` handlers being dropped, and wrong-type loader results throwing
- Fix list and array drawers reloading on every repaint, Resources release errors for prefabs, the SceneReference Fix button, editor-only sample scripts, and the synchronous loading docs sample

## 2.0.0
- Add asset release API
- Add async loading cancellation support
- Support UniTask for async loading
- Add editor tool for setting references through editor scripts
- Update documentation

## 1.0.0
- Improve scene reference, add auto fix button
- Use version defines
- Update README

## 0.9.0
- Auto update path or guid when the referenced asset is moved or renamed
- Add prebuild script to update all references in the project
- Add simple demo

## 0.8.0
- Add scene reference field
- Simplify initialization

## 0.7.0
- First release
