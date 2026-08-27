# Player Synchronization UDP Protocol

## Scope

This document defines the Phase 5 UDP payload layout for:

- `PlayerInput = 34`
- `PlayerStateSnapshot = 35`

All offsets in the payload tables are relative to the first byte after
the existing 32-byte UDP message header.

## Encoding Rules

- Multi-byte integers use network byte order (big-endian).
- Floating-point values use IEEE 754 binary32.
- Float bits are transferred in network byte order.
- Native C++ struct memory must never be sent directly.
- Boolean values and compiler padding are not allowed.
- Reserved fields must be encoded as zero.
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

Fixed payload size: 20 bytes.

| Payload offset | Size | Type | Field |
| ---: | ---: | --- | --- |
| 0 | 4 | float32 | Move X |
| 4 | 4 | float32 | Move Y |
| 8 | 4 | float32 | Aim X |
| 12 | 4 | float32 | Aim Y |
| 16 | 4 | uint32 | Reserved |

Total datagram size: 52 bytes.

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

The reserved field is zero during movement-and-aim synchronization.
It may be assigned input-button flags in a later Phase 5 step without
changing the payload size.

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
| 20 | 4 | uint32 | Reserved |

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
- Reserved fields must be zero.

The reserved record field may be assigned authoritative action-state flags
in a later Phase 5 step without changing the record size.

## Authority Rules

- A joining client sends input, not position.
- The server validates identity and forwards input to the host.
- The host simulates both players.
- Only the host produces `PlayerStateSnapshot`.
- A joining client applies or interpolates host snapshots.
- The client must not treat its locally predicted position as authoritative.

## Excluded From This Layout

This layout does not yet assign bits for:

- Primary fire
- Dash
- Shotgun
- Damage
- Health
- Enemies
- Experience
- Upgrades
- Boss state

The two reserved fields remain zero until their semantics are explicitly
defined and tested in later Phase 5 steps.