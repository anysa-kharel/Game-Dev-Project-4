# Template integration and repair notes

The uploaded asset parts identify themselves as Mineshaft Survival Complete Project. The original package layout is restored under `Assets/Mineshaft Survival`, with Standard Assets beside it. Original metadata GUIDs are retained wherever possible.

## Compatibility changes

- Supplied first-person helper classes are present: MouseLook, FOVKick, CurveControlledBob, LerpControlledBob, and FirstPersonInput. The incomplete CrossPlatformInput dependency is removed. Movement stays inside Standard Assets; the adventure adapter references it without a circular first-pass assembly dependency.
- Replaced unavailable `GetEntityID` persistence calls with a key derived from scene path, hierarchy sibling indices, component type, and component index. The original demo uses its own PlayerPrefs namespace and reset registry. It no longer clears unrelated preferences. Adventure checkpoints are separate, in-session state; demo location saves cannot move the forest player into mine coordinates.
- Remapped old uGUI DLL/file-ID references to current Text, Image, Button, Slider, CanvasScaler, GraphicRaycaster, EventSystem, and StandaloneInputModule scripts. The old scene has a font fallback bootstrap.
- Removed the unavailable legacy postprocessing component from the original Player prefab and its scene override. URP supplies rendering. Assigned the supplied PC pipeline in Graphics and every quality level.
- Converted built-in/absent material shaders to included URP-compatible Lit, particle, or water shaders. Kept supplied textures when available. Missing optional effect textures/cookies are cleared; missing water geometry uses an engine cylinder mesh and an included water material. Water-drip geometry/material references have included fallbacks.
- Restored the missing seed script reference to the supplied Seed script, and replaced the empty legacy scene component with TemplateDemoBootstrap. Unavailable optional third-person animation references are cleared; this does not replace the original FPS torch/pickaxe animation controller.
- Food initializes its save key before loading and safely toggles full/empty models. Zero saved fuel/food/rock values are respected.
- Fuel is clamped. Empty generators turn off, lights check valid fuel/power, and missing visual references are guarded. A spotlight remains enabled through a temporary generator outage and resumes with power.
- Food/thirst/health are managed once by the adventure rather than two independent update loops. Original demo HUD bindings remain optional.
- Added controller hooks for stamina, crouch, gusts, and safe feet-based teleporting. The original capsule pivot is centered, so teleports include its half-height. Player children ignore interaction raycasts.
- Disabled template coordinate-dependent water-height changes for forest supplies. Idle grabbing joints have zero spring; releasing a prop does not leave a spring attached to the world origin.
- Guarded legacy AI movement when its agent has no NavMesh. Enemies and planting are retained in the source assets but are not part of the forest route.

## New adventure files

`Assets/StormSurvival/Scripts` contains exposure/inventory rules, the template-player adapter, world layout, mining reward integration, UI, weather, audio, and objective progression. `Assets/StormSurvival/Editor` adds scene-opening, preview, and validation commands.

All three uploaded asset parts remain represented in the project. New forest assets are from the user's earlier template upload. The source backup predates these repairs and is kept outside Assets so Unity does not compile duplicate classes.

These are source and reference repairs. Editor import/upgrading, package restoration, shader compilation, and complete runtime verification still need Unity on the user's machine.
