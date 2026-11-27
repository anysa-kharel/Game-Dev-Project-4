# The Big Storm

A single-player first-person forest survival demo built around the supplied **Mineshaft Survival Complete Project** player and usable prefabs. Designed for a 5–10 minute first playthrough; that duration still needs confirmation with human playtests.

## Open the project

1. Open the `Survival` folder in Unity Hub using **Unity 6000.6.10f1**. The project folder contains `Assets`, `Packages`, and `ProjectSettings`.
2. Open `Assets/StormSurvival/Scenes/TheBigStorm.unity`. The forest appears automatically in Scene view. `StormWorld.cs` defines the generated layout and is where to make lasting layout changes.
3. Enter Play mode, then press Enter to start. Use the checks in `Documentation/PLAYTEST.md` to test the game.

The supplied project had old built-in rendering settings alongside newer URP assets. This version assigns the included PC URP pipeline in Graphics and all quality levels, and updates the gameplay materials. The project uses **Active Input Handling = Both** so the template's legacy controls and the Unity Input System are available.

## Goal

Gather wood and mine stone. Prepare a campfire and torch, clear the river crossing, and choose a route over the ridge. Start the lookout generator and aim the spotlight at the clearing marked by three amber lamps. Once the clearing signals back, light the beacon with **6 wood, a burning torch, and generator power**.

| Control | Action |
|---|---|
| WASD / mouse | Move / look |
| Shift | Sprint; consumes stamina |
| Space | Jump; costs stamina |
| C or Ctrl | Crouch; also reduces gust push |
| 1 / 2 / 3 | Torch / fuel cup / pickaxe |
| Left click | Swing the held tool; pickaxe mines rocks and timber |
| Hold E | Campfire, supply crate, beacon |
| E | Eat, drink, collect supplies or barrel fuel, toggle equipment, turn the rescue spotlight |
| R with fuel cup | Refill generator or lantern |
| Right mouse | Hold a movable prop or grab the optional supply sled. Step toward the trail to pull the sled free. |
| Tab | Craft or rewrap torch; pauses play |
| Esc | Pause / resume |

## Survival and progression

- Campfire: **3 wood + 2 stone**. Burns for 150 seconds; 1 wood adds 90 seconds when fuel is low. A burning fire quickly warms and dries you. Resting there sets an in-session checkpoint.
- Torch: **2 wood + 1 cloth**. You start with one cloth. Craft with Tab, then hold E at a burning fire to light it. Rewrapping costs 1 wood. A held burning torch slows heat loss; stowing it conserves fuel.
- Timber takes three pickaxe swings and gives four wood; it renews after 65 seconds. Amber-marked rocks give four stone. Mining the marked river rubble releases the bridge.
- Food and water are the actual template supplies. Their hunger/thirst effects are shown in the HUD. Use the template fuel cup to take oil from barrels and refill the generator.
- Shelter reduces exposure; fires restore warmth faster. Water immersion costs health and warmth and returns you to shore.
- The ridge splits into an exposed shortcut and a longer covered path with a food and water stop. Rising wind audio and swaying trees warn that a gust is coming. Crouching reduces sideways push.
- The optional logging-camp sled gives two wood and up to 60 seconds of torch fuel. If there is no room for a wrap, it gives one cloth instead.
- Failure returns you to your last campfire with recovery supplies. Inventory and opened routes remain. Checkpoints last for the current session; restarting the adventure starts fresh.

## What's reused

The actual template Player prefab supplies the controller, camera, animated torch, pickaxe, fuel cup, and footstep/jump audio. The world uses its food, water, rocks, fuel barrels, generator, spotlight, lantern, and mine shelter. Forest trees/textures come from the earlier user-supplied Polyeler Essential Nature Pack. New systems add storm exposure, route progression, camps, bridge, UI, procedural ambience, and rescue.

The forest is the only build scene. The local template archive at `Reference/OriginalTemplateCodeAndScenes.zip` preserves the original code, scenes, prefabs, and materials for comparison.

## Validation status

See `Verification/VALIDATION.md` for the original project validation and `Documentation/PLAYTEST.md` for the runtime checks.

## Build and submit

In Unity Build Profiles, select the required Windows x86-64 target and build to a separate folder. Install the matching Mac and Linux modules and build those as well for the assignment. Do not place builds inside `Assets`.

The assignment also requires actual playtesting revisions, GitHub history, a High Concept and Game Script, Box releases, and instructor repository access. Draft design notes and a playtest/revision form are included. Record real feedback and measured completion times before treating the demo as submission-ready.
