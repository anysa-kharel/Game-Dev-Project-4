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
- Jump the training branch and crouch through the stone crawlspace. Check head clearance when releasing crouch inside. Approach the optional sled, hold right mouse and step toward the trail, then collect the bundle with E. Verify the bundle pays out once.
- Mine river rubble: gain stone, see bridge appear. Jump bridge branch. Fall intentionally: shore recovery, wetness and health cost.
- Rest at far-bank camp and retry after freezing. Checkpoint keeps materials and bridge; needs/recovery supplies restore.
- Try both ridge routes. Verify the gust warning, sideways push, crouch reduction, and roofed shelter protection. Note whether first-time players see the fork and understand the tradeoff.
- Refill the final generator with 2 + R and start it with E. Turn the powered spotlight until the clearing marked with three amber lamps signals back. Verify that the beam turns and the flash and sound confirm alignment.
- Beacon rejects insufficient wood, an unlit torch, missing power, or incomplete alignment without consuming wood. With all conditions met, it consumes six wood and reaches the win screen.
- Pause, craft, focus loss, retry and restart work; timers stop during pause. Release a grabbed prop on pause.
- Validate Running Level succeeds early in a run. Finish a Windows, Mac, and Linux build and test each required release on its target platform.

- At the lookout, mine both marked rocks and deposit stone at the counterweight scale. Check progress after four stones and confirm the passage lifts at six. Retry before and after opening to check that deposits and access persist.

## Replay variation

- Start two adventures. Check which sled, supply stash, spare fuel, and wolf locations changed. Verify that the early supplies and lookout fuel remain accessible.
- Retry from a checkpoint and confirm that optional supply locations and the weather pattern stay consistent. Look for the gust warning before the next push.
- Follow the trail past the first wolf on the logging route, then look for separate wolves beyond the far-bank camp and on the upper ridge. Check that getting past one wolf leaves the later encounters available. Test the lunge warning, lethal bite, torch, and pickaxe. Confirm that retry returns all three wolves to their patrols.

## Human playtest record (copy for each participant/build)

- Tester/date/build commit:
- Prior first-person game experience:
- Completion time from Begin Survival to rescue, including crafting/retries:
- Ridge route chosen and time spent at camp, ridge, and lookout:
- Choices that changed between runs and what prompted them:
- Which objective or control was unclear:
- Observed searches/backtracking:
- Warmth, thirst, movement and crossing difficulty:
- What felt fun / frustrating:
- Specific change made and why:
- Commit containing change:
- Follow-up result:

Target: first completion 5–10 minutes. Tune route length, interaction time, survival rates, and resource visibility from observed results. A fast experienced run may be shorter; do not add a forced wait merely to hit the duration.
