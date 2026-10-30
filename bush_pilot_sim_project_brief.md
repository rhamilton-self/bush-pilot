# Bush/Cargo Pilot Sim: Project Brief and 12-Phase Plan

> **For Claude (read first):** This file is context carried over from an earlier chat. The user is building this project in Godot 4 (.NET) with C#. It is their first major project. They prefer **extremely brief explanations, minimal pleasantries, only information that adds to the discussion.** Do not expand scope. When asked about a specific phase, focus on that phase's section, respect the decisions below, and update the Progress Log if the user reports status.

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

## 5. Flight Model Notes (for phases 2-4)

- Lift: `L = 0.5 * rho * V^2 * S * CL(alpha)`
- Drag: `D = 0.5 * rho * V^2 * S * CD(alpha)` (parasitic + induced)
- `CL(alpha)`: roughly linear up to critical AoA, then drops sharply (stall). Use an `AnimationCurve`/`Curve` exported for tuning.
- Use **airspeed relative to the air mass** (groundspeed minus wind) for all aerodynamic forces. This is what makes wind matter.
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
- Landing damage design: no landing report screens. Landing wear accumulates silently via the Touchdown event. A mechanic NPC mentions it vaguely at repair time (“looks like you had some hard landings recently, that’ll cost you X”).

## 11. Progress Log (update as you go)

| Phase | Status | Notes |
|------|--------|-------|
| 1 | Complete | |
| 2-3 | Not started | |
| 4 | Not started | |
| 5 | Not started | |
| 6 | Not started | |
| 7 | Not started | |
| 8 | Not started | |
| 9 | Not started | |
| 10 | Not started | |
| 11 | Not started | |
| 12 | Not started | |

**Current issues / questions:**
-

**Key tuning values found so far (mass, wing area, CL max, etc.):**
-Aircraft mass: 1100kg
-Camera chase rate: 5f
-Aircraft control response rate: 4f

## 12. How to Resume in a New Chat

Upload this file and paste something like:

> Context is in the attached file. I'm on **Phase N**. Current status: [what works / what's broken]. Here's my relevant code: [paste or attach]. Help me with [specific task].

Tips:
- Include the relevant scripts (e.g. `FlightModel.cs`) so Claude doesn't guess at your structure.
- Include error messages and the Godot version.
- Update the Progress Log and tuning values before uploading again.
