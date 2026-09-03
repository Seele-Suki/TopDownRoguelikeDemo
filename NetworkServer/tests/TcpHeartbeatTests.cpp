#include "net/TcpClientSession.h"
#include "net/ServerCoordinator.h"
#include "net/SocketRuntime.h"
#include "net/TcpConnection.h"
#include "protocol/PacketCodec.h"
#include "room/RoomManager.h"
#include "room/PlayerIdAllocator.h"
#include "room/SessionTokenGenerator.h"

#include <chrono>
#include <iostream>
#include <stdexcept>
#include <WinSock2.h>

namespace { void Require(bool v, const char* m) { if (!v) throw std::runtime_error(m); } }

static void SendPacket(tdr::net::TcpClientSession& session,
                       tdr::protocol::MessageType type,
                       const std::vector<std::uint8_t>& payload)
{
    const auto encoded = tdr::protocol::PacketCodec::Encode(type, payload);
    session.ReceiveBytes(encoded.data(), encoded.size());
}

int main()
{
    try
    {
        tdr::room::PlayerIdAllocator ids;
        tdr::room::SessionTokenGenerator tokens;
        tdr::room::RoomManager rooms;
        const auto beforeConstruction = tdr::net::TcpClientSession::Clock::now();
        tdr::net::TcpClientSession session(ids, tokens, rooms);
        Require(session.LastActivity() >= beforeConstruction, "connection activity was not initialized");
        const auto base = tdr::net::TcpClientSession::TimePoint{};
        session.MarkActivity(base);
        Require(!session.IsTimedOut(base + std::chrono::seconds(5), std::chrono::seconds(6)), "timed out too early");
        Require(session.IsTimedOut(base + std::chrono::seconds(6), std::chrono::seconds(6)), "did not time out");

        const auto encoded = tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::TcpHeartbeatRequest, {});
        session.ReceiveBytes(encoded.data(), encoded.size());
        const auto outgoing = session.TakeOutgoingPackets();
        Require(outgoing.size() == 1U, "heartbeat response missing");
        tdr::protocol::PacketCodec responseCodec;
        responseCodec.Append(outgoing.front().data(), outgoing.front().size());
        const auto decodedPackets = responseCodec.DecodeAvailable();
        Require(decodedPackets.size() == 1U, "heartbeat response did not decode");
        Require(decodedPackets.front().type == tdr::protocol::MessageType::TcpHeartbeatResponse, "wrong heartbeat response");
        Require(decodedPackets.front().payload.empty(), "heartbeat response payload not empty");

        session.MarkActivity(base);
        const std::vector<std::uint8_t> invalidPayload{1U};
        const auto invalid = tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::TcpHeartbeatRequest, invalidPayload);
        session.ReceiveBytes(invalid.data(), invalid.size());
        Require(session.IsTimedOut(base + std::chrono::seconds(6), std::chrono::seconds(6)), "invalid heartbeat refreshed activity");

        const auto oldActivity = tdr::net::TcpClientSession::Clock::now() - std::chrono::seconds(1);
        session.MarkActivity(oldActivity);
        const std::vector<std::uint8_t> nickname{'A', 'l', 'i', 'c', 'e'};
        const auto ordinary = tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::SetNickname, nickname);
        session.ReceiveBytes(ordinary.data(), ordinary.size());
        Require(session.LastActivity() > oldActivity, "ordinary legal message did not refresh activity");

        tdr::net::SocketRuntime socketRuntime;
        tdr::net::ServerCoordinator coordinator;
        const SOCKET rawSocket = ::socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
        Require(rawSocket != INVALID_SOCKET, "could not create coordinator test socket");
        auto& attached = coordinator.AttachConnection(tdr::net::TcpConnection(rawSocket));
        attached.MarkActivity(tdr::net::TcpClientSession::Clock::now() - std::chrono::seconds(7));
        const auto removed = coordinator.RemoveTimedOutConnections(
            tdr::net::TcpClientSession::Clock::now(), std::chrono::seconds(6));
        Require(removed.size() == 1U && removed.front() == rawSocket, "timed out connection was not removed");
        Require(coordinator.ConnectionCount() == 0U && coordinator.SessionCount() == 0U, "timeout cleanup left coordinator state");
        coordinator.RemoveConnection(rawSocket);

        tdr::net::ServerCoordinator roomCoordinator;
        const SOCKET hostSocket = ::socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
        const SOCKET guestSocket = ::socket(AF_INET6, SOCK_STREAM, IPPROTO_TCP);
        Require(hostSocket != INVALID_SOCKET && guestSocket != INVALID_SOCKET, "could not create room sockets");
        auto& host = roomCoordinator.AttachConnection(tdr::net::TcpConnection(hostSocket));
        auto& guest = roomCoordinator.AttachConnection(tdr::net::TcpConnection(guestSocket));
        SendPacket(host, tdr::protocol::MessageType::SetNickname, {'H'});
        SendPacket(host, tdr::protocol::MessageType::CreateRoomRequest, {});
        const std::string roomId = host.CurrentRoom().Id();
        SendPacket(guest, tdr::protocol::MessageType::SetNickname, {'G'});
        SendPacket(guest, tdr::protocol::MessageType::JoinRoomRequest, {});
        Require(roomCoordinator.Rooms().ContainsRoom(roomId), "room was not created");
        roomCoordinator.RemoveConnection(hostSocket);
        Require(!roomCoordinator.Rooms().ContainsRoom(roomId), "host disconnect left room behind");
        Require(!guest.HasRoom(), "guest room mapping was not invalidated");
        roomCoordinator.RemoveConnection(hostSocket);
        roomCoordinator.RemoveConnection(guestSocket);
        Require(roomCoordinator.ConnectionCount() == 0U && roomCoordinator.SessionCount() == 0U, "room disconnect cleanup was not idempotent");
        std::cout << "TCP heartbeat tests passed.\n";
        return 0;
    }
    catch (const std::exception& e) { std::cerr << "[FAIL] " << e.what() << '\n'; return 1; }
}
