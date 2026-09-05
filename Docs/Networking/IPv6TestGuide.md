# IPv6 Test Guide

## Address Rules

- Same-machine testing uses `::1`.
- Host display prefers a detected global IPv6 address.
- Do not advertise `fe80::/10` link-local addresses, `fc00::/7` ULA
  addresses, multicast `ff00::/8`, IPv4 addresses, or other loopback addresses
  as a public host address.
- If no global IPv6 exists, the host falls back to `::1`.
- The joining client enters the Host IPv6 address, never the default gateway.
- Display an IPv6 address with the port as `[IPv6]:7777`.

The server listener is IPv6 dual-stack, so the same configured port handles
the TCP and UDP channels. The client still uses the exact Host address shown
by the host.

## Test Matrix

### Same Machine (`::1`)

1. Start `NetworkServer.exe 7777`.
2. Run the Editor or a packaged client.
3. Create a host room using port `7777`.
4. Start a second Editor instance or packaged client and enter `::1`.
5. Join, prepare both players, and start the game.

### Editor and Packaged Client

Start the server once, run the Editor as host, and connect one packaged client
to the displayed IPv6 address. Repeat with the packaged client as host if the
server executable is configured for automatic startup.

### Two Packaged Clients

Build `GoldenSandCaravan.exe`, start the server, launch two clients, and use
the host's displayed `[IPv6]:7777` endpoint in the joining client's address
and port fields. Verify room state, player movement, combat, health bars,
experience, upgrades, Boss flow, and result screens.

### Public IPv6

Use the host machine's global IPv6 address. Confirm that the address is not
link-local or ULA, then allow TCP and UDP 7777 through Windows Firewall on the
host. The remote client enters only the host's global address and `7777`.

## Firewall and Port Checks

Run on the host:

```powershell
Get-NetTCPConnection -LocalPort 7777 -ErrorAction SilentlyContinue
Get-NetUDPEndpoint -LocalPort 7777 -ErrorAction SilentlyContinue
Test-NetConnection -ComputerName ::1 -Port 7777
```

If firewall rules are required, create narrowly scoped inbound rules for TCP
7777 and UDP 7777, then remove them after testing according to local policy.

## Troubleshooting

- **Invalid address**: enter a syntactically valid IPv6 address; do not enter
  brackets in an address-only field unless the UI explicitly requests the
  combined `[IPv6]:port` form.
- **Invalid port**: use an integer from 1 through 65535; the project default is
  7777.
- **Cannot connect**: confirm the server is running, the address is the Host's
  address, TCP/UDP 7777 is allowed, and no other process owns the port.
- **No public address**: use `::1` for same-machine tests; public IPv6 testing
  requires a routable global address and firewall permission.
- **Server closes**: expect the client to raise one disconnect state, close
  transports, clear remote objects, and show the appropriate dialog.
- **Client closes**: the server removes its room and UDP mapping. A host
  disconnect ends the joining client's room; a joining-client disconnect lets
  the host choose continue-single-player or exit.

## Expected Results

The client must remain responsive during a disconnect. No duplicate dialog,
stale player/enemy/Boss/experience object, background thread, socket, or
Unity-owned server process may remain after shutdown. Manual server processes
remain running because Unity does not own them.
