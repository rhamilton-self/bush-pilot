# Bush/Cargo Pilot Sim: Project Brief and 12-Phase Plan

> **For Claude (read first):** This file is context carried over from an earlier chat. The user is building this project in Godot 4 (.NET) with C#. It is their first major project. They prefer **extremely brief explanations, minimal pleasantries, only information that adds to the discussion.** Do not expand scope. When asked about a specific phase, focus on that phase's section, respect the decisions below, and update the Progress Log if the user reports status.
>
> **Teaching format (important):** This is a **tutorial**, not a "Claude writes the project" process. The user types all code themselves. Work one small step at a time: short explanation of what and why, a code block, brief notes on what each part does, then a checkpoint describing what they should see. Never dump whole script files or generate files for them to copy in. Wait for the user to confirm each checkpoint before the next step. Fix mistakes plainly (the user caught a sign error in roll earlier).

---

## 1. Project Summary

A career bush/cargo pilot game. The player flies contracts between small airstrips, earns money, pays for fuel and repairs, and buys new aircraft.

- **Flight model:** "Simcade", not arcade. Force-based, with real stall behaviour. No full systems simulation (no detailed engine management, avionics, or failure trees).
- **Core challenge:** Weather (wind, gusts, low visibility) and demanding approaches to short, sloped, or gusty strips.
- **Progression:** Money in from contracts, money out for fuel and repairs. Spend savings on new aircraft.
- **Map:** Not highly detailed. Terrain is sparse and stylised. Airstrips are the detailed parts.
- **Art:** Stylised, low-poly. Placeholder art until late in the plan.

## 2. Background / Decisions Made

- Original idea was a tactical CQC shooter (injury, ragdolls, destructible terrain). Judged **too complex for a first project** and shelved. It remains a possible future project.
- Pilot sim chosen because it has one core loop, mostly scales by adding content, and stylised visuals hide a lot.
- Competition exists, but arcade-leaning-to-simcade bush flying with a strong economy loop is less served than full sims.
- Timeline is phase-based, not calendar-based. The user is progressing faster than the original week estimates, so dates are ignored. Order and the phase 3 gate are what matter.
- Flight feel was tuned by feel and is "close enough". The user plans to re-tune once there is terrain and more game to judge against.

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

## 4. Suggested Project Structure

```
res://
  scenes/        (aircraft, airstrips, world, ui)
  scripts/
    flight/      (FlightModel, AeroSurface, Engine, Gear, WindModel)
    economy/     (Contract, Bank, Wear, Repair)
    world/       (Airstrip, Weather, Terrain)
    ui/          (HUD, ContractBoard, Hangar, Map)
    data/        (AircraftDef : Resource, ContractDef, WeatherPreset)
  resources/     (aircraft .tres files, weather presets)
  assets/        (placeholder models, audio)
```

*Actual state:* all scripts currently sit flat in `res://scripts/` and the world is built in code from `Main.cs`. Reorganising into the structure above is optional and can wait until the aircraft becomes a data-driven resource (phase 10).

## 5. Flight Model Notes (for phases 2-4)

- Lift: `L = 0.5 * rho * V^2 * S * CL(alpha)`
- Drag: `D = 0.5 * rho * V^2 * S * CD(alpha)` (parasitic + induced)
- `CL(alpha)`: roughly linear up to critical AoA, then drops sharply (stall). Implemented as a smoothstep blend to a flat-plate curve, with hysteresis.
- Use **airspeed relative to the air mass** (groundspeed minus wind) for all aerodynamic forces. This is what makes wind matter. `AirData.Compute` already takes a wind vector (currently `Vector3.Zero`).
- Control surfaces contribute torque proportional to deflection x dynamic pressure. Include a simple damping term to avoid wobble.
- Propwash over tail/wing: boost airflow at low speed and high throttle, which helps low-speed control.
- Ground effect: increase lift and reduce induced drag within about one wingspan of the ground.
- Throttle gives thrust that falls off with airspeed. Prop torque/P-factor is optional but adds feel.
- Flaps: increase CL and CD, lower stall speed.
- Keep input smoothing and deadzones to make keyboard and controller usable.
- Tune with a **debug HUD** (airspeed, AoA, altitude, vertical speed, throttle, G-load).

## 6. The 12-Phase Plan

### Phase 1: Foundations
- Project, folder structure, flat test terrain, one placeholder aircraft body.
- Chase camera plus cockpit-ish camera toggle.
- Input map (controller and keyboard), with deadzones and smoothing.
- **Done when:** aircraft body exists in a scene, cameras switch, inputs read correctly.

### Phases 2-3: Flight model v1
- Thrust, gravity, lift and drag as functions of angle of attack and airspeed.
- Control surfaces as torque/force contributions (elevator, aileron, rudder, flaps).
- Stall (lift curve drops past critical AoA), plus stall-warning feedback.
- Debug HUD: airspeed, AoA, altitude, vertical speed, throttle.
- **Milestone:** it is satisfying to fly circuits with no objectives.
- **Gate:** do not proceed past phase 3 until flying feels good. Extend this phase if needed.

### Phase 4: Ground handling
- Wheel colliders/suspension, brakes, steering, takeoff roll, flare and landing.
- Landing-impact measurement (vertical speed, gear load) for later damage.
- Ground effect and propwash over the tail.
- **Done when:** takeoff roll, rotation, and landings are controllable and repeatable.

### Phase 5: Terrain and first airstrips
- Heightmap or generated low-poly terrain, with 2-3 handcrafted strips (flat, sloped, short).
- Basic stylised lighting, sky, and water.
- Runway placement tools so adding strips is cheap.

### Phase 6: Weather v1
- Steady wind (direction/speed) applied to airspeed vs. groundspeed.
- Gusts and turbulence as noise-driven force and torque.
- Windsock object at strips.
- Weather presets selectable per flight.

### Phase 7: Contracts and flow
- Cargo/passenger contract data (origin, destination, weight, payout, deadline).
- Weight and balance affecting takeoff performance and handling (mass and CG on the RigidBody).
- Flight state machine: hangar, takeoff, in flight, landed, results screen.

### Phase 8: Economy
- Fuel burn and fuel cost, payout calculation, bank balance.
- Wear and damage: hard landings, overspeed, overstress accumulate; repair costs.
- Save/load (JSON is fine).

### Phase 9: Navigation and UI
- Waypoint markers, simple map, compass/heading, distance and ETA.
- Menus: contract board, hangar, aircraft stats.
- Audio pass: engine pitch with RPM, wind, stall horn, touchdown.

### Phase 10: Second aircraft and progression
- Aircraft defined as data (resource files): mass, wing area, power, fuel, payload, gear.
- Purchase screen, one new aircraft with different handling.

### Phase 11: Weather v2 and challenge
- Low cloud/visibility (fog, cloud ceiling), rain.
- Weather-linked contract risk/reward.
- 4-6 strips total with distinct approach challenges.

### Phase 12: Polish and playtest
- Balance economy and difficulty, fix flight-feel issues.
- Get 3-5 outside playtesters.
- Build and export.

## 7. Rules to Avoid Getting Bogged Down

- Don't proceed past phase 3 until flying feels good.
- Placeholder art until phase 11 or later.
- **Cut order if behind:** second aircraft, then rain, then save/load polish.
- Keep a "Later" list instead of adding features mid-plan.
- Only add a feature once the current build is fun and stable.

## 8. Later List (explicitly out of scope for v1)

- NPC traffic
- Night flying
- Detailed instruments / IFR navigation
- Seasons and dynamic weather evolution
- Large detailed map
- Multiplayer
- Full systems simulation (engine failures, electrics, etc.)
- Pitch trim control (rate-based input like throttle, about 10 lines; offered, not yet added)
- Wing drop in stalls (one wing stalling first)
- Strut bottoming-out model for very hard landings
- Tailwheel aircraft variant

## 9. Risks and Pitfalls

- **Flight feel** is the make-or-break item. If it isn't fun to fly with no objectives, nothing else saves it.
- **Scope creep** from "simcade" drifting toward full sim. Resist.
- **Navigation:** keep simple (waypoint markers, basic compass or map) at first.
- **Economy balance:** do it last, with playtest data.
- **Physics instability** (jitter, explosions) when tuning forces. Clamp values and test at fixed timestep.

## 10. Design Ideas to Keep in Mind (from discussion)

- Approaches are the skill test: short, sloped, gusty strips, terrain-hugging valleys, go-around decisions.
- Risk vs. reward contracts: medical flights in bad weather pay more but risk the aircraft.
- Cargo weight and balance: overloading is tempting but dangerous.
- Fuel planning: remote strips with no fuel force range decisions.
- Wear and maintenance: one crash should set the player back economically.
- Landing damage design: no landing report screens. Landing wear accumulates silently via the `Touchdown` event. A mechanic NPC mentions it vaguely at repair time ("looks like you had some hard landings recently, that'll cost you X"). The HUD landing line is debug-only and should be hidden behind a debug toggle later.

## 11. Progress Log (update as you go)

| Phase | Status | Notes |
|------|--------|-------|
| 1 | Complete | World built in code from `Main.cs`; input actions registered in code; chase/cockpit cameras; debug HUD. |
| 2-3 | Complete | Full force-based flight model, tuned by feel. Stall has nose-drop moment, hysteresis and heavy post-stall drag. To be re-tuned once terrain exists. |
| 4 | Complete (step 5 untested) | Raycast-spring suspension, tyre friction, brakes, nose-wheel steering, takeoff verified, landing tracking via `Touchdown` event. Step 5 (ground effect and propwash) was written but not yet confirmed in testing. |
| 5 | Not started | |
| 6 | Not started | |
| 7 | Not started | |
| 8 | Not started | |
| 9 | Not started | |
| 10 | Not started | |
| 11 | Not started | |
| 12 | Not started | |

**Current issues / questions:**
- Confirm ground effect and propwash behave as expected (parked full-throttle tail speed about 20 m/s, no twitching; noticeably longer float in the flare).
- Possible later tweaks: climb rate (lower `EnginePower` toward 140000 if too strong), pitch trim control.

**Key tuning values found so far:**
Defaults are as given in the tutorial. The user has tuned some by feel, so check their current inspector values.

- Aircraft mass: 1100kg
- Camera chase rate: 5f
- Aircraft control response rate: 4f
- Inertia (pitch X, yaw Y, roll Z): (1825, 2667, 1285) kg·m²
- Engine: `EnginePower` 170000 W, `PropEfficiency` 0.8, `StaticThrust` 3000 N
- Wing: `WingArea` 16, `CL0` 0.3, `CLSlope` 5, `CD0` 0.04, `InducedDragK` 0.055, `IdlePropDrag` 0.012
- Stall: `StallAoADeg` 15, `StallBlendDeg` 6, `StallWarnMarginDeg` 3, `BuffetStrength` 2, `StallNoseDrop` 4, `StallDragGain` 3.5, `StallHysteresisDeg` 5, post-stall CL scale 0.7 (hard-coded in `Aero.LiftCoefficient`)
- Flaps: `FlapCL0Gain` 0.5, `FlapCD0Gain` 0.035, `FlapStallReductionDeg` 2, `FlapTrimShiftDeg` 3, `FlapSpeed` 0.4
- Trim: `TrimAoADeg` 1.5
- Control authority: Elevator 5.5, Aileron 4, Rudder 3
- Stability: Pitch 14, Yaw 10, Roll 5
- Damping: Pitch 10, Roll 6.5, Yaw 7
- Gear: `SuspensionLength` 0.6, `WheelRadius` 0.35, mains stiffness 22500 / damping 4000, nose stiffness 9000 / damping 1600
- Tyres: `TireGrip` 4000, `TireMu` 0.8, `RollingResistance` 0.03, `BrakeMu` 0.6, `MaxSteerDeg` 35
- Ground effect and propwash: `GroundEffectStrength` 2, `GroundLiftGain` 0.3, `PropDiscArea` 3.1, `PropwashFactor` 1
- Landing grades (sink rate, m/s): Smooth <1, Good <2, Firm <3, Hard <4.5, Crash above

## 12. Current Code State (for resuming)

**Scripts in `res://scripts/`:**
- `Main.cs`: builds the world in code (sky, light, ground, pillars, runway), spawns aircraft at (0, 1.5, 300), camera rig, HUD.
- `InputSetup.cs`: static class registering all actions.
- `AircraftInput.cs`: smoothed Pitch/Roll/Yaw, rate-based Throttle, Brake, Flaps (0-3).
- `AirData.cs`: struct computing relative velocity, local velocity, airspeed, AoA, sideslip.
- `Aero.cs`: dynamic pressure, lift/drag coefficients, stall factor.
- `Propulsion.cs`: thrust from power and static limit.
- `Wheel.cs`: wheel data class (setup plus live state).
- `TouchdownReport.cs`: `LandingGrade` enum, `TouchdownReport` struct, `Landing.GradeFor`.
- `Aircraft.cs`: `RigidBody3D` with all force/torque methods.
- `CameraRig.cs`: chase and cockpit cameras.
- `DebugHud.cs`: text readout.

**Conventions:**
- Aircraft forward = **-Z**, right = +X, up = +Y.
- Pitch +1 = nose up. Roll +1 = right wing down. Yaw +1 = nose right. Roll and yaw need a negative sign in torque/rotation maths.
- AoA positive when the airflow hits the belly. Sideslip positive when drifting right.
- Centre of mass pinned to the origin, so `GlobalPosition` is the CoM.

**`Aircraft._PhysicsProcess` order:** reset keys, `AirData.Compute`, (debug free-fly returns early), `UpdateFlaps`, `ApplySuspension`, `TrackLanding`, `ApplyTireForces`, `ApplyThrust`, `UpdateGroundEffect`, `ApplyAerodynamics`, `ApplyControlTorques`, `ApplyBuffet`.

**Controls:** W/S pitch, A/D roll, Q/E yaw and nose-wheel steering, Shift/Ctrl throttle, F/R flaps down/up, Space brake, C camera toggle, Backspace reset to runway, Delete reset in air (300 m, 45 m/s). Gamepad equivalents bound.

**Events:** `Aircraft.Touchdown` (`TouchdownReport`) fires after each landing. Phase 8's wear tracker should subscribe to it.

## 13. How to Resume in a New Chat

Upload this file and paste something like:

> Context is in the attached file. I'm on **Phase N**. Current status: [what works / what's broken]. Here's my relevant code: [paste or attach]. Help me with [specific task]. Continue in tutorial format: one step at a time, I type the code.

Tips:
- Include the relevant scripts (e.g. `Aircraft.cs`) so Claude doesn't guess at your structure.
- Include error messages and the Godot version.
- Update the Progress Log and tuning values before uploading again.
