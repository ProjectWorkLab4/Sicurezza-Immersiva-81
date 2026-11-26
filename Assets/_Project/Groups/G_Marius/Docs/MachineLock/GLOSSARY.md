# Scenario 08 — Machine Lock

Domain vocabulary for Scenario 08 (stamping-room hydraulic press emergency, LOTO isolation, PPE selection, and hot-oil spill containment). Extends the shared VR input vocabulary without redefining it.

## Language

### UI and Guidance Surfaces

**World Label**:
A floating 3D UI card anchored in world space to a scene object via the shared label system, subject to scene depth occlusion.
_Avoid_: Tooltip, floating hint, 3D HUD

**Wrist Hint**:
A concise directive shown on the player's non-dominant wrist panel guiding the current step in Guided mode.
_Avoid_: Tooltip, popup hint

**Event Alert**:
A high-priority temporary notification presented in front of the player's gaze for critical hazards, tester readouts, or blocked actions.
_Avoid_: Error tooltip, popup banner

**Action Feedback**:
A brief low-priority status line triggered by hovering or acting on a scenario object.
_Avoid_: Toast, tooltip

**Diegetic Plaque**:
A physical sign or control nameplate mounted directly on a prop or wall so its label belongs to the environment and never clips through geometry.
_Avoid_: World label, UI canvas

**Guided Wayfinding**:
Proactive environmental cues in Guided mode—yellow safety floor paintings, glowing Containment Zone floor rings, valve indicators, and an exit beacon—that show the player how to navigate the procedure without text clutter.
_Avoid_: Quest markers, breadcrumbs, tutorial arrows

### Spatial Layout and Hazards

**Dry Perimeter**:
The oil-free walkable corridor surrounding the hot-oil spill and hydraulic press along the room walls.
_Avoid_: Safe zone, outer floor

**Containment Zone**:
A floor snap target around or on the oil spill that accepts either a delimitation cone or an absorbent mat/sock.
_Avoid_: Drop spot, socket, snap area

**Sub-Station**:
One of the three physically separated, color-coded units within the electrical wall bay (Main Disconnect Switch, Lockout Station, Voltage Tester).
_Avoid_: Panel button, sub-panel

**Layout Pattern**:
One of the three randomized placements of movable safety equipment (PPE cabinet, spill kit, water extinguisher) and puddle footprint selected at mission start.
_Avoid_: Variant, room preset

### Safety Procedure

**PPE Slot**:
One of the four required personal protective equipment categories needed for hot-oil protection: Gloves (Heat Gloves), Body (Chemical Apron), Face (Face Shield), and Feet (Safety Boots).
_Avoid_: Equipment slot, gear category

**Distractor PPE**:
An unsuitable protective item in the PPE cabinet (Latex Gloves on the Gloves slot, Surgical Mask on the Face slot) that occupies a PPE Slot without conferring hot-oil protection.
_Avoid_: Fake PPE, decoy gear

**Residual Energy**:
Stored electrical or hydraulic hazard remaining in the spilled oil after the main switch is opened until it decays below threshold.
_Avoid_: Leftover power, live charge

**Lockout**:
The abstract lock applied to the electrical panel after opening the main switch to prevent re-energizing the press.
_Avoid_: Padlock, breaker lock

**Critical Event**:
A high-severity safety violation (live-oil shock, hot-oil burn, wrong-PPE burn, press contact, water on hot oil, unisolated try-start, or inaction) that penalizes Safety and Decision, fires damage feedback, and respawns the player in Guided mode.
_Avoid_: Fatal error, death, fail state
