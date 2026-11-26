# Validation performed

## Passed outside Unity

- Parsed all 44 C# files, including Editor scripts: no C# grammar errors.
- Compiled 9 Standard Assets scripts as a separate first-pass assembly, then 33 gameplay/template runtime scripts against it: zero C# errors. This checks the helper dependencies, component APIs used by these scripts, and the assembly boundary.
- Executed 24 assertions against the actual engine-independent SurvivalState source. Covered atomic campfire/torch costs, fuel caps, held/stowed torch consumption, fire/shelter drying, cold damage, recovery, retained progress, and rescue/invalid-time guards.
- Scanned 162 serialized assets. No duplicate local object IDs or asset GUIDs; no unresolved gameplay content GUIDs or local object references; all current Assets entries have metadata.
- Checked 14 explicit main-scene asset fields; prefab root file IDs exist in their referenced prefab files.
- Verified legacy input mode, the first enabled survival scene, and the supplied PC URP asset in Graphics and all six quality levels.
- Original input ZIP CRC checks and reconstruction were performed during assembly. Output ZIP CRC and complete/nonoverlapping file coverage are checked during packaging.

## Exact limits of these results

The compatibility compilation used UnityEngine.Modules **2021.3.33** reference assemblies and an official uGUI 2018.4 source-derived verification reference. Two uGUI implementation files were adapted in the external harness for newer delegate/UV APIs. These references and binaries are **not** shipped inside Assets and do not change the requested project's packages. The legacy controller branch compiled; Unity 6's Rigidbody.linearVelocity branch awaits the actual editor.

The delivered project retains the user's **6000.6.10f1 / URP 17.6.0 / uGUI 2.6.0 / Input System 1.20.1** package configuration. URP configuration and the supplied sample scene contain package-owned references, which were not treated as missing gameplay content and must resolve when Unity restores those packages.

**No Unity editor was available.** Exact-version Unity compilation/import and legacy prefab upgrading, shader compilation, graphics appearance, tool animation playback, physics, interaction reach, UI input, audio mixing, full-route completion, 5–10 minute pacing, and Windows/Mac/Linux builds have not been verified. The local menu validators and manual checklist support that next stage. No human playtest outcomes are claimed.

Source tests: `Verification/SurvivalRulesTests.cs`. Detailed static result: `Verification/validation.json`.
