# The Big Storm — design draft

## High concept

A lost traveler must prepare fire, water, food, and tools while a massive storm closes over an abandoned forest logging route. Gather materials, adapt familiar survival actions to new obstacles, and power a lookout beacon to call for rescue. The theme **Big** appears in the name and in the storm that shapes the entire level.

Single-player first-person survival/adventure. Intended audience: players comfortable with basic WASD movement, with contextual prompts and a short tutorial route supporting newcomers. The target first run is 5–10 minutes. This is a design target, not a measured playtest result. No combat is required; cold, water, gusts, and resource decisions provide opposition.

## Game script

1. **Trailhead / learn preparation.** Start with an animated pickaxe, an empty fuel cup, and cloth. Amber wraps mark timber and rock. Mining grants stone; timber grants wood. A nearby fire ring teaches material costs, warmth, drying, and checkpoints. Craft a torch and light it at the fire. Eat and drink from the template supplies.
2. **Logging route / practice movement.** A low branch introduces jumping and a stone crawlspace teaches crouching. Learn to fill the fuel cup and refill the lantern at the logging camp. A wolf crosses the main trail at one of three spots, then roams the surrounding woods. Standing players who come into view can be chased. A growl signals its lunge, and one bite ends the run. Crouch at a distance or use the torch to drive it away. Look for marked supplies and an optional sled that can be pulled free.
3. **River / apply the tool differently.** The pickaxe now clears rubble rather than only collecting a resource. A falling trunk forms a crossing. Jump a branch on the narrow bridge while maintaining position. A fall costs warmth/health and sends the player ashore.
4. **Far bank / plan ahead.** Build another fire, rest, replenish needs, and carry wood toward the lookout. A second wolf patrols the woods beyond the camp.
5. **Ridge / apply crouching differently.** Gusts slow the player and push them sideways. Their timing and force change between adventures, with rising wind and swaying trees as warnings. Players can time the exposed shortcut or take the longer covered route with food and water. A third wolf patrols the upper ridge between the fork and the lookout camp.
6. **Lookout / combine systems.** Mine the marked rocks and deposit six stones at the counterweight scale to lift the gate. Fill the cup from the final barrel beyond the gate, refill the generator, and start it. Use the powered spotlight to find the clearing marked by three amber lamps. The clearing flashes back and a reply sounds. Ignite the beacon with six wood, a burning torch, and generator power. A short rescue transition leads to a results screen.

## Rules, states, and feedback

Wood and stone are limited by gathering actions; timber renews. Crafting consumes exact costs only when all requirements are satisfied. Exposure raises wetness and drains warmth; freezing drains health. Sprinting and jumping spend stamina. Shelter dries and a fire rapidly restores warmth. Food and water replenish the template's hunger/thirst meters. Stowing the torch conserves fuel but removes its warmth protection.

Title, play, pause, crafting, failure, and rescue-result states are explicit. Pause/crafting stop survival time and disable player interaction. Failure restores the last in-session campfire checkpoint, retains route progress, and provides enough supplies to recover. Restart begins a fresh adventure.

The HUD shows the objective direction/distance, inventory, six survival meters, torch fuel, weather state, contextual action, hold progress, and session time. Amber markers link resources and the route. Original first-person tool animations, template movement audio, generated wind/rain, thunder, action sounds, and visible fire support feedback.

## Mechanics discussed in The Art of Game Design

| Category | Implementation |
|---|---|
| Space | Bounded 3D route with camps, river, ridge, shelters, and final lookout. |
| Objects, attributes, states | Wood, stone, cloth, fuel cup, food/water, torch, campfires, generator, and beacon; their quantities, fuel, health, and on/off states affect available actions. |
| Actions | Move, look, sprint, jump, crouch, mine, gather, craft, light, refill, toggle, rest, and signal rescue. |
| Rules | Exact crafting costs, stamina costs, exposure rates, interaction distances, checkpoint recovery, and the combined rescue condition. |
| Skill | Navigation, timing jumps, bracing during gusts, remembering tools, and planning resource use. |
| Chance | Each new adventure chooses a seeded layout for optional supplies, three wolf patrols, and gusts. Core materials and the route to rescue stay accessible. Weather and wildlife give cues before the player has to react. |

## Balance lens: Challenge

What challenges occur? Gathering while cooling, choosing when to rest, crossing water, managing torch fuel, and moving between gust shelters. Do challenges build? The safe trail introduces one action at a time; later sections reuse actions for crossing, bracing, and powering rescue. How can failure remain useful? Clear feedback identifies cold/water costs, and checkpoints retain progress. What needs measuring? First-run completion, failed jumps, time spent searching, and whether hunger/thirst are noticed before damage.

## Balance lens: Meaningful Choices

What choices affect outcomes? Spending wood on fire fuel, a torch rewrap, or saving six for the beacon; stowing a torch to conserve fuel; sprinting versus saving stamina; advancing versus warming up. Are choices understandable? Visible costs, inventory, fuel, and survival meters expose consequences. Is there always a dominant answer? The route should offer short fast crossings versus safe shelter stops, but testing must establish whether those choices feel useful. How are dead ends reduced? Renewable timber, a lookout vein that replenishes while more stone is needed, final fuel supplies, and recovery supplies support another attempt.

## Iteration record — fill with real playtesting

Implemented engineering changes include the original-template integration, safer persistence, contextual prompts, recovery checkpoints, and reuse of movement/tool/fuel mechanics. These are implementation revisions, **not evidence of human playtesting**.

For each test: record participant/date, build/commit, completion time, observed difficulty, feedback, resulting change, and a follow-up observation. The assignment requires meaningful modifications based on actual playtesting and visible GitHub history. Use the form in PLAYTEST.md; do not report fabricated sessions.
