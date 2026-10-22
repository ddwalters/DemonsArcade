# Player locomotion and camera

The player uses the Humanoid model and locomotion controller in
`Assets/_Game/Features/Player/Prefabs/player.prefab`. `CharacterMotor` blends
idle, walking, running, and airborne motions. `CharacterRootMotion` sends the
animated root displacement through the collision controller. The gameplay camera
stays in third person.

## Speed tuning

The player's `CharacterMotor` has an **Animation Speed Multiplier** field, set to
**1.3**. This plays the player's animation 30% faster. Since grounded movement
uses root motion, it also makes the player travel faster with the animation. Set
it to **1.0** for the original rate. This is the main place to tune the current
player's pace; `airSpeed` separately controls horizontal steering while airborne.

**Movement Blend Time** and **Stopping Blend Time** control responsiveness rather
than top speed. Both are now **0.08** seconds of smoothing, down from 0.25 and
0.14. Lower values respond faster. Free exploration uses the same forward walk/run
gait for every movement direction. The motor directs its root travel along the
camera-relative input, so diagonals keep the same pace while the character turns.

## Camera and controls

The camera orbits freely with mouse/right-stick input and eases back from geometry.
Its yaw is independent of the player's facing. Movement is relative to the camera's
horizontal direction; the character turns toward travel, including when moving
toward the camera. Sprint works in any movement direction. **PlayerController →
Turn Speed** controls how quickly the character turns (default 720 degrees/second).
The Options screen has one camera FOV slider. F5 and the former camera-view option
no longer switch perspective.

- WASD: move relative to the camera and turn toward travel
- Mouse/right stick: orbit the camera independently
- Shift: sprint
- Space: jump
- Tab: inventory
- Escape: pause

## Combat scope

The recent combat prototype, attack controller, custom weapon grip placements,
and first-person view model were removed. Inventory can still store equipped
weapons, but the player no longer spawns a held weapon model or has an attack
handler. Combat presentation can be redesigned after the locomotion and
animation changes are reviewed.

The Town blackout was caused by the saved full-screen RenderTexture overlay being
active. That overlay remains saved inactive in the Game UI prefab.
