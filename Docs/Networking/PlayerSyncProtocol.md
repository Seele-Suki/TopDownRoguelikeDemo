# Player Synchronization UDP Protocol

## Scope

This document defines the Phase 5 UDP payload layout for:

- `PlayerInput = 34`
- `PlayerStateSnapshot = 35`
- `PlayerShotgunEvent = 37`

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

Each player record has a fixed size of 24 bytes.

| Record offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | uint32 | Player ID |
| 4 | 4 | float32 | Position X |
| 8 | 4 | float32 | Position Y |
| 12 | 4 | float32 | Aim X |
| 16 | 4 | float32 | Aim Y |
| 20 | 4 | uint32 | State flags |

Payload size formula:

`4 + player count * 24`

Total datagram size formula:

`36 + player count * 24`

Expected sizes:

| Players | Payload | Complete datagram |
| ---: | ---: | ---: |
| 1 | 28 bytes | 60 bytes |
| 2 | 52 bytes | 84 bytes |
| 4 | 100 bytes | 132 bytes |

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