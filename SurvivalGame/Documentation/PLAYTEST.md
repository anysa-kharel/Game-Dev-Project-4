# Unity verification and playtest form

Editor runtime status: **not run in this environment**. Record results locally.

## First import

- Both ZIPs were merged; Objects, UsablePrefabs, Scripts, and the StormSurvival scene exist.
- Unity 6000.6.10f1 restores packages and completes prefab upgrades without Console errors.
- Validate Project succeeds; the PC URP asset is assigned in Graphics/Quality. No magenta gameplay materials.
- Open survival scene and Play. One camera/audio listener; visible hands/pickaxe; title buttons work.

## Complete route

- Begin with Enter or button. Mouse look, sprint stamina, jump stamina, crouch and stand-up clearance work.
- 3 equips the original pickaxe and plays PunchPick; three hits collect wood/stone. Interaction ray does not hit the player's own hands.
- Insufficient materials consume nothing. With 3 wood + 2 stone, Hold E builds fire. Warmth rises and wetness falls.
- Tab pauses. Craft torch for 2 wood + 1 cloth, return, light it at fire, select 1. Torch flames/light match fuel. Select 3 and confirm stowed torch conserves fuel.
- Eat and drink with E. Meter effects occur once; water remains beside its supply station.
- Select 2; E at barrel fills cup; R at lantern consumes one cup unit and refills it.
- Jump the training branch, crouch under trunk. Check no head clipping when attempting to stand under a trunk.
- Mine river rubble: gain stone, see bridge appear. Jump bridge branch. Fall intentionally: shore recovery, wetness and health cost.
- Rest at far-bank camp and retry after freezing. Checkpoint keeps materials and bridge; needs/recovery supplies restore.
- Cross ridge: gust cue, sideways push, crouch reduction, roofed shelter protection.
- Refill final generator with 2 + R, E starts it; linked spotlight responds. Empty/stopped generator prevents rescue.
- Beacon rejects insufficient wood/unlit torch/no power without consuming wood. With all conditions met, consumes six wood and reaches win screen.
- Pause, craft, focus loss, retry and restart work; timers stop during pause. Release a grabbed prop on pause.
- Validate Running Level succeeds early in a run. Finish a Windows, Mac, and Linux build and test each required release on its target platform.

## Human playtest record (copy for each participant/build)

- Tester/date/build commit:
- Prior first-person game experience:
- Completion time/retries:
- Which objective or control was unclear:
- Observed searches/backtracking:
- Warmth, thirst, movement and crossing difficulty:
- What felt fun / frustrating:
- Specific change made and why:
- Commit containing change:
- Follow-up result:

Target: first completion 5–10 minutes. Tune route length, interaction time, survival rates, and resource visibility from observed results. A fast experienced run may be shorter; do not add a forced wait merely to hit the duration.
