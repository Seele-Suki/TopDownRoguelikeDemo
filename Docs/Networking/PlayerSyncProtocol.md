# Player Synchronization UDP Protocol

## Scope

This document defines the Phase 5 and Phase 6 UDP payload layouts for:

- `PlayerInput = 34`
- `PlayerStateSnapshot = 35`
- `PlayerShotgunEvent = 37`

- `WorldStateSnapshot = 40`

`WorldStateSnapshot` is a UDP message produced by the authoritative host
and forwarded by the server to joining clients.

It carries authoritative high-frequency state for Player, Enemy, Boss,
and ExperienceOrb entities when those records are present.

`PlayerStateSnapshot = 35` remains the dedicated player movement, aim, and
health message. `WorldStateSnapshot = 40` provides the host-authoritative
world entity state used to reconcile joining clients.

The payload layout is defined in the later Phase 6 protocol sections.
This message type does not replace `PlayerStateSnapshot = 35`; the player
snapshot remains the dedicated player movement, aim, and health message.

All offsets in the payload tables are relative to the first byte after
the existing 32-byte UDP message header.

## Encoding Rules

- Multi-byte integers use network byte order (big-endian).
- Floating-point values use IEEE 754 binary32.
- Float bits are transferred in network byte order.
- Native C++ struct memory must never be sent directly.
- Boolean values and compiler padding are not allowed.
- Flag fields may contain only documented bits; all reserved bits must be zero.
- All decoded floating-point values must be finite.

## Existing UDP Header

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 4 | Magic |
| 4 | 2 | Protocol version |
| 6 | 2 | Message type |
| 8 | 16 | Session token |
| 24 | 4 | Sender player ID |
| 28 | 4 | Packet sequence |
| 32 | variable | Message payload |

The UDP header player ID identifies the sender.

The UDP header sequence is the sequence number of the complete datagram.
Sequence zero is valid. Comparison uses the existing wrap-aware UDP
sequence ordering.

## Sequence Tracking Ownership

Each Unity client uses one monotonically increasing sequence counter for
all UDP datagrams that it originates. Sequence gaps are valid.

The C++ server validates client-originated UDP datagrams with the tracker
owned by that client's `TcpClientSession`.

When the server forwards a `PlayerInput` datagram, it preserves the
originating player ID and sequence number.

When the server forwards a `PlayerStateSnapshot` datagram, it preserves
the authoritative host player ID and sequence number.

Gameplay receivers keep sequence trackers separate by originating player
ID. A tracker must never be shared by unrelated player IDs.

During Phase 5:

- The host owns one input tracker for each remote player.
- A joining client owns a snapshot tracker for the authoritative host.
- UDP bind and ping response validation remains in the existing network
  handshake logic and does not use gameplay sequence trackers.
- Trackers are reset when the network gameplay session is destroyed.
- Duplicate, older, and ambiguous half-range sequences are rejected.
- Sequence wrap from `0xFFFFFFFF` to `0` is accepted.

## PlayerInput Payload

`PlayerInput` is sent by a client and forwarded to the authoritative host.

The UDP header player ID identifies the player whose input is described.
The payload does not repeat the player ID or sequence number.

Fixed payload size: 28 bytes.

| Payload offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | float32 | Move X |
| 4 | 4 | float32 | Move Y |
| 8 | 4 | float32 | Aim X |
| 12 | 4 | float32 | Aim Y |
| 16 | 4 | uint32 | Input flags |
| 20 | 4 | uint32 | Dash request sequence |
| 24 | 4 | uint32 | Shotgun request sequence |

Total datagram size: 60 bytes.

Movement rules:

- Move X and Move Y must be finite.
- Each component must be between `-1` and `1`.
- Squared movement magnitude must not exceed `1.0001`.
- `(0, 0)` means no movement.

Aim rules:

- Aim X and Aim Y must be finite.
- `(0, 0)` means no valid aim direction.
- A non-zero aim vector is normalized by the receiver before use.
- The payload contains direction, not mouse coordinates.

Input flag rules:

- Bit 0 is `FireHeld`.
- Bits 1 through 31 are reserved and must be zero.

Dash request sequence rules:

- The sequence starts at zero before any dash request.
- Each local dash press increments the sequence once.
- The latest value persists across later input packets.
- The server validates the UDP sender and forwards the value unchanged.
- Gameplay acceptance and cooldown validation belong to the authoritative host.

- The host keeps the first observed dash request sequence as its baseline.
- Duplicate, older, and ambiguous half-range dash request sequences do not
  replace the host's current dash request sequence.
- Rejecting a dash request sequence does not discard movement, aim, or fire
  state from the same accepted gameplay input packet.
- A request observed during cooldown is consumed and is not queued for later
  execution.
- Sequence wrap from `0xFFFFFFFF` to `0` is accepted.

Shotgun request sequence rules:

- The sequence starts at zero before any shotgun request.
- Each local shotgun press increments the sequence once.
- The latest value persists across later input packets.
- The server validates the UDP sender and forwards the value unchanged.
- Gameplay acceptance and cooldown validation belong to the authoritative host.
- Duplicate, older, and ambiguous half-range values must not replace the
  authoritative host's current shotgun request sequence.
- Sequence wrap from `0xFFFFFFFF` to `0` is accepted.

## PlayerStateSnapshot Payload

`PlayerStateSnapshot` is produced by the authoritative host and forwarded
to joining clients.

The UDP header player ID identifies the host that produced the snapshot.
Each state record contains the ID of the player it describes.

Snapshot prefix size: 4 bytes.

| Payload offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Player record count |

The record count must be between 1 and 4.

Each player record has a fixed size of 28 bytes.

| Record offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Player ID |
| 4 | 4 | float32 | Position X |
| 8 | 4 | float32 | Position Y |
| 12 | 4 | float32 | Aim X |
| 16 | 4 | float32 | Aim Y |
| 20 | 4 | uint32 | State flags |
| 24 | 2 | uint16 | Current health |
| 26 | 2 | uint16 | Maximum health |

Payload size formula:

`4 + player count * 28`

Total datagram size formula:

`36 + player count * 28`

Expected sizes:

| Players | Payload | Complete datagram |
| ---: | ---: | ---: |
| 1 | 32 bytes | 64 bytes |
| 2 | 60 bytes | 92 bytes |
| 4 | 116 bytes | 148 bytes |

Snapshot validation rules:

- Player IDs must be non-zero.
- Player IDs must be unique within one snapshot.
- Records are encoded in ascending player-ID order.
- Position and aim components must be finite.
- Non-zero aim vectors are normalized by the receiver.
- Payload size must exactly match the record count.
- Trailing or missing bytes are rejected.
- Bit 0 of state flags is `FireHeld`.
- Bit 1 of state flags is authoritative `IsDashing`.
- Bits 2 through 31 are reserved and must be zero.

Player state flags remain a 32-bit field. Bit 0 is `FireHeld`, bit 1 is
`IsDashing`, and bits 2 through 31 are reserved and must be zero.

The 24-byte Phase 5 player record is extended to 28 bytes by appending
two network-order uint16 health fields. The existing action flags are not
removed or repurposed.

`Maximum health` must be at least 1. `Current health` must be between zero
and maximum health. `Current health == 0` represents authoritative player
death.

Health values outside the uint16 range are invalid. A decoder must reject
zero maximum health and current health greater than maximum health.

Player health is authoritative on the host. Joining clients apply the
decoded health to the corresponding PlayerHealth component on the Unity
main thread. HealthBarView continues to observe PlayerHealth and does not
decode network bytes directly.

The player snapshot remains separate from `WorldStateSnapshot = 40`.
A world snapshot may also contain Player records so both players can be
reconciled from the host-authoritative world state.

## PlayerShotgunEvent Payload

`PlayerShotgunEvent` describes one complete authoritative shotgun volley.
It is produced by the authoritative host and forwarded to joining clients.

Fixed payload size: 36 bytes.

| Payload offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Player ID |
| 4 | 4 | uint32 | Volley sequence |
| 8 | 4 | float32 | Origin X |
| 12 | 4 | float32 | Origin Y |
| 16 | 4 | float32 | Center direction X |
| 20 | 4 | float32 | Center direction Y |
| 24 | 4 | uint32 | Projectile count |
| 28 | 4 | float32 | Spread angle |
| 32 | 4 | float32 | Effective cooldown |

Total datagram size: 68 bytes.

Validation rules:

- Player ID must be non-zero.
- Origin and direction components must be finite.
- Center direction must be non-zero.
- Projectile count must be between 1 and 32.
- Spread angle must be between 0 and 180 degrees.
- Effective cooldown must be finite and non-negative.
- Volley sequence is maintained independently for each player.
- One payload represents one complete volley.
- Damage and penetration are not included because they remain authoritative
  host Gameplay state.

## Authority Rules

- A joining client sends input, not position.
- The server validates identity and forwards input to the host.
- The host simulates both players.
- Only the host produces `PlayerStateSnapshot`.
- A joining client applies or interpolates host snapshots.
- The client must not treat its locally predicted position as authoritative.

## Phase 6 Entity ID Policy

All gameplay entities use a non-zero uint32 entity ID.

The ID uses the high four bits for the entity category and the low
28 bits for the category-local instance sequence.

| Entity category | High four bits | ID range |
| --- | ---: | --- |
| Player | 0x0 | 0x00000001 - 0x0FFFFFFF |
| Enemy | 0x1 | 0x10000001 - 0x1FFFFFFF |
| Boss | 0x2 | 0x20000001 - 0x2FFFFFFF |
| Experience orb | 0x3 | 0x30000001 - 0x3FFFFFFF |
| Reserved | 0x4 - 0xF | Not used in Phase 6 |

Entity ID `0x00000000` means unassigned or invalid.

Player entities reuse the server-assigned PlayerId. The server must keep
player IDs inside the 0x0xxxxxxx range.

Enemy, Boss, and ExperienceOrb instances receive a new category-specific
ID when they are created or activated for a match.

An entity ID remains stable for the entire lifetime of that entity within
the match. Destroyed entities must not release their ID for reuse during
the same match.

An ExperienceOrb that is returned to the object pool and activated again
must receive a new entity ID. Object pooling never reuses an old network
entity ID.

Entity ID allocation restarts only when a new network gameplay match is
created. A new match must reset all category-local sequences before the
first entity is spawned.

The NetworkEntityRegistry stores all active gameplay entities in one
global uint32 namespace. Entity category validation is performed from the
high four bits before an entity is registered.

## Phase 6 Battle Event Message Types

The following message types are reliable TCP gameplay events.

| Value | Message type | Direction | Purpose |
| ---: | --- | --- | --- |
| 41 | `WorldEntitySpawned` | Host to joining clients | Creates a replicated entity |
| 42 | `WorldEntityRemoved` | Host to joining clients | Removes a replicated entity |
| 43 | `PlayerDied` | Host to both clients | Confirms authoritative player death |
| 44 | `ExperienceOrbSpawned` | Host to joining clients | Creates a replicated experience orb |
| 45 | `ExperienceOrbCollected` | Host to joining clients | Removes a collected experience orb |
| 46 | `UpgradeStarted` | Host to both clients | Starts the synchronized upgrade pause |
| 47 | `UpgradeChoiceSubmitted` | Client to host | Submits one player's upgrade choice |
| 48 | `UpgradeCompleted` | Host to both clients | Applies choices and resumes gameplay |
| 49 | `BossPhaseChanged` | Host to joining clients | Announces an authoritative Boss phase |
| 50 | `GameResult` | Host to both clients | Announces authoritative victory or defeat |

`WorldStateSnapshot = 40` remains the UDP message for high-frequency
authoritative world state. The reliable events above carry lifecycle and
flow transitions that must not depend on a single UDP datagram.

Payload layouts, validation rules, and event ordering are defined in later
Phase 6 steps.

## Phase 6 Entity Types

| Value | Entity type | Required entity-ID prefix |
| ---: | --- | --- |
| 0 | Invalid | Not allowed |
| 1 | Player | 0x0 |
| 2 | Enemy | 0x1 |
| 3 | Boss | 0x2 |
| 4 | ExperienceOrb | 0x3 |

Entity type values and entity-ID prefixes are distinct fields. A decoder
must validate that they describe the same category.

## Phase 6 Enemy Archetypes

| Value | Enemy archetype |
| ---: | --- |
| 0 | Invalid |
| 1 | Basic |
| 2 | Fast |

Enemy archetype values are stable protocol identifiers, not array indexes.
Enemy records must use Basic or Fast. Player, Boss, and ExperienceOrb
records must use Invalid.

## WorldStateSnapshot Payload

`WorldStateSnapshot = 40` is an authoritative host-originated UDP message.

The payload begins with a 4-byte network-order uint32 entity count,
followed by 1 to 64 fixed-size entity records. An empty snapshot is
invalid.

Each record is 32 bytes.

| Record offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Entity ID |
| 4 | 1 | uint8 | Entity type |
| 5 | 1 | uint8 | Entity lifecycle |
| 6 | 2 | uint16 | Entity state flags |
| 8 | 4 | float32 | Position X |
| 12 | 4 | float32 | Position Y |
| 16 | 4 | float32 | Rotation degrees |
| 20 | 2 | uint16 | Current health |
| 22 | 2 | uint16 | Maximum health |
| 24 | 1 | uint8 | Boss phase |
| 25 | 1 | uint8 | Enemy archetype |
| 26 | 6 | uint8[6] | Reserved; must be zero |

Entity type values are:

| Value | Entity type |
| ---: | --- |
| 1 | Player |
| 2 | Enemy |
| 3 | Boss |
| 4 | ExperienceOrb |

Entity lifecycle values are:

| Value | Lifecycle |
| ---: | --- |
| 0 | Snapshot |
| 1 | Spawn |
| 2 | Update |
| 3 | Dead |
| 4 | Removed |

State flag bit 0 is `Active`. State flag bit 1 is `Dead`.
Bits 2 through 15 are reserved and must be zero.

For enemy archetype values:

- Enemy records must use Basic or Fast.
- Player, Boss, and ExperienceOrb records must use Invalid.
- Unknown enemy archetype values must be rejected.

For Player, Enemy, and Boss records:

- Maximum health must be at least 1.
- Current health must be between 0 and maximum health.
- `Current health == 0` means dead.
- The `Dead` flag must be set exactly when current health is zero.
- Boss phase must be between 1 and 2 for a Boss.
- Boss phase must be zero for Player and Enemy records.

For ExperienceOrb records:

- Current health must be zero.
- Maximum health must be zero.
- Boss phase must be zero.
- The `Dead` flag must not be set.

Records are encoded in ascending entity-ID order. Entity IDs must be
non-zero and unique.
Entity type, lifecycle, flags, health values, enemy archetype, and reserved bytes are validated by both the Unity and C++ decoders.
All floating-point values must be finite.

Payload size is:

`4 + entity count * 32`

Including the existing 32-byte UDP header, the maximum datagram size for
64 records is:

`32 + 4 + 64 * 32 = 2084 bytes`

## WorldEntitySpawned Payload

`WorldEntitySpawned = 41` carries one 32-byte world entity record using
the `WorldStateSnapshot` record layout defined above.

The record must use the `Spawn` lifecycle value. Entity type, health,
Boss phase, enemy archetype, state flags, and reserved bytes follow the
same validation rules as the world snapshot record. The entity ID must not already be
registered. A spawn record must not have the `Dead` flag set. Combat entities must
have current health between one and maximum health. ExperienceOrb records
must use zero health values.

## WorldEntityRemoved Payload

`WorldEntityRemoved = 42` uses an 8-byte payload.

| Offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Entity ID |
| 4 | 1 | uint8 | Entity type |
| 5 | 1 | uint8 | Removal reason |
| 6 | 2 | uint16 | Reserved |

Removal reasons are Invalid = 0, Died = 1, Cleared = 2, and Despawned = 3.
Reserved bytes must be zero.

Repeated removal messages must not award duplicate kills or experience.

## PlayerDied Payload

`PlayerDied = 43` uses a 4-byte network-order uint32 Player ID.

The ID must identify a player in the current room. The authoritative
player health must already be zero. Duplicate death events are ignored.

## ExperienceOrbSpawned Payload

`ExperienceOrbSpawned = 44` uses a 16-byte payload.

| Offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Entity ID |
| 4 | 4 | float32 | Position X |
| 8 | 4 | float32 | Position Y |
| 12 | 2 | uint16 | Experience amount |
| 14 | 2 | uint16 | Reserved |

The entity ID must use the ExperienceOrb range. Experience amount must be
greater than zero. Reserved bytes must be zero.

## ExperienceOrbCollected Payload

`ExperienceOrbCollected = 45` uses an 8-byte payload.

| Offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Experience-orb entity ID |
| 4 | 4 | uint32 | Collector player ID |

An orb may be collected only once. Returning a pooled orb removes its
registry entry and clears its old ID. A later activation receives a new
entity ID.

## Excluded From This Layout

This layout does not yet assign bits for:

- Damage
- Health
- Enemies
- Experience
- Upgrades
- Boss state

Unassigned state-flag bits remain zero until their semantics are explicitly
defined and tested in later phases.