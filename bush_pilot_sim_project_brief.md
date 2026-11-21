# Bush/Cargo Pilot Sim: Project Brief and 14-Phase Plan

> **For Claude (read first):** This file is context carried over from an earlier chat. The user is building this project in Godot 4 (.NET) with C#. It is their first major project. They prefer **extremely brief explanations, minimal pleasantries, only information that adds to the discussion.** The scope was deliberately expanded by the user (see section 6, the logistics design), so follow the phase plan below, but do not add further scope on your own. When asked about a specific phase, focus on that phase's section, respect the decisions below, and update the Progress Log if the user reports status.
>
> **Teaching format (important):** This is a **tutorial**, not a "Claude writes the project" process. The user types all code themselves. Work one small step at a time: short explanation of what and why, a code block, brief notes on what each part does, then a checkpoint describing what they should see. Never dump whole script files or generate files for them to copy in. Wait for the user to confirm each checkpoint before the next step. Fix mistakes plainly (the user has caught a sign error in roll and an enum type slip in the camera code).

---

## 1. Project Summary

A career bush/cargo pilot game built around a **logistics sandbox**. The player buys cargo, flies it to remote airstrips whose demand rises and falls, and is paid on delivery. Supplied communities grow (more buildings, longer runways, new services), which opens up bigger aircraft and further destinations.

- **Flight model:** "Simcade", not arcade. Force-based, with real stall behaviour. No full systems simulation (no detailed engine management, avionics, or failure trees).
- **Core challenge:** Weather, night flying, navigation to hard-to-find small strips, and demanding approaches to short, sloped, rough or gusty strips. **The risk comes from the flying, not from the economy.**
- **Progression:** Tiny aircraft (Cub-class) up to twin-prop cargo aircraft. Players can over-scope their abilities and their aircraft's landing capability.
- **Map:** Expansive, procedurally generated. Airstrips are the detailed parts.
- **Art:** Stylised, low-poly. Placeholder art until late in the plan.

## 2. Background / Decisions Made

- Original idea was a tactical CQC shooter (injury, ragdolls, destructible terrain). Judged **too complex for a first project** and shelved. It remains a possible future project.
- Pilot sim chosen because it has one core loop, mostly scales by adding content, and stylised visuals hide a lot.
- Timeline is phase-based, not calendar-based. The user is progressing faster than the original week estimates, so dates are ignored. Order and the phase 3 gate are what matter.
- Flight feel was tuned by feel and is "close enough". The user plans to re-tune once there is more game to judge against (wind, loaded aircraft, surfaces, real contracts).
- **World approach:** terrain, water and runways are code-generated. This is now the permanent approach (procedural base, authored templates), not temporary scaffolding. `TerrainHeight.Height(x, z)` stays a pure function and is the height query interface for terrain, map, placement and weather. **Rule: nothing outside `TerrainHeight`, `Terrain` and `Airstrip` should know how the world is generated.**
- The lighting/sky polish step of phase 5 was deliberately skipped (cosmetic, redone later).
- **Determinism rule:** everything generated (terrain, strip positions, names, village layouts, starting economy) must come from **seeded** random generators, never the global one. A save stores the seed, world settings and the player's changes, and the world rebuilds from them.

## 3. Tech Stack and Setup

- **Engine:** Godot 4.x **.NET build**, language **C#**.
- **Physics:** Jolt (built in from Godot 4.4).
- **IDE:** VS Code or Rider.
- **Version control:** Git from day one.
- **Principles:**
  - Keep flight maths in plain C# classes with minimal Node dependencies, so they are easy to tune and test.
  - Use `[Export]` fields so tuning can be done live in the inspector.
  - Define aircraft as data using custom `Resource` classes.
  - Aircraft physics goes in `_PhysicsProcess` and/or `_IntegrateForces` (on a `RigidBody3D`).
  - Define prices, fuel costs and payouts in terms of **distance and flight time**, so they scale with world size automatically.

## 4. Suggested Project Structure

```
res://
  scenes/        (aircraft, airstrips, world, ui)
  scripts/
    flight/      (FlightModel, AeroSurface, Engine, Gear, WindModel)
    economy/     (Contract, Bank, Wear, Repair, Market)
    world/       (Airstrip, Weather, Terrain, Community)
    ui/          (HUD, ContractBoard, Hangar, Map)
    data/        (AircraftDef : Resource, SurfaceDef, CargoDef, WeatherPreset)
  resources/     (aircraft .tres files, surface defs, weather presets)
  assets/        (placeholder models, audio)
```

*Actual state:* all scripts currently sit flat in `res://scripts/` and the world is built in code from `Main.cs`. Reorganising into the structure above is optional and can wait until the aircraft becomes a data-driven resource (phase 8).

## 5. Flight Model Notes

- Lift: `L = 0.5 * rho * V^2 * S * CL(alpha)`
- Drag: `D = 0.5 * rho * V^2 * S * CD(alpha)` (parasitic + induced)
- `CL(alpha)`: roughly linear up to critical AoA, then drops sharply (stall). Implemented as a smoothstep blend to a flat-plate curve, with hysteresis.
- Use **airspeed relative to the air mass** (groundspeed minus wind) for all aerodynamic forces. This is what makes wind matter. `AirData.Compute` already takes a wind vector (currently `Vector3.Zero`).
- Control surfaces contribute torque proportional to deflection x dynamic pressure. Pitch and yaw use the tail's airspeed (includes propwash); roll uses plain airspeed. Damping terms avoid wobble.
- Propwash: tail airspeed boosted from thrust via momentum theory.
- Ground effect: induced drag reduction and lift-slope boost within about one wingspan of the ground (measured by raycast to terrain).
- Throttle gives thrust that falls off with airspeed.
- Flaps: increase CL and CD, lower stall speed, small nose-down trim shift.
- Pitch trim: rate-based input (like throttle) that shifts the hands-off AoA, so the player can set approach speed.
- Keep input smoothing and deadzones to make keyboard and controller usable.
- Tune with a **debug HUD** (airspeed, AoA, altitude, vertical speed, throttle, trim, gear loads, etc.).
- **Current aircraft is roughly Cessna-180 class** (1100 kg, 170 kW). The planned first aircraft is **Cub-class** (about 500 kg, 55-75 kW, roughly 38 m/s cruise), which needs a retune once aircraft are data (phase 8).

## 6. Core Design: The Logistics Loop

All decisions below come from the user's design discussion. Numbers are placeholders unless stated.

### Cargo and pay
- **Four cargo types to start, expandable:** food, building materials, medical supplies, plus **passengers** (separate category, stored differently from cargo). Easy to add more types later.
- **The player buys cargo** (at the hub or at strips with surplus) and is **paid on delivery**. Crashing loses the cargo and brings a repair bill. Failure is meant to be possible and to be feared.
- **Pay falls as a destination's demand is filled.** Well-supplied or large communities pay **less**, deliberately pushing players toward riskier, more remote, harder flights. Safeguards: a **pay floor** so established strips still pay a little, and **scarcity-based pricing** (surplus is cheap to buy, scarce goods pay more).
- **Strips export what they produce**, so there is cargo on return legs and players think about **chaining** journeys, not just hub-and-back.
- **Urgent cargo contracts with a bonus** (mostly medical) tempt players into risk.
- Bigger communities produce food and building materials faster and cheaper, but pay less to receive them.
- **Late-game passenger traffic:** bigger communities need increasing numbers of passengers moved between them and the hub occasionally. Paid per passenger per distance, regardless of demand. This is the income floor once individual strips stop paying well.
- **Demand changes between flights, not during them,** so a payout isn't pulled out from under the player mid-trip. *(Open: exact rule, e.g. locked quotes at load time. Decide in phase 10.)*

### The hub
- The hub is a **port** with a **steady, limited supply**, not unlimited. Ships deliver a decent amount of resources **once per game day at a set time**.
- The hub always has a hangar, services and a long surface.

### Communities (no levels, just growth)
- Basis is **population** with a **population cap**. Example (placeholder): a 3-building settlement with 6 people.
- **Food and building materials, both supplied,** let the settlement build a new building, raising the cap (e.g. to 8).
- **Passengers** fill population up to the cap (e.g. 2 more passengers can be dropped after the cap rises). Passengers can also restore lost population.
- **Medical supplies** make up the urgent contracts and prevent population dropping (e.g. 6 might risk falling to 5 without them).
- **Visible growth:** very small villages (Transport Fever 2 style, but tiny). Buildings appear under construction, then finished. Placeholder boxes in construction stages are fine.
- **Services unlock as communities grow:** runway length and surface, fuel, repair, **accommodation** (medium-established strips), **hangar** (only well-established communities).
- **Established communities become self-sufficient** after a point, so the player never has to "spin plates" to keep them open. Only fledgling communities can slip, and never below a small floor. Neglect should never punish absence. The player should feel they are expanding outward and moving into the passenger space.
- Optional urgent contracts at established strips are bonuses with no penalty for ignoring them.

### Airstrip types and surfaces
Different surfaces and roughness discourage larger aircraft from small strips. Length is dynamic within a type, not a fixed list. Strips improve as communities grow.

| Type | Typical length (placeholder) | Notes |
|------|------------------------------|-------|
| Dirt | 200-400 m | Roughest, lowest grip, muddy when wet |
| Grass | 300-500 m | Soft and bumpy, slow when wet |
| Gravel | 400-700 m | Moderate roughness, decent grip |
| Small tarmac | 600-900 m | Smooth, good grip |
| Medium tarmac | 900-1400 m | Centreline marking, accommodation common |
| Long tarmac with markings | 1500+ m | Hub / port class, full markings, lights |

Per-surface properties (all in a `SurfaceDef` resource): dry and wet friction (`TireMu`), rolling resistance, **roughness** (bump amplitude, which drives suspension loads and wear), **bearing weight** (heavy aircraft sink and bog down, with a steep rolling-resistance and wear penalty beyond it), visuals and markings. Starting guesses: dirt mu about 0.5 dry / 0.3 wet, rolling resistance about 0.06; small tarmac close to the current values (mu 0.8, rolling resistance 0.03). Wet weather worsens soft surfaces most. Gear/tyre type should matter (bush tyres cope with rough strips, small tyres on bigger aircraft do not).
- *Implementation direction:* the current `TireMu` and `RollingResistance` constants become per-contact lookups. The `Airstrip` gets a `Surface` field and decides the surface under a wheel by containment. Off-strip ground defaults to rough scrub or grass.

### Time and night
- **Linear in-game time. One game day is 90 real minutes.**
- Flights should be short enough to fit a couple of legs per day. The player plans to avoid **night landings**, or accepts the extra difficulty (navigation and landing).
- **Overnighting** is only possible at strips with accommodation (medium-established). It skips to morning. There is no tiredness mechanic, and sleeping is never essential.
- **Weather follows the game clock,** so resting can bring better weather. Time of day and upcoming weather are both planning factors.
- Implementation direction: weather is a **pure function** `Weather.At(position, time)`. Forecasts are free (evaluate ahead), no weather state needs saving, and weather can vary by region. Fixed presets remain as a testing override.

### Aircraft, hangars and fleet
- The player **owns multiple aircraft**. Any owned aircraft can be selected at **any hangar**. Aircraft not in use are "ethereal": **location is not stored.** The aircraft being replaced is despawned.
- **Cargo is lost when changing aircraft at a hangar,** with a clear warning first.
- **Damage persists per aircraft** and is saved.
- At the hangar the player chooses a **seat configuration**: seats installed cap the cargo capacity. Aircraft have a shared payload weight limit, cargo volume, and maximum seats, which allows cargo, passenger and hybrid configurations.
- Hangars unlock only at well-established communities. A swapped-in aircraft should be able to take off from where the player is (warn about big aircraft at short or rough strips).

### Failure and recovery
- **Forced landing:** recovery to the hub for a fee that scales with distance.
- **Proper crash:** two options. Return to the hub **free but lose the aircraft,** or **recover the aircraft to the hub and repair it at cost.** This makes small aircraft feel less risky once established and means expensive aircraft need good forward planning.
- **Hard bankruptcy** is possible if the player cannot afford a crash and it is their last aircraft. *(Open: exact end state, assumed to end the run.)*
- **Landing wear is silent:** no landing report screens. Wear accumulates via the `Touchdown` event, and a mechanic NPC mentions it vaguely at repair time ("looks like you had some hard landings recently, that'll cost you X"). The HUD landing line is debug-only.
- Rough surfaces and overweight operations add to wear.

### Saves
- **Saving only at hangars** (well-established strips). This gives a rogue-lite run structure: supplying several risky small strips in one run is riskier, and successful takeoffs matter.
- **In-air suspend-save** that is deleted when loaded, so quitting mid-flight doesn't lose hours.
- Crash recovery is an in-game outcome, so no reload is needed for it.

### Navigation and map
- **No pointers to strips.** A **map and compass** to begin with, and a proper **NAV system upgrade** for aircraft later.
- The map shows terrain and a coordinate grid and the player's current position. The player can **draw lines** to plot distances and headings. **Unknown strips give coordinates only, with no marker.** Strips are marked once found. Contracts give grid references.
- The map must feel big enough that finding small, poorly established strips is a real challenge. Terrain needs distinctive landmarks (coastlines, lakes, rivers, ridges). The map renders from the same height function, so it needs no extra data.
- Possible later: rumours or exploration to discover strips.

### World and generation
- **Expansive map:** a flight across the map takes **at least 30 minutes in the slowest (Cub-class) aircraft**. Target roughly **100-150 km** across (about 70 km is a 30-minute Cub flight). The user would like it bigger if feasible, and wants freedom to enlarge it later. Far strips should need building-out to reach, and early aircraft should feel restricted from flying cross-map. Hub resets should feel like a big step back.
- **Chunked terrain** with distance-based levels of detail. **Floating origin** (re-centring the world as the aircraft moves) is needed because 32-bit floats get jittery beyond roughly 20-30 km from the origin.
- **Procedural base + authored templates.** Ridged noise, domain warping and height/moisture regions give landforms. Landmarks (peak, lake, island chain) and village layouts are rule-placed **templates/stamps** (extending the `Pad` idea). Presentation does most of the prettiness: instanced trees and rocks, palette, water, fog, time-of-day lighting. Keep a **fixed dev seed** for tuning.
- **Strip generator:** the survey we did by hand becomes an automated site-finder. The hub is a coastal port. Difficulty scales with distance from the hub. The network must be connected and playable on any seed, with validity checks and a re-roll if a seed fails. Generates names and village layouts.
- **`WorldSettings`:** seed, size preset (2-3 presets such as Medium, Large, Huge, no tiny worlds), strip density (a range, not an exact count). Build generation as a pure function of `WorldSettings` in phase 7, expose the seed as a dev setting, and add the new-game screen in phase 14.

## 7. The 14-Phase Plan

### Phase 1: Foundations (complete)
- Project, folder structure, flat test terrain, one placeholder aircraft body.
- Chase camera plus cockpit-ish camera toggle.
- Input map (controller and keyboard), with deadzones and smoothing.

### Phases 2-3: Flight model v1 (complete)
- Thrust, lift and drag, control torques, stall with warning, flaps, debug HUD.
- **Gate:** it is satisfying to fly circuits with no objectives.

### Phase 4: Ground handling (complete)
- Raycast-spring suspension, tyre friction, brakes, nose-wheel steering, takeoff and landing.
- Landing-impact measurement (`Touchdown` event). Ground effect and propwash.

### Phase 5: Terrain and first airstrips (complete)
- Noise terrain with heightmap collision, water, fog, `Airstrip` class, three strips.
- Also: movable camera, pitch trim, revised flap values.

### Phase 6: Weather v1
- Steady wind applied to airspeed vs. groundspeed (feed a wind vector into `AirData.Compute`).
- Wind as a function of **position and game time** from the start. Gusts and turbulence as noise-driven force and torque.
- Windsock object at strips.
- Fixed weather presets selectable as a testing override.
- **Done when:** crosswinds and gusts make approaches meaningfully harder, and presets switch cleanly.

### Phase 7: World scale
- `WorldSettings` (seed, size preset, strip density), all generation seeded.
- Chunked terrain with levels of detail. Floating origin. Heights stay a pure function.
- Richer terrain (ridged noise, domain warping, regions), instanced vegetation, landmark stamps/templates.
- Strip site-finder and generator (coastal hub port, difficulty by distance, validity check, names). Each generated strip carries a size class and surface type (surface physics arrives in phase 8).
- **Done when:** a 100+ km world generates and flies without jitter, with believable coasts, ridges and valleys, and a valid strip network on several seeds.

### Phase 8: Aircraft as data and surfaces
- `AircraftDef` resources (mass, wing area, power, fuel, payload, seats, gear, tyres). Cub-class first aircraft, plus the ladder up to a twin-prop cargo aircraft. Retune the flight model per aircraft.
- Surface physics: `SurfaceDef` (friction, wet factor, rolling resistance, roughness, bearing weight) applied per wheel contact. Visuals and markings per surface type.
- **Done when:** a Cub and a larger aircraft both fly, and the same strip feels very different to each on dirt versus tarmac.

### Phase 9: Game clock and night
- 90-minute game day, sun position, day/night lighting, time scaling.
- Night landing difficulty (lighting at larger strips). Overnight at accommodation strips. Weather linked to the clock.
- Hooks for the hub's daily delivery time.

### Phase 10: Cargo and market
- Four cargo types plus passengers. Buying cargo, payout on delivery, cargo loss on crash.
- Seat/cargo configuration with a shared weight limit. Weight and balance affecting takeoff and handling.
- Hub with a daily ship delivery. Scarcity pricing with a floor. Demand rules (decide locked quotes). Urgent contracts with bonuses.
- Fuel burn and fuel cost, bank balance.

### Phase 11: Communities
- Population, cap and growth. Building construction stages. Medical urgency. Self-sufficiency at established strips.
- Services and runway/surface upgrades as communities grow. Export production. Late-game passenger traffic between large communities and the hub.

### Phase 12: Map and navigation
- Map with terrain and grid, compass, drawable distance/heading lines. Known/unknown strip rules (coordinates only for unknown). Contracts give grid references.
- Hooks for the later NAV system upgrade.

### Phase 13: Hangars, fleet and failure
- Owning multiple aircraft, hangar selection, seat configuration, cargo-loss warning on swap, persisted damage.
- Forced-landing and crash recovery, repair costs, silent wear and the mechanic NPC, bankruptcy.
- Saves at hangars and the in-air suspend-save. JSON saves of seed, settings and player changes.

### Phase 14: Weather v2, polish and playtest
- Low cloud/visibility, rain, regional weather variation.
- New-game screen (seed, size, strip density), menus, audio pass, balance.
- Get 3-5 outside playtesters. Build and export.

## 8. Rules to Avoid Getting Bogged Down

- Don't proceed past phase 3 until flying feels good (done).
- Placeholder art until the polish phase.
- **Cut order if behind:** weather v2 (rain), the new-game settings screen, extra aircraft on the ladder, then save polish.
- Keep a "Later" list instead of adding features mid-plan.
- Only add a feature once the current build is fun and stable.
- Keep the economy simple: about 4-6 strips worth of systems at first, plain numbers, test with a few seeds before scaling up.

## 9. Later List (explicitly out of scope for v1)

- NPC traffic
- Detailed instruments / IFR navigation (beyond the planned NAV upgrade)
- Seasons and dynamic weather evolution beyond the clock-driven model
- Multiplayer
- Full systems simulation (engine failures, electrics, etc.)
- Wing drop in stalls (one wing stalling first)
- Strut bottoming-out model for very hard landings
- Tailwheel aircraft variant
- Modelled/sculpted hand-built world (procedural is now the plan)
- Lighting and sky polish (skipped in phase 5)
- Chase camera terrain collision (camera can currently go underground when orbiting low)
- Gamepad camera-look binding (needs a modifier button, since the right stick is yaw)
- Ground effect over water (currently measured against the seabed)
- Water is not solid: ditching and sea-crash handling belongs with the damage system
- Strip discovery via rumours or exploration
- Larger world size presets beyond the first set

## 10. Risks and Pitfalls

- **Flight feel** is the make-or-break item. If it isn't fun to fly with no objectives, nothing else saves it.
- **Scope creep.** The logistics sandbox is already large. Stick to the phase plan.
- **Float precision** on a 100+ km world. Plan floating origin early in phase 7 (all world nodes under one root that can shift).
- **Generation validity.** Any seed must produce a connected, playable strip network. Needs checks and a re-roll.
- **Snowballing.** Well-supplied strips produce more, which can run away. Use diminishing returns, caps and the pay floor.
- **Death spirals.** A crash and repair bill must not strand the player with no way to recover (recovery options and cheap early aircraft help), while still allowing hard bankruptcy.
- **Balance scales with world size.** Define costs and payouts by distance and flight time.
- **Physics instability** (jitter, explosions) when tuning forces. Clamp values and test at fixed timestep.

## 11. Progress Log (update as you go)

| Phase | Status | Notes |
|------|--------|-------|
| 1 | Complete | World built in code from `Main.cs`; input actions registered in code; chase/cockpit cameras; debug HUD. |
| 2-3 | Complete | Full force-based flight model, tuned by feel. Stall has nose-drop moment, hysteresis and heavy post-stall drag. Pitch trim added in phase 5. To be re-tuned once wind, loads and surfaces exist. |
| 4 | Complete | Raycast-spring suspension, tyre friction, brakes, nose-wheel steering, takeoff, landing tracking via `Touchdown` event, ground effect and propwash (confirmed working). |
| 5 | Complete | Noise terrain (4 km, 16 m cells) with heightmap collision, water, fog, `Airstrip` class and three strips. Lighting/sky polish skipped on purpose. Also added: movable camera, pitch trim, revised flap values. |
| 6 | Not started | Next up. |
| 7 | Not started | |
| 8 | Not started | |
| 9 | Not started | |
| 10 | Not started | |
| 11 | Not started | |
| 12 | Not started | |
| 13 | Not started | |
| 14 | Not started | |

**Current issues / questions:**
- Approach speed fix (flap values plus pitch trim) is applied, but the feel of approaches hasn't been explicitly confirmed. Expected: full flaps, throttle 0, trim 0 gives about 32-33 m/s hands-off.
- `N` (cycle strips) is a test aid and will be replaced by the hangar/contract flow later.
- Fog density 0.0002 is a placeholder and becomes part of the visibility/weather system.
- Possible later tweak: climb rate (lower `EnginePower` toward 140000 if too strong).

**Open design decisions:**
- Exact demand rule (locked quotes at load time or not), decide in phase 10.
- Bankruptcy end state (assumed to end the run).
- Passenger weight and how seats cost cargo volume.
- Whether larger strips have lights at night, and which strips count as having accommodation.
- Cargo type details (weight and bulk per type).

**Key tuning values found so far:**
Defaults are as given in the tutorial. The user has tuned some by feel, so check their current inspector values. These are for the current Cessna-class aircraft.

- Aircraft mass: 1100kg
- Camera chase rate: 5f
- Aircraft control response rate: 4f
- Inertia (pitch X, yaw Y, roll Z): (1825, 2667, 1285) kg·m²
- Engine: `EnginePower` 170000 W, `PropEfficiency` 0.8, `StaticThrust` 3000 N
- Wing: `WingArea` 16, `CL0` 0.3, `CLSlope` 5, `CD0` 0.04, `InducedDragK` 0.055, `IdlePropDrag` 0.012
- Stall: `StallAoADeg` 15, `StallBlendDeg` 6, `StallWarnMarginDeg` 3, `BuffetStrength` 2, `StallNoseDrop` 4, `StallDragGain` 3.5, `StallHysteresisDeg` 5, post-stall CL scale 0.7 (hard-coded in `Aero.LiftCoefficient`)
- Flaps: `FlapCL0Gain` 0.7, `FlapCD0Gain` 0.06, `FlapStallReductionDeg` 2, `FlapTrimShiftDeg` 1, `FlapSpeed` 0.4
- Trim: `TrimAoADeg` 1.5, `TrimRate` 0.15/s, `TrimRangeDeg` 5
- Control authority: Elevator 5.5, Aileron 4, Rudder 3
- Stability: Pitch 14, Yaw 10, Roll 5
- Damping: Pitch 10, Roll 6.5, Yaw 7
- Gear: `SuspensionLength` 0.6, `WheelRadius` 0.35, mains stiffness 22500 / damping 4000, nose stiffness 9000 / damping 1600
- Tyres: `TireGrip` 4000, `TireMu` 0.8, `RollingResistance` 0.03, `BrakeMu` 0.6, `MaxSteerDeg` 35 (these become per-surface in phase 8)
- Ground effect and propwash: `GroundEffectStrength` 2, `GroundLiftGain` 0.3, `PropDiscArea` 3.1, `PropwashFactor` 1, `WingSpan` 11
- Landing grades (sink rate, m/s): Smooth <1, Good <2, Firm <3, Hard <4.5, Crash above
- Camera: chase offset (0, 3, 14), `LookAhead` 6, cockpit offset (0, 0.7, -1.5), `KeyLookRate` 2, `MouseLookSens` 0.005, `LookRecenterRate` 4, zoom range 0.5-2.5, camera `Far` 12000
- Terrain: seed 1234, size 4096 m, cell 16 m, `WaterLevel` -6 m. Natural height = `-30 + shaped*450 + detail*(4 + 25*shaped)` where `shaped` is squared broad noise (freq 0.0004, 3 octaves) and `detail` is noise at freq 0.004 (2 octaves). Edge falloff from 1500 m to 2048 m blends down to -25 m (seabed). Pad margin 25 m, blend 150 m.
- Environment: fog density 0.0002, water plane 40 km with 200x200 subdivisions (needed to stop shadow jitter)
- Strips (position X, Z; heading; size; slope): **Home Strip** (0, 0), 0°, 800 x 20 m, flat. **Valley Strip** (700, -1050), 180°, 450 x 15 m, 4% (rises in the takeoff direction). **Cove Strip** (1300, 880), 0°, 320 x 12 m, flat. Elevation is taken from the natural terrain height at each centre (minimum 3 m).

## 12. Current Code State (for resuming)

**Scripts in `res://scripts/`:**
- `Main.cs`: builds sky/light/fog (`BuildEnvironment`), water (`BuildWater`), creates `TerrainHeight`, registers strips via `AddStrip` (pads must be added **before** `Terrain.Build`), builds terrain, adds strips to the tree, spawns aircraft at the home strip, camera rig and HUD. `_Process` handles `next_strip` (cycles strips and teleports the aircraft).
- `InputSetup.cs`: static class registering all actions.
- `AircraftInput.cs`: smoothed Pitch/Roll/Yaw, rate-based Throttle and Trim, Brake, Flaps (0-3).
- `AirData.cs`: struct computing relative velocity, local velocity, airspeed, AoA, sideslip.
- `Aero.cs`: dynamic pressure, lift/drag coefficients, stall factor.
- `Propulsion.cs`: thrust from power and static limit.
- `Wheel.cs`: wheel data class (setup plus live state).
- `TouchdownReport.cs`: `LandingGrade` enum, `TouchdownReport` struct, `Landing.GradeFor`.
- `Aircraft.cs`: `RigidBody3D` with all force/torque methods, `RunwayStart` / `AirStart` transforms, `RequestReset(bool toRunway)`, `ContinuousCd` on.
- `TerrainHeight.cs`: `Pad` struct and `TerrainHeight` class (pure height function: `Natural`, `Height`, `AddPad`, `WaterLevel`).
- `Terrain.cs`: `StaticBody3D` that builds the vertex-coloured mesh and `HeightMapShape3D` collision from `TerrainHeight`.
- `Airstrip.cs`: `Node3D` with `StripName`, `Length`, `Width`, `Slope`, `HeadingDeg`; `ToPad()`, runway visuals, `StartTransform()`, `AirTransform(altitude)`.
- `CameraRig.cs`: chase and cockpit cameras with orbit/look, zoom and recentre.
- `DebugHud.cs`: text readout.

**Conventions:**
- Aircraft forward = **-Z**, right = +X, up = +Y.
- World: **north = -Z, east = +X**. Strip headings are degrees clockwise from north; node yaw = -heading. Positive slope rises toward the strip's forward (takeoff) direction.
- Airstrip `Position` is the runway centre and its Y is the elevation there.
- Pitch +1 = nose up. Roll +1 = right wing down. Yaw +1 = nose right. Roll and yaw need a negative sign in torque/rotation maths.
- AoA positive when the airflow hits the belly. Sideslip positive when drifting right.
- Centre of mass pinned to the origin, so `GlobalPosition` is the CoM.
- Event/input types: gamepad buttons are `JoyButton` / `InputEventJoypadButton`; mouse buttons are `MouseButton` / `InputEventMouseButton`. Don't mix them.

**`Aircraft._PhysicsProcess` order:** reset keys, `AirData.Compute`, (debug free-fly returns early), `UpdateFlaps`, `ApplySuspension`, `TrackLanding`, `ApplyTireForces`, `ApplyThrust`, `UpdateGroundEffect`, `ApplyAerodynamics`, `ApplyControlTorques`, `ApplyBuffet`.

**Controls:**
- Keyboard: W/S pitch, A/D roll, Q/E yaw and nose-wheel steering, Shift/Ctrl throttle, Z/X pitch trim down/up, F/R flaps down/up, Space brake, C camera toggle, arrow keys look, right mouse + move looks, mouse wheel zooms chase camera, Backspace reset to the current strip's runway, Delete reset 250 m above the current strip's near end at 45 m/s, N next strip.
- Gamepad: left stick pitch/roll, right stick X yaw, triggers throttle, D-pad up/down flaps, D-pad left/right trim, A brake, Y camera toggle, Back reset, Start next strip.

**Events:** `Aircraft.Touchdown` (`TouchdownReport`) fires after each landing. The wear tracker (phase 13) should subscribe to it.

## 13. How to Resume in a New Chat

Upload this file and paste something like:

> Context is in the attached file. I'm on **Phase N**. Current status: [what works / what's broken]. Here's my relevant code: [paste or attach]. Help me with [specific task]. Continue in tutorial format: one step at a time, I type the code.

Tips:
- Include the relevant scripts (e.g. `Aircraft.cs`) so Claude doesn't guess at your structure.
- Include error messages and the Godot version.
- Update the Progress Log and tuning values before uploading again.
