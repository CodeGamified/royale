# Royale — Battle Royale Coding Game

**Code your way to the last survivor standing.**

Top-down 2D battle royale inspired by surviv.io. Drop onto a shrinking island,
loot weapons and scopes from crates, take cover behind buildings and obstacles, and
eliminate opponents — all by writing code. Your script controls movement, aiming,
shooting, looting, and zone awareness. Finding better scopes widens your view
radius — a massive tactical advantage. The last bot alive wins.

---

## Game Design

### Arena
- **Map**: 200×200 unit flat 2D arena viewed top-down (XZ plane, camera on Y axis)
- **Zone**: Circular safe zone starts covering the full map, shrinks in phases
  every 30s game-time. Damage ramps per phase (1→2→4→8 HP/s outside zone)
- **Buildings**: ~20 simple rectangular structures (4 walls, 1-2 doorways) scattered
  across the map. Block movement and line-of-sight
- **Obstacles**: ~60 destructible crates (wood, 30 HP), ~40 indestructible rocks,
  ~30 trees (indestructible, partial cover)
- **Loot crates**: ~80 glowing crates spawn at match start containing weapons,
  ammo, armor vests, scopes, and healing items. Break open by walking over them

### Players
- **Count**: 1 code-controlled player + 15 AI bots = 16 total
- **Health**: 100 HP base + up to 50 armor (absorbs 50% damage while active)
- **Movement speed**: 5 units/s base, 3.5 units/s while shooting
- **Collision radius**: 0.5 units
- **Inventory**: 2 weapon slots, 1 heal slot (bandage=30HP, medkit=full), 1 scope slot
- **Scope**: Determines camera zoom / view radius. Upgraded by looting better scopes

### Scopes

| Scope | View Radius | Camera Size | Rarity |
|-------|-------------|-------------|--------|
| None (default) | 30 | 30 | start |
| 2x | 40 | 22.5 | common |
| 4x | 55 | 16 | uncommon |
| 8x | 75 | 11 | rare |
| 15x | 100 | 7.5 | legendary |

Scopes affect two things:
1. **Camera zoom** — orthographic size shrinks, showing more of the map
2. **Enemy detection range** — `get_enemy_dist()` can see further with better scopes

Scopes auto-upgrade on pickup (can't downgrade). The scope level is readable via `get_scope()`.

### Weapons

| Weapon | Damage | Fire Rate | Range | Mag | Reload | Spread | Rarity |
|--------|--------|-----------|-------|-----|--------|--------|--------|
| Fists | 8 | 2/s | 1.5 | ∞ | — | 0° | start |
| Pistol (M9) | 12 | 6/s | 60 | 15 | 1.5s | 4° | common |
| Shotgun (MP220) | 9×8 | 1.5/s | 20 | 2 | 2.5s | 12° | uncommon |
| SMG (MAC-10) | 9 | 14/s | 40 | 32 | 1.8s | 7° | uncommon |
| Assault Rifle (M4) | 14 | 10/s | 100 | 30 | 2.2s | 3° | rare |
| Sniper (Mosin) | 68 | 0.9/s | 200 | 5 | 3.0s | 0.5° | rare |

### Loot Table
- **Common crate** (60%): Pistol, 30 ammo, bandage, 30% chance of 2x scope
- **Uncommon crate** (25%): Shotgun or SMG, 20 ammo, bandage, level-1 armor, 4x scope
- **Rare crate** (12%): Assault Rifle, 60 ammo, medkit, level-2 armor, 8x scope
- **Legendary crate** (3%): Sniper, 10 ammo, medkit, level-2 armor, 15x scope

### Zone Phases

| Phase | Delay | Shrink Time | End Radius | Outside DPS |
|-------|-------|-------------|------------|-------------|
| 1 | 30s | 20s | 80 | 1 |
| 2 | 25s | 18s | 50 | 2 |
| 3 | 20s | 15s | 25 | 4 |
| 4 | 15s | 12s | 5 | 8 |
| 5 | 10s | 10s | 0 | 16 |

### Match Flow
1. All 16 players spawn at random positions across the map
2. 3s invulnerability grace period for looting
3. Zone begins shrinking after Phase 1 delay
4. Match ends when 1 player remains (Victory) or all eliminated
5. Auto-restart after 5s (configurable)

---

## Folder Structure

```
royale/Royale/Assets/
├── Engine/                ← .engine submodule (do not edit)
├── Core/
│   ├── RoyaleBootstrap.cs
│   └── RoyaleSimulationTime.cs
├── Game/
│   ├── RoyaleArena.cs          ← map grid, buildings, obstacles, zone
│   ├── RoyalePlayer.cs         ← position, health, armor, inventory, facing
│   ├── RoyaleCrate.cs          ← loot crate entity
│   ├── RoyaleWeapon.cs         ← weapon stats, firing, reload
│   ├── RoyaleProjectile.cs     ← bullet travel + hit detection
│   ├── RoyaleZone.cs           ← shrinking circle logic + damage
│   ├── RoyaleMatchManager.cs   ← spawning, scoring, elimination, victory
│   └── RoyaleRenderer.cs       ← top-down visual (players, buildings, zone ring)
├── Scripting/
│   ├── RoyaleOpCode.cs
│   ├── RoyaleCompilerExtension.cs
│   ├── RoyaleIOHandler.cs
│   ├── RoyaleProgram.cs
│   ├── RoyaleEditorExtension.cs
│   └── RoyaleInputProvider.cs
└── AI/
    └── RoyaleAIController.cs   ← bot behavior (loot→fight→zone)
```

---

## Opcode Surface

### Enum

```csharp
public enum RoyaleOpCode
{
    // === Queries (read → R0) ===
    GET_X              = 0,   // Player X position
    GET_Y              = 1,   // Player Y position (Z in world space)
    GET_HEALTH         = 2,   // Current HP (0-100)
    GET_ARMOR          = 3,   // Current armor points (0-50)
    GET_ALIVE_COUNT    = 4,   // Players still alive
    GET_KILL_COUNT     = 5,   // Player's elimination count

    // Zone
    GET_ZONE_X         = 6,   // Safe zone center X
    GET_ZONE_Y         = 7,   // Safe zone center Y
    GET_ZONE_RADIUS    = 8,   // Current safe zone radius
    GET_ZONE_SHRINKING = 9,   // 1 if zone is actively shrinking, 0 if paused
    GET_IN_ZONE        = 10,  // 1 if player inside safe zone, 0 if outside

    // Weapon
    GET_WEAPON         = 11,  // Current weapon type (0=fists,1=pistol,2=shotgun,3=smg,4=ar,5=sniper)
    GET_AMMO           = 12,  // Ammo in current weapon magazine
    GET_AMMO_RESERVE   = 13,  // Reserve ammo for current weapon
    GET_RELOADING      = 14,  // 1 if currently reloading, 0 otherwise
    GET_WEAPON_RANGE   = 15,  // Effective range of current weapon

    // Nearest enemy (line-of-sight only)
    GET_ENEMY_DIST     = 16,  // Distance to nearest visible enemy (-1 if none)
    GET_ENEMY_ANGLE    = 17,  // Angle to nearest visible enemy (degrees, 0=right, CCW)
    GET_ENEMY_HEALTH   = 18,  // Approx health of nearest visible enemy

    // Nearest loot
    GET_CRATE_DIST     = 19,  // Distance to nearest unopened crate (-1 if none)
    GET_CRATE_ANGLE    = 20,  // Angle to nearest crate

    // Nearest cover
    GET_COVER_DIST     = 21,  // Distance to nearest cover (building/rock)
    GET_COVER_ANGLE    = 22,  // Angle to nearest cover

    // Inventory
    GET_SLOT_WEAPON    = 23,  // Weapon type in slot R0 (0 or 1)
    GET_HAS_HEAL       = 24,  // 1 if player has a healing item, 0 otherwise
    GET_FACING         = 25,  // Current aim angle in degrees
    GET_SCOPE          = 26,  // Current scope level (0=none,1=2x,2=4x,3=8x,4=15x)
    GET_VIEW_RANGE     = 27,  // Current view/detection radius (30-100 based on scope)

    // Input (manual play)
    GET_INPUT          = 28,  // Raw manual input value

    // === Commands (act → R0 = 1 success / 0 fail) ===
    MOVE_TOWARD        = 29,  // Move in direction R0 (angle degrees), full speed
    SET_FACING         = 30,  // Aim at angle R0 (degrees)
    SHOOT              = 31,  // Fire current weapon
    RELOAD             = 32,  // Start reload
    LOOT               = 33,  // Pick up nearest crate/item within 2 units
    SWAP_WEAPON        = 34,  // Switch to other weapon slot
    USE_HEAL           = 35,  // Use healing item (must stand still 3s)
    MOVE_TO_ZONE       = 36,  // Convenience: move toward zone center
}
```

29 queries + 8 commands = **37 opcodes** (fits in CUSTOM_0..CUSTOM_31 + 5 overflow).

### Builtin Functions

| Function | Args | Returns (R0) | Description |
|----------|------|-------------|-------------|
| `get_x()` | 0 | float | Player X |
| `get_y()` | 0 | float | Player Y |
| `get_health()` | 0 | 0-100 | Current HP |
| `get_armor()` | 0 | 0-50 | Current armor |
| `get_alive()` | 0 | int | Alive player count |
| `get_kills()` | 0 | int | Kill count |
| `get_zone_x()` | 0 | float | Zone center X |
| `get_zone_y()` | 0 | float | Zone center Y |
| `get_zone_radius()` | 0 | float | Zone radius |
| `get_zone_shrinking()` | 0 | 0/1 | Is zone shrinking? |
| `get_in_zone()` | 0 | 0/1 | Inside safe zone? |
| `get_weapon()` | 0 | 0-5 | Current weapon type |
| `get_ammo()` | 0 | int | Magazine ammo |
| `get_ammo_reserve()` | 0 | int | Reserve ammo |
| `get_reloading()` | 0 | 0/1 | Is reloading? |
| `get_weapon_range()` | 0 | float | Weapon range |
| `get_enemy_dist()` | 0 | float/-1 | Nearest enemy distance |
| `get_enemy_angle()` | 0 | degrees | Angle to nearest enemy |
| `get_enemy_health()` | 0 | 0-100 | Nearest enemy HP (approx) |
| `get_crate_dist()` | 0 | float/-1 | Nearest crate distance |
| `get_crate_angle()` | 0 | degrees | Angle to nearest crate |
| `get_cover_dist()` | 0 | float/-1 | Nearest cover distance |
| `get_cover_angle()` | 0 | degrees | Angle to nearest cover |
| `get_slot_weapon(slot)` | 1 | 0-5 | Weapon in slot |
| `get_has_heal()` | 0 | 0/1 | Has healing item? |
| `get_facing()` | 0 | degrees | Current aim angle |
| `get_scope()` | 0 | 0-4 | Scope level (0=none,1=2x,2=4x,3=8x,4=15x) |
| `get_view_range()` | 0 | 30-100 | Current view/detection radius |
| `get_input()` | 0 | float | Manual input |
| `move_toward(angle)` | 1 | 1/0 | Move in direction |
| `set_facing(angle)` | 1 | 1 | Set aim angle |
| `shoot()` | 0 | 1/0 | Fire weapon |
| `reload()` | 0 | 1/0 | Start reload |
| `loot()` | 0 | 1/0 | Pick up nearby item |
| `swap_weapon()` | 0 | 1 | Switch weapon slot |
| `use_heal()` | 0 | 1/0 | Use heal item |
| `move_to_zone()` | 0 | 1/0 | Move toward zone center |

---

## Default Starter Code

```python
# Royale — survive the shrinking zone
# Loot weapons, fight enemies, stay in the zone!

while True:
    hp = get_health()
    
    # Heal if low and have item
    if hp < 40:
        if get_has_heal() == 1:
            use_heal()
    
    # Stay inside the zone
    if get_in_zone() == 0:
        move_to_zone()
        wait
    
    # Got a weapon? Look for fights
    if get_weapon() > 0:
        dist = get_enemy_dist()
        if dist > 0:
            # Enemy visible — aim and shoot
            angle = get_enemy_angle()
            set_facing(angle)
            if dist < get_weapon_range():
                shoot()
                if get_ammo() == 0:
                    reload()
            else:
                move_toward(angle)
            wait
    
    # No enemies — go loot (scopes + weapons)
    cdist = get_crate_dist()
    if cdist > 0:
        move_toward(get_crate_angle())
        if cdist < 2:
            loot()
    else:
        # Nothing to do, drift toward zone center
        move_to_zone()
    
    wait

# Tip: better scopes = see enemies first = huge advantage.
# Check get_scope() — prioritize looting until you have at least 4x.
```

---

## Visual Style

Top-down camera looking straight down (Y axis), surviv.io aesthetic:

- **Background**: Dark olive-green ground (`0.12, 0.15, 0.08`) with subtle noise
- **Players**: Colored circles (radius 0.5) with a small aim-direction line
  - Player bot: bright green (`0.2, 1.0, 0.3`)
  - AI bots: orange/red spectrum
  - Eliminated: grey, fade out over 1s
- **Buildings**: Dark grey rectangles with lighter doorway gaps, slightly elevated
- **Obstacles**: Brown cubes (crates), grey spheres (rocks), dark green cones (trees)
- **Loot crates**: Glowing gold cubes, pulse animation, burst particles on open
- **Scopes on ground**: Small cyan lens icon, brightness increases with tier
- **Weapons on ground**: Small colored rectangles matching rarity tier
- **Zone**: Semi-transparent red ring at the boundary, fills outside with red tint
  that intensifies as zone shrinks. White ring for next zone preview
- **Bullets**: Tiny bright-yellow elongated quads traveling along trajectory
- **HUD overlay** (3D world-space): Kill feed text at top, alive count, minimap circle

### Color Palette

| Element | Color (RGB) |
|---------|-------------|
| Ground | `(0.12, 0.15, 0.08)` |
| Player | `(0.2, 1.0, 0.3)` |
| Enemy | `(1.0, 0.4, 0.2)` |
| Building wall | `(0.25, 0.25, 0.28)` |
| Building floor | `(0.18, 0.18, 0.20)` |
| Obstacle rock | `(0.4, 0.4, 0.38)` |
| Obstacle tree | `(0.1, 0.35, 0.1)` |
| Loot crate | `(1.0, 0.85, 0.2)` |
| Zone safe | `(0.2, 0.6, 1.0)` — blue ring |
| Zone danger | `(1.0, 0.1, 0.1, 0.3)` — red tint |
| Scope pickup | `(0.3, 0.9, 1.0)` |
| Bullet | `(1.0, 0.95, 0.4)` |
| Camera BG | `(0.08, 0.10, 0.05)` |

---

## Camera Setup

```
- Orthographic: true
- Base size: 30 (no scope — shows ~60×60 unit view)
- Scope overrides size: 2x→22.5, 4x→16, 8x→11, 15x→7.5
- Effective size: min(scopeSize, aliveZoom) — scope sets base, late-game zoom can tighten further
- Position: player.X, 50, player.Y
- Rotation: looking straight down (-Y)
- Follow: smooth lerp to player position, 8 units/s
- Late-game zoom: scale toward 15 as alive count drops below 8 (tighter final circles)
- Scope transition: smooth lerp over 0.5s when scope upgrades (satisfying zoom-out pop)
```

---

## AI Bot Behavior (15 bots)

Simple state machine per bot:

1. **Loot** (first 20s): Move to nearest crate, pick up everything (weapons, scopes, heals)
2. **Zone** (always): If outside zone or zone about to shrink, move toward center
3. **Fight**: If enemy visible within view range and weapon range, aim + shoot. Strafe randomly
4. **Heal**: If HP < 30 and have heal and no enemy nearby, use heal
5. **Wander**: Random drift toward zone center with noise

Difficulty tiers: 5 easy (poor aim, slow react), 5 medium, 5 hard (snappy aim, good positioning).

---

## Physics / Collision

- **2D top-down** on XZ plane (Y is up/camera axis)
- **Circle-circle** for player-player and player-obstacle collisions
- **Circle-AABB** for player-building collisions
- **Raycast** for bullet hit detection (instant for hitscan, or fast projectile travel)
- **Line-of-sight**: Raycast from player to target, blocked by buildings and rocks
  (trees provide partial cover: 50% chance to block LOS)
- **View range**: Enemy detection capped by scope level (30/40/55/75/100 units).
  Enemies beyond view range are invisible to queries even if geometrically in LOS
- **Zone damage**: Simple distance check from player to zone center vs radius

---

## Match Scoring

```
Placement points: 1st=25, 2nd=15, 3rd=10, 4-8th=5, 9-16th=1
Kill points: 3 per elimination
Damage points: 1 per 50 damage dealt
Survival points: 1 per 30s survived
```

---

## Bootstrap Wiring

```csharp
void Start()
{
    SettingsBridge.Load();
    QualityBridge.SetTier((QualityTier)SettingsBridge.QualityLevel);

    EnsureSimulationTime<RoyaleSimulationTime>();
    SetupCamera();           // orthographic, top-down, follow player
    CreateArena();           // RoyaleArena — map, buildings, obstacles, crates
    CreateZone();            // RoyaleZone — shrinking circle
    CreatePlayers();         // 1 RoyalePlayer (code) + 15 AI bots
    CreateMatchManager();    // RoyaleMatchManager — spawns, eliminations, victory
    CreateRenderer();        // RoyaleRenderer — top-down visuals
    CreateInputProvider();   // RoyaleInputProvider — WASD/arrow + mouse aim
    CreatePlayerProgram();   // RoyaleProgram — bytecode execution

    if (enableAI) CreateAIControllers();
    WireEvents();
    StartCoroutine(RunBootSequence());
}
```

---

## Key Design Decisions

1. **Angles, not coordinates for commands**: `move_toward(angle)` not `move_to(x,y)` —
   forces the player to compute navigation, which is the fun part
2. **Line-of-sight gating**: Enemy queries only return visible enemies, so players
   must write scouting/rotation logic
3. **Limited inventory**: 2 weapon slots + 1 heal + 1 scope forces trade-off decisions in code
4. **Scopes as progression**: View radius is the core loot progression. A 15x scope
   player sees 100 units vs 30 for a naked drop — huge intel advantage. Scopes
   auto-upgrade so code must factor current view range into engagement decisions
5. **Deterministic zone**: Zone positions and timings are fixed per match seed,
   so smart code can pre-calculate optimal rotations
6. **Fog of war**: No global map awareness — player view radius depends on scope
   (30 base → 100 with 15x). Enemies only detected when in LOS *and* within view
   range. Makes looting scopes and writing scouting logic both valuable
7. **Simple buildings**: Axis-aligned rectangles with doorways. Easy collision math,
   but creates tactical cover decisions for the coder