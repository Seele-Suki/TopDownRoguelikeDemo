# Golden Sand Caravan Network Protocol

All integer fields are encoded in network byte order (big endian). Floating
point values use the IEEE-754 32-bit bit pattern, with that pattern written as
a network-order `uint32`.

## TCP Packet

Every TCP packet has a 12-byte header followed by its payload:

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 4 | Magic `0x54445231` (`TDR1`) |
| 4 | 2 | Protocol version `1` |
| 6 | 2 | `MessageType` (`uint16`) |
| 8 | 4 | Payload size (`uint32`) |

Maximum payload is 65,536 bytes. TCP is a byte stream: receivers buffer data,
reject invalid headers/lengths, and emit a packet only after all header and
payload bytes are present. This handles both half-packets and multiple packets
in one read.

## UDP Packet

The UDP header is 32 bytes:

| Offset | Size | Field |
| ---: | ---: | --- |
| 0 | 4 | Magic `0x54445255` (`TDRU`) |
| 4 | 2 | Protocol version `1` |
| 6 | 2 | `MessageType` |
| 8 | 16 | Session token |
| 24 | 4 | Player ID |
| 28 | 4 | Sequence number |

UDP packets are authenticated by the session token and player ID. The server
binds a UDP endpoint only after the TCP-issued credentials are presented.

## Message Types

| Value | Name | Channel |
| ---: | --- | --- |
| 1 | ClientHello | TCP |
| 2 | ServerHello | TCP |
| 3 | SetNickname | TCP |
| 10-20 | Room requests, responses, snapshots, start, leave, errors | TCP |
| 21 | TcpHeartbeatRequest | TCP |
| 22 | TcpHeartbeatResponse | TCP |
| 30-33 | UDP bind, ping and pong | UDP |
| 34 | PlayerInput | UDP |
| 35 | PlayerStateSnapshot | UDP |
| 36 | PlayerShotEvent | UDP |
| 37 | PlayerShotgunEvent | UDP |
| 40 | WorldStateSnapshot | UDP |
| 41 | WorldEntitySpawned | TCP/forwarded event |
| 42 | WorldEntityRemoved | TCP/forwarded event |
| 43 | PlayerDied | TCP |
| 44 | ExperienceOrbSpawned | TCP/forwarded event |
| 45 | ExperienceOrbCollected | TCP/forwarded event |
| 46-48 | UpgradeStarted, UpgradeChoiceSubmitted, UpgradeCompleted | TCP |
| 49 | BossPhaseChanged | TCP |
| 50 | GameResult | TCP |
| 51 | SharedExperienceSnapshot | UDP/forwarded state |
| 52 | BossCombatState | UDP/forwarded state |

## Gameplay Payloads

`PlayerInput` is 28 bytes: `moveX`, `moveY`, `aimX`, `aimY` floats, flags,
dash request sequence, and shotgun request sequence. `PlayerStateSnapshot`
starts with a 4-byte player count and contains up to four sorted 28-byte
records. Each record is:

```text
uint32 playerId
float  positionX, positionY, aimX, aimY
uint32 flags (bit 0 fire-held, bit 1 dashing)
uint16 currentHealth
uint16 maxHealth
```

The health invariant is `maxHealth >= 1` and
`0 <= currentHealth <= maxHealth`; zero current health means dead.

`WorldStateSnapshot` starts with a 4-byte entity count and contains up to 64
48-byte records. A record contains entity ID/type/lifecycle/flags, position,
rotation, current/max health, Boss phase, enemy archetype, experience amount,
direction, projectile speed/damage, and projectile sequence. Entity types are
Player, Enemy, Boss, ExperienceOrb, and BossProjectile.

## Sequence and Heartbeat Rules

UDP sequence windows discard duplicate and older packets. Later snapshots are
authoritative, so a snapshot received after a lost health update corrects the
display automatically. TCP heartbeat requests are sent every 2 seconds; the
timeout is 6 seconds. Heartbeat and transport failures become main-thread
disconnect events.

## Leave and Error Handling

`LeaveRoom` has an empty payload. `ErrorMessage` carries strict UTF-8 text.
`PlayerDied` and `GameResult` are reliable critical-flow messages; final result
handling does not depend on one UDP packet. Disconnect cleanup removes the room
mapping, UDP binding, player/world objects, subscriptions, sockets, threads,
and only the server process owned by the Unity client.

## Byte Layout Example

For a player record with ID `1`, health `7/10`, and zero vectors, the final
eight bytes are `00 07 00 0A`. C++ `PlayerStateSnapshotCodec` and C#
`PlayerStateSnapshotCodec` both write the same 4-byte count, 28-byte records,
network-order integers, and float bit patterns.
