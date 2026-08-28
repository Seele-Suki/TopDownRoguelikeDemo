#include "net/PlayerShotEventForwarder.h"
#include "net/ServerCoordinator.h"

#include "protocol/MessageType.h"
#include "protocol/PacketCodec.h"
#include "protocol/PlayerShotEventCodec.h"
#include "protocol/UdpPacketCodec.h"

#include <WinSock2.h>

#include <cstdint>
#include <exception>
#include <iostream>
#include <string>
#include <vector>

namespace
{
    void SendTcpPacket(
        tdr::net::TcpClientSession& session,
        const tdr::protocol::MessageType type,
        const std::vector<std::uint8_t>& payload
    )
    {
        const auto packet =
            tdr::protocol::PacketCodec::Encode(
                type,
                payload
            );

        session.ReceiveBytes(
            packet.data(),
            packet.size()
        );

        static_cast<void>(
            session.TakeOutgoingPackets()
            );
    }

    std::vector<std::uint8_t> ToBytes(
        const std::string& value
    )
    {
        return {
            value.begin(),
            value.end()
        };
    }

    sockaddr_in6 CreateAddress(
        const std::uint16_t port
    )
    {
        sockaddr_in6 address{};

        address.sin6_family =
            AF_INET6;

        address.sin6_addr =
            in6addr_loopback;

        address.sin6_port =
            ::htons(port);

        return address;
    }

    std::vector<std::uint8_t> CreateShotPayload(
        const std::uint32_t playerId
    )
    {
        tdr::protocol::PlayerShotEvent shotEvent{};

        shotEvent.playerId =
            playerId;

        shotEvent.shotSequence =
            17U;

        shotEvent.originX =
            3.0F;

        shotEvent.originY =
            -2.0F;

        shotEvent.directionX =
            0.6F;

        shotEvent.directionY =
            0.8F;

        return tdr::protocol::PlayerShotEventCodec::Encode(
            shotEvent
        );
    }

    bool ExpectRejected(
        tdr::net::PlayerShotEventForwarder& forwarder,
        const std::vector<std::uint8_t>& datagram,
        const sockaddr_in6& sourceAddress,
        const char* description
    )
    {
        try
        {
            static_cast<void>(
                forwarder.Forward(
                    datagram.data(),
                    datagram.size(),
                    sourceAddress
                )
                );
        }
        catch (const std::exception&)
        {
            return true;
        }

        std::cerr
            << "[FAIL] PlayerShotEventForwarder accepted "
            << description
            << "."
            << std::endl;

        return false;
    }

    void ConfigureStartedRoom(
        tdr::net::ServerCoordinator& coordinator,
        tdr::net::TcpClientSession& host,
        tdr::net::TcpClientSession& guest
    )
    {
        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Host")
        );

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::CreateRoomRequest,
            {}
        );

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Guest")
        );

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::JoinRoomRequest,
            {}
        );

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged
                ),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Normal
                )
            }
        );

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Melee
                ),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::None
                )
            }
        );

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetReady,
            { 1U }
        );

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetReady,
            { 1U }
        );

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::StartGameRequest,
            {}
        );

        const sockaddr_in6 hostAddress =
            CreateAddress(46001U);

        const sockaddr_in6 guestAddress =
            CreateAddress(46002U);

        host.BindUdpEndpoint(
            hostAddress
        );

        guest.BindUdpEndpoint(
            guestAddress
        );
    }
}

int main()
{
    try
    {
        tdr::net::ServerCoordinator coordinator;

        auto& host =
            coordinator.CreateSession();

        auto& guest =
            coordinator.CreateSession();

        ConfigureStartedRoom(
            coordinator,
            host,
            guest
        );

        const auto payload =
            CreateShotPayload(
                host.PlayerId()
            );

        tdr::protocol::UdpMessageHeader header{};

        header.type =
            tdr::protocol::MessageType::PlayerShotEvent;

        header.sessionToken =
            host.SessionTokenBytes();

        header.playerId =
            host.PlayerId();

        header.sequence =
            50U;

        const auto request =
            tdr::protocol::UdpPacketCodec::Encode(
                header,
                payload
            );

        tdr::net::PlayerShotEventForwarder forwarder(
            coordinator
        );

        const auto forwarded =
            forwarder.Forward(
                request.data(),
                request.size(),
                host.UdpEndpointAddress()
            );

        const auto decoded =
            tdr::protocol::UdpPacketCodec::Decode(
                forwarded.bytes.data(),
                forwarded.bytes.size()
            );

        if (!guest.MatchesUdpEndpoint(
            forwarded.destination))
        {
            throw std::runtime_error(
                "Shot event destination was not the guest."
            );
        }

        if (decoded.header.type !=
            tdr::protocol::MessageType::PlayerShotEvent)
        {
            throw std::runtime_error(
                "Forwarded packet type did not match."
            );
        }

        if (decoded.header.playerId !=
            host.PlayerId())
        {
            throw std::runtime_error(
                "Forwarded player ID did not match."
            );
        }

        if (decoded.header.sequence !=
            50U)
        {
            throw std::runtime_error(
                "Forwarded sequence did not match."
            );
        }

        if (decoded.header.sessionToken !=
            guest.SessionTokenBytes())
        {
            throw std::runtime_error(
                "Forwarded session token did not match."
            );
        }

        if (decoded.payload !=
            payload)
        {
            throw std::runtime_error(
                "Forwarded shot payload did not match."
            );
        }

        const auto decodedShot =
            tdr::protocol::PlayerShotEventCodec::Decode(
                decoded.payload.data(),
                decoded.payload.size()
            );

        if (decodedShot.playerId !=
            host.PlayerId())
        {
            throw std::runtime_error(
                "Decoded shot player ID did not match."
            );
        }

        if (decodedShot.shotSequence !=
            17U)
        {
            throw std::runtime_error(
                "Decoded shot sequence did not match."
            );
        }

        tdr::protocol::UdpMessageHeader guestHeader =
            header;

        guestHeader.sessionToken =
            guest.SessionTokenBytes();

        guestHeader.playerId =
            guest.PlayerId();

        guestHeader.sequence =
            51U;

        const auto guestRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                guestHeader,
                CreateShotPayload(
                    guest.PlayerId()
                )
            );

        const bool guestRejected =
            ExpectRejected(
                forwarder,
                guestRequest,
                guest.UdpEndpointAddress(),
                "a shot event sent by the guest"
            );

        if (!guestRejected)
        {
            return 1;
        }

        std::cout
            << "[PASS] Host PlayerShotEvent was "
            << "forwarded to the guest."
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