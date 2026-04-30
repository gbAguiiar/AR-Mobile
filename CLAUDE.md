# AR-Mobile — Claude Project Context

## Project Overview

Unity 6 mobile AR app built on the **Mobile AR Template**. Lets users tap to place interactive 3D objects on real-world surfaces detected by AR. The main custom addition is a Yggdrasil tree model (`3DEC_12.fbx`).

- **Unity version:** 6000.3.11f1
- **Render pipeline:** Universal Render Pipeline (URP) 17.3.0
- **Target platforms:** Android (ARCore), iOS (ARKit)
- **Main scene:** `Assets/Scenes/SampleScene.unity`

## Key Packages

| Package | Version | Purpose |
|---|---|---|
| com.unity.xr.arfoundation | 6.3.3 | AR plane detection, raycasting |
| com.unity.xr.arcore | 6.3.3 | Android AR backend |
| com.unity.xr.arkit | 6.3.3 | iOS AR backend |
| com.unity.xr.interaction.toolkit | 3.3.1 | XR interaction (grab, scale, move) |
| com.unity.render-pipelines.universal | 17.3.0 | URP rendering |
| com.unity.inputsystem | 1.19.0 | Input handling |

## Scene Hierarchy (SampleScene)

```
AR Session                      — manages AR session lifecycle
Directional Light
EventSystem
UI/                             — all UI elements (buttons, menus, coaching prompts)
  Create Button                 — opens object picker
  Delete Button
  Options Button / Modal
  Coaching UI                   — onboarding prompts (scan, tap to place, etc.)
  Object Menu Animator/
    Object Menu                 — spawnable object selector
XR Origin (AR Rig)/
  Camera Offset/
    Object Spawner              — spawns prefabs on AR plane hit
    Main Camera
    Screen Space Ray Interactor — touch → AR raycast → spawn/interact
```

## Object Spawning Flow

1. User taps screen → `ARInteractorSpawnTrigger` fires
2. `ObjectSpawner.TrySpawnObject(spawnPoint, spawnNormal)` instantiates selected prefab
3. Spawner sets `rotation = Quaternion.LookRotation(projectedForward, spawnNormal) * prefabRotation`
   - **Important:** rotation is composed with the prefab's existing rotation, not overwritten.
     This was patched in `ObjectSpawner.cs` to support prefabs that require an axis correction.
4. Object is spawned as child of Object Spawner
5. `XRGrabInteractable` + `ARTransformer` handle subsequent move/scale/rotate

## Custom Assets

### 3DEC_12 (Yggdrasil Tree)

- **FBX:** `Assets/3DEC_12.fbx`
- **Textures:** `Assets/Tex_3DEC09.png`, `Assets/Tex_3DEC12.png`
- **Prefab:** `Assets/3DEC_12.prefab`

**Known FBX quirks:**
- Model is exported Z-up and has its tall axis along +Z in Unity space even after `bakeAxisConversion`
- Prefab root has a **+90° X rotation** to correct this — do not remove it
- `bakeAxisConversion: 1` and `addColliders: 1` are set in `3DEC_12.fbx.meta` — do not revert these

**Prefab components (on root):**
- `ARTransformer` — handles plane-constrained translation and pinch-to-scale
- `Rigidbody` — kinematic, no gravity
- `XRGrabInteractable` — `AttachTransform` and `PredictedVisualsTransform` are both null (fileID: 0); do not set these to internal stripped transforms or you get "Setting the parent of a transform which resides in a Prefab Asset" errors at runtime
- `AxisCorrectionTransformer` (`Assets/Scripts/AxisCorrectionTransformer.cs`) — **must come after ARTransformer in the Inspector**. ARTransformer resets rotation to the plane pose every frame (because the +90° X makes `transform.up` ≠ plane normal, always triggering the reset branch). This transformer re-composes the correct rotation from the grab-start pose + accumulated Y delta.

## Spawnable Prefabs (ObjectSpawner list)

```
0  Assets/3DEC_12.prefab                    ← Yggdrasil tree (custom)
1  Assets/MobileARTemplateAssets/Prefabs/CubeVariant.prefab
2  Assets/MobileARTemplateAssets/Prefabs/PyramidVariant.prefab
3  Assets/MobileARTemplateAssets/Prefabs/TorusVariant.prefab
4  Assets/MobileARTemplateAssets/Prefabs/WedgeVariant.prefab
5  Assets/MobileARTemplateAssets/Prefabs/ArchVariant.prefab
6  Assets/MobileARTemplateAssets/Prefabs/CylinderVariant.prefab
7  Assets/MobileARTemplateAssets/Prefabs/DebugCubeVariant.prefab
```

Template prefabs (1–7) inherit from `Assets/Samples/XR Interaction Toolkit/3.3.0/AR Starter Assets/ARDemoSceneAssets/Prefabs/Cube.prefab` and have identity root rotation with a separate `Visuals` child.

## Adding New AR Prefabs

To add a new 3D model as a spawnable object:

1. Import FBX into `Assets/`. In Model Import Settings:
   - Enable **Bake Axis Conversion** if the model is from a Z-up app (Blender, Maya)
   - Enable **Add Colliders** so XRGrabInteractable can detect touch
2. Create a prefab variant. Add to the root:
   - `ARTransformer` (from XR Interaction Toolkit)
   - `Rigidbody` — set kinematic, disable gravity
   - `XRGrabInteractable` — leave `AttachTransform` = None
3. If the model is still tilted at runtime, add the rotation correction on the prefab root.
   The `ObjectSpawner` composes spawn rotation with the prefab's rotation, so corrections survive spawning.
4. Add the new prefab to `Object Spawner > Object Prefabs` list in the scene.

## Scripts

| File | Purpose |
|---|---|
| `Assets/Samples/XR Interaction Toolkit/3.3.0/Starter Assets/Scripts/ObjectSpawner.cs` | Spawns prefabs on AR surface hit. **Patched** to compose spawn rotation with prefab rotation. |
| `Assets/MobileARTemplateAssets/Scripts/ARTemplateMenuManager.cs` | Manages UI state, object selection, delete/options menus |
| `Assets/MobileARTemplateAssets/Scripts/ARPlaneMeshVisualizerFader.cs` | Fades plane visualizer mesh |
| `Assets/MobileARTemplateAssets/Scripts/GoalManager.cs` | Onboarding coaching prompt progression |
| `Assets/MobileARTemplateAssets/UI/Scripts/CutoutMaskUI.cs` | UI cutout mask rendering |

## Common Pitfalls

- **"Setting the parent of a transform which resides in a Prefab Asset"** — XRGrabInteractable's `AttachTransform` or `PredictedVisualsTransform` is pointing to a stripped transform (prefab-internal reference). Set both to `{fileID: 0}` (None).
- **Model spawns flat on ground** — prefab root rotation is being overwritten. Verify `ObjectSpawner.cs` composes rotations (`spawnRotation * prefabRotation`) rather than assigning directly.
- **Model appears sideways in editor** — FBX axis mismatch. Enable `bakeAxisConversion` in import settings and/or add a rotation correction on the prefab root.
- **Grab/scale not working** — no collider on the object. Enable `addColliders` in FBX import settings or add a MeshCollider manually.
- **Rotation resets to 0 when moved/scaled** — ARTransformer always resets rotation to the AR plane pose when `transform.up` ≠ plane normal (triggered by any non-identity X/Z rotation on the root). Fixed by `AxisCorrectionTransformer` on the prefab root, which must be listed **after** ARTransformer so it runs second in the transformer pipeline.
