#include "net/PlayerShotgunEventForwarder.h"
#include "net/ServerCoordinator.h"

#include "protocol/MessageType.h"
#include "protocol/PacketCodec.h"
#include "protocol/PlayerShotgunEventCodec.h"
#include "protocol/UdpPacketCodec.h"

#include <WinSock2.h>

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace
{
    void SendTcpPacket(
        tdr::net::TcpClientSession& session,
        const tdr::protocol::MessageType type,
        const std::vector<std::uint8_t>& payload)
    {
        const auto packet =
            tdr::protocol::PacketCodec::Encode(type, payload);

        session.ReceiveBytes(packet.data(), packet.size());
        static_cast<void>(session.TakeOutgoingPackets());
    }

    sockaddr_in6 CreateAddress(
        const std::uint16_t port)
    {
        sockaddr_in6 address{};

        address.sin6_family = AF_INET6;
        address.sin6_addr = in6addr_loopback;
        address.sin6_port = ::htons(port);

        return address;
    }

    std::vector<std::uint8_t> Bytes(
        const std::string& value)
    {
        return {
            value.begin(),
            value.end()
        };
    }

    std::vector<std::uint8_t> CreateShotgunPayload(
        const std::uint32_t playerId)
    {
        tdr::protocol::PlayerShotgunEvent event{};

        event.playerId = playerId;
        event.volleySequence = 9U;
        event.originX = 3.0F;
        event.originY = -2.0F;
        event.centerDirectionX = 0.6F;
        event.centerDirectionY = 0.8F;
        event.projectileCount = 5U;
        event.spreadAngle = 40.0F;
        event.effectiveCooldown = 0.75F;

        return tdr::protocol::PlayerShotgunEventCodec::Encode(event);
    }

    void ConfigureStartedRoom(
        tdr::net::ServerCoordinator& coordinator,
        tdr::net::TcpClientSession& host,
        tdr::net::TcpClientSession& guest)
    {
        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetNickname,
            Bytes("Host"));

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::CreateRoomRequest,
            {});

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetNickname,
            Bytes("Guest"));

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::JoinRoomRequest,
            {});

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Normal)
            });

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Melee),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::None)
            });

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetReady,
            { 1U });

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetReady,
            { 1U });

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::StartGameRequest,
            {});

        host.BindUdpEndpoint(CreateAddress(46101U));
        guest.BindUdpEndpoint(CreateAddress(46102U));
    }

    std::vector<std::uint8_t> CreateDatagram(
        const tdr::protocol::MessageType type,
        const std::array<
        std::uint8_t,
        tdr::protocol::kUdpSessionTokenSize>& token,
        const std::uint32_t packetPlayerId,
        const std::uint32_t sequence,
        const std::vector<std::uint8_t>& payload)
    {
        tdr::protocol::UdpMessageHeader header{};

        header.type = type;
        header.sessionToken = token;
        header.playerId = packetPlayerId;
        header.sequence = sequence;

        return tdr::protocol::UdpPacketCodec::Encode(
            header,
            payload);
    }

    bool ExpectRejected(
        tdr::net::PlayerShotgunEventForwarder& forwarder,
        const std::vector<std::uint8_t>& datagram,
        const sockaddr_in6& source,
        const char* description)
    {
        try
        {
            static_cast<void>(
                forwarder.Forward(
                    datagram.data(),
                    datagram.size(),
                    source));
        }
        catch (const std::exception&)
        {
            return true;
        }

        std::cerr
            << "[FAIL] Accepted "
            << description
            << "."
            << std::endl;

        return false;
    }
}

int main()
{
    try
    {
        tdr::net::ServerCoordinator coordinator;

        auto& host = coordinator.CreateSession();
        auto& guest = coordinator.CreateSession();

        ConfigureStartedRoom(
            coordinator,
            host,
            guest);

        tdr::net::PlayerShotgunEventForwarder forwarder(
            coordinator);

        const auto payload =
            CreateShotgunPayload(host.PlayerId());

        const auto request =
            CreateDatagram(
                tdr::protocol::MessageType::PlayerShotgunEvent,
                host.SessionTokenBytes(),
                host.PlayerId(),
                100U,
                payload);

        const auto forwarded =
            forwarder.Forward(
                request.data(),
                request.size(),
                host.UdpEndpointAddress());

        const auto decoded =
            tdr::protocol::UdpPacketCodec::Decode(
                forwarded.bytes.data(),
                forwarded.bytes.size());

        if (!guest.MatchesUdpEndpoint(
            forwarded.destination))
        {
            throw std::runtime_error(
                "Destination was not the guest.");
        }

        if (decoded.header.type !=
            tdr::protocol::MessageType::PlayerShotgunEvent)
        {
            throw std::runtime_error(
                "Forwarded type did not match.");
        }

        if (decoded.header.playerId != host.PlayerId())
        {
            throw std::runtime_error(
                "Forwarded player ID did not preserve host identity.");
        }

        if (decoded.header.sequence != 100U)
        {
            throw std::runtime_error(
                "Forwarded packet sequence changed.");
        }

        if (decoded.header.sessionToken !=
            guest.SessionTokenBytes())
        {
            throw std::runtime_error(
                "Forwarded token was not the guest token.");
        }

        if (decoded.payload != payload)
        {
            throw std::runtime_error(
                "Forwarded payload changed.");
        }

        auto guestHeaderRequest =
            CreateDatagram(
                tdr::protocol::MessageType::PlayerShotgunEvent,
                guest.SessionTokenBytes(),
                guest.PlayerId(),
                101U,
                CreateShotgunPayload(guest.PlayerId()));

        if (!ExpectRejected(
            forwarder,
            guestHeaderRequest,
            guest.UdpEndpointAddress(),
            "a non-host authority event"))
        {
            return 1;
        }

        auto forgedPayload =
            CreateShotgunPayload(
                guest.PlayerId() + 1000U);

        const auto forgedHeaderRequest =
            CreateDatagram(
                tdr::protocol::MessageType::PlayerShotgunEvent,
                host.SessionTokenBytes(),
                host.PlayerId(),
                102U,
                forgedPayload);

        if (!ExpectRejected(
            forwarder,
            forgedHeaderRequest,
            host.UdpEndpointAddress(),
            "a payload player outside the current room"))
        {
            return 1;
        }

        auto invalidPayload = payload;
        invalidPayload.pop_back();

        const auto invalidLengthRequest =
            CreateDatagram(
                tdr::protocol::MessageType::PlayerShotgunEvent,
                host.SessionTokenBytes(),
                host.PlayerId(),
                103U,
                invalidPayload);

        if (!ExpectRejected(
            forwarder,
            invalidLengthRequest,
            host.UdpEndpointAddress(),
            "an invalid payload length"))
        {
            return 1;
        }

        const auto wrongTypeRequest =
            CreateDatagram(
                tdr::protocol::MessageType::PlayerShotEvent,
                host.SessionTokenBytes(),
                host.PlayerId(),
                104U,
                payload);

        if (!ExpectRejected(
            forwarder,
            wrongTypeRequest,
            host.UdpEndpointAddress(),
            "a wrong message type"))
        {
            return 1;
        }

        std::cout
            << "[PASS] Host PlayerShotgunEvent forwarding "
            << "identity and validation checks passed."
            << std::endl;

        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr
            << "[FAIL] "
            << exception.what()
            << std::endl;

        return 1;
    }
}