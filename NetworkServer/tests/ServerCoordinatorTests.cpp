#include "net/ServerCoordinator.h"
#include "net/SocketRuntime.h"
#include "net/TcpConnection.h"
#include "net/UdpBindHandler.h"
#include "protocol/PacketCodec.h"
#include "protocol/UdpMessageHeader.h"
#include "protocol/UdpPacketCodec.h"

#include <WinSock2.h>

#include <iostream>
#include <utility>
#include <cstdint>
#include <string>
#include <vector>

namespace
{
    std::vector<std::uint8_t> ToBytes(
        const std::string& value
    )
    {
        return std::vector<std::uint8_t>(
            value.begin(),
            value.end()
        );
    }

    void SendPacket(
        tdr::net::TcpClientSession& session,
        const tdr::protocol::MessageType type,
        const std::vector<std::uint8_t>& payload
    )
    {
        const auto encoded =
            tdr::protocol::PacketCodec::Encode(
                type,
                payload
            );

        session.ReceiveBytes(
            encoded.data(),
            encoded.size()
        );
    }

    sockaddr_in6 CreateUdpAddress(
        const std::uint16_t port,
        const std::uint8_t lastAddressByte
    )
    {
        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_port = ::htons(port);
        address.sin6_addr = in6addr_any;
        address.sin6_addr.u.Byte[15] =
            lastAddressByte;

        return address;
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;
        tdr::net::ServerCoordinator server;

        const SOCKET rawSocket =
            ::socket(
                AF_INET6,
                SOCK_STREAM,
                IPPROTO_TCP
            );

        if (rawSocket == INVALID_SOCKET)
        {
            std::cerr
                << "[FAIL] Could not create test TCP socket."
                << std::endl;

            return 1;
        }

        tdr::net::TcpConnection connection(
            rawSocket
        );

        auto& session =
            server.AttachConnection(
                std::move(connection)
            );

        SendPacket(
            session,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("DisconnectedHost")
        );

        SendPacket(
            session,
            tdr::protocol::MessageType::CreateRoomRequest,
            {}
        );

        if (!session.HasRoom())
        {
            std::cerr
                << "[FAIL] Disconnect-test session "
                << "did not create a room."
                << std::endl;

            return 1;
        }

        const std::string roomId =
            session.CurrentRoom().Id();

        if (connection.IsValid())
        {
            std::cerr
                << "[FAIL] Connection ownership "
                << "was not transferred."
                << std::endl;

            return 1;
        }

        if (server.ConnectionCount() != 1
            || server.SessionCount() != 1)
        {
            std::cerr
                << "[FAIL] Server did not store "
                << "the connection and session."
                << std::endl;

            return 1;
        }

        if (&server.FindSessionBySocket(rawSocket)
            != &session)
        {
            std::cerr
                << "[FAIL] Socket maps to "
                << "the wrong session."
                << std::endl;

            return 1;
        }

        const auto playerId =
            session.PlayerId();

        if (&server.FindSession(playerId)
            != &session)
        {
            std::cerr
                << "[FAIL] Player ID maps to "
                << "the wrong session."
                << std::endl;

            return 1;
        }

        tdr::protocol::UdpMessageHeader validUdpHeader{};
        validUdpHeader.type =
            tdr::protocol::MessageType::UdpBindRequest;
        validUdpHeader.playerId = playerId;
        validUdpHeader.sessionToken =
            session.SessionTokenBytes();

        if (&server.FindSessionForUdp(validUdpHeader)
            != &session)
        {
            std::cerr
                << "[FAIL] Valid UDP identity mapped "
                << "to the wrong TCP session."
                << std::endl;

            return 1;
        }

        auto wrongTokenHeader = validUdpHeader;
        wrongTokenHeader.sessionToken[0] ^= 0xFFU;

        bool rejectedWrongToken = false;

        try
        {
            static_cast<void>(
                server.FindSessionForUdp(
                    wrongTokenHeader
                )
                );
        }
        catch (const std::invalid_argument&)
        {
            rejectedWrongToken = true;
        }

        if (!rejectedWrongToken)
        {
            std::cerr
                << "[FAIL] UDP identity accepted "
                << "an invalid session token."
                << std::endl;

            return 1;
        }

        auto unknownPlayerHeader = validUdpHeader;
        unknownPlayerHeader.playerId = playerId + 1000U;

        bool rejectedUnknownUdpPlayer = false;

        try
        {
            static_cast<void>(
                server.FindSessionForUdp(
                    unknownPlayerHeader
                )
                );
        }
        catch (const std::out_of_range&)
        {
            rejectedUnknownUdpPlayer = true;
        }

        if (!rejectedUnknownUdpPlayer)
        {
            std::cerr
                << "[FAIL] UDP identity accepted "
                << "an unknown player ID."
                << std::endl;

            return 1;
        }

        const sockaddr_in6 udpAddress =
            CreateUdpAddress(41000U, 1U);

        if (session.HasUdpEndpoint())
        {
            std::cerr
                << "[FAIL] UDP endpoint was already bound "
                << "before BindRequest."
                << std::endl;

            return 1;
        }

        auto wrongTypeHeader = validUdpHeader;
        wrongTypeHeader.type =
            tdr::protocol::MessageType::UdpPing;

        bool rejectedWrongBindType = false;

        try
        {
            static_cast<void>(
                server.BindUdpEndpoint(
                    wrongTypeHeader,
                    udpAddress
                )
                );
        }
        catch (const std::invalid_argument&)
        {
            rejectedWrongBindType = true;
        }

        if (!rejectedWrongBindType
            || session.HasUdpEndpoint())
        {
            std::cerr
                << "[FAIL] A non-BindRequest packet "
                << "created a UDP binding."
                << std::endl;

            return 1;
        }

        bool rejectedBindWithWrongToken = false;

        try
        {
            static_cast<void>(
                server.BindUdpEndpoint(
                    wrongTokenHeader,
                    udpAddress
                )
                );
        }
        catch (const std::invalid_argument&)
        {
            rejectedBindWithWrongToken = true;
        }

        if (!rejectedBindWithWrongToken
            || session.HasUdpEndpoint())
        {
            std::cerr
                << "[FAIL] Invalid UDP token created "
                << "an endpoint binding."
                << std::endl;

            return 1;
        }

        auto& boundSession =
            server.BindUdpEndpoint(
                validUdpHeader,
                udpAddress
            );

        if (&boundSession != &session)
        {
            std::cerr
                << "[FAIL] BindUdpEndpoint returned "
                << "the wrong TCP session."
                << std::endl;

            return 1;
        }

        if (!session.MatchesUdpEndpoint(
            udpAddress))
        {
            std::cerr
                << "[FAIL] Valid BindRequest did not "
                << "store the source address."
                << std::endl;

            return 1;
        }

        // 同一 BindRequest 重发必须保持幂等。
        static_cast<void>(
            server.BindUdpEndpoint(
                validUdpHeader,
                udpAddress
            )
            );

        const sockaddr_in6 replacementAddress =
            CreateUdpAddress(41001U, 1U);

        bool rejectedAddressReplacement = false;

        try
        {
            static_cast<void>(
                server.BindUdpEndpoint(
                    validUdpHeader,
                    replacementAddress
                )
                );
        }
        catch (const std::runtime_error&)
        {
            rejectedAddressReplacement = true;
        }

        if (!rejectedAddressReplacement)
        {
            std::cerr
                << "[FAIL] Coordinator replaced an "
                << "existing UDP endpoint."
                << std::endl;

            return 1;
        }

        if (!session.MatchesUdpEndpoint(
            udpAddress))
        {
            std::cerr
                << "[FAIL] Rejected replacement changed "
                << "the original UDP endpoint."
                << std::endl;

            return 1;
        }

        tdr::net::UdpBindHandler udpBindHandler(
            server
        );

        auto bindRequestHeader =
            validUdpHeader;

        bindRequestHeader.sequence = 73U;

        const auto encodedBindRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                bindRequestHeader,
                {}
            );

        const auto encodedBindAccepted =
            udpBindHandler.Handle(
                encodedBindRequest.data(),
                encodedBindRequest.size(),
                udpAddress
            );

        const auto decodedBindAccepted =
            tdr::protocol::UdpPacketCodec::Decode(
                encodedBindAccepted.data(),
                encodedBindAccepted.size()
            );

        if (decodedBindAccepted.header.type
            != tdr::protocol::MessageType::UdpBindAccepted
            || decodedBindAccepted.header.sessionToken
            != bindRequestHeader.sessionToken
            || decodedBindAccepted.header.playerId
            != bindRequestHeader.playerId
            || decodedBindAccepted.header.sequence
            != bindRequestHeader.sequence
            || !decodedBindAccepted.payload.empty())
        {
            std::cerr
                << "[FAIL] Valid UDP BindRequest did not "
                << "produce the expected BindAccepted packet."
                << std::endl;

            return 1;
        }

        const std::vector<std::uint8_t>
            nonEmptyBindPayload{
                0x01U
        };

        const auto encodedBindWithPayload =
            tdr::protocol::UdpPacketCodec::Encode(
                bindRequestHeader,
                nonEmptyBindPayload
            );

        bool rejectedNonEmptyBindPayload = false;

        try
        {
            static_cast<void>(
                udpBindHandler.Handle(
                    encodedBindWithPayload.data(),
                    encodedBindWithPayload.size(),
                    udpAddress
                )
                );
        }
        catch (const std::invalid_argument&)
        {
            rejectedNonEmptyBindPayload = true;
        }

        if (!rejectedNonEmptyBindPayload)
        {
            std::cerr
                << "[FAIL] UDP BindRequest accepted "
                << "a non-empty payload."
                << std::endl;

            return 1;
        }

        auto& mappedConnection =
            server.FindConnectionByPlayerId(
                playerId
            );

        if (mappedConnection.NativeHandle()
            != rawSocket)
        {
            std::cerr
                << "[FAIL] Player ID maps to "
                << "the wrong TCP connection."
                << std::endl;

            return 1;
        }

        bool rejectedUnknownPlayer = false;

        try
        {
            server.SendPacketToPlayer(
                999999,
                tdr::protocol::MessageType::ServerHello,
                {}
            );
        }
        catch (const std::out_of_range&)
        {
            rejectedUnknownPlayer = true;
        }

        if (!rejectedUnknownPlayer)
        {
            std::cerr
                << "[FAIL] Server accepted a packet "
                << "for an unknown player."
                << std::endl;

            return 1;
        }

        const SOCKET guestRawSocket =
            ::socket(
                AF_INET6,
                SOCK_STREAM,
                IPPROTO_TCP
            );

        if (guestRawSocket == INVALID_SOCKET)
        {
            std::cerr
                << "[FAIL] Could not create "
                << "guest test TCP socket."
                << std::endl;

            return 1;
        }

        tdr::net::TcpConnection guestConnection(
            guestRawSocket
        );

        auto& guestSession =
            server.AttachConnection(
                std::move(guestConnection)
            );

        SendPacket(
            guestSession,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("ConnectedGuest")
        );

        SendPacket(
            guestSession,
            tdr::protocol::MessageType::JoinRoomRequest,
            ToBytes(roomId)
        );

        if (!guestSession.HasRoom())
        {
            std::cerr
                << "[FAIL] Guest session did not "
                << "join the host room."
                << std::endl;

            return 1;
        }

        if (server.ConnectionCount() != 2
            || server.SessionCount() != 2)
        {
            std::cerr
                << "[FAIL] Server did not store "
                << "both connected clients."
                << std::endl;

            return 1;
        }

        SendPacket(
            session,
            tdr::protocol::MessageType::
            SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Hard)
            }
        );

        SendPacket(
            session,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );

        SendPacket(
            guestSession,
            tdr::protocol::MessageType::
            SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Melee),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::None)
            }
        );

        SendPacket(
            guestSession,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );

        const auto snapshot =
            server.BuildRoomStateSnapshot(
                roomId
            );

        if (snapshot.roomId != roomId ||
            snapshot.roomStatus !=
            static_cast<std::uint8_t>(
                tdr::room::RoomStatus::Waiting) ||
            snapshot.difficultyId !=
            static_cast<std::uint8_t>(
                tdr::room::DifficultyId::Hard))
        {
            std::cerr
                << "[FAIL] Coordinator mapped "
                << "the wrong room-level snapshot data."
                << std::endl;

            return 1;
        }

        if (snapshot.players.size() != 2U)
        {
            std::cerr
                << "[FAIL] Coordinator mapped "
                << "the wrong snapshot player count."
                << std::endl;

            return 1;
        }

        const auto& hostSnapshot =
            snapshot.players[0];

        if (hostSnapshot.playerId !=
            session.PlayerId() ||
            !hostSnapshot.isHost ||
            !hostSnapshot.isReady ||
            hostSnapshot.characterId !=
            static_cast<std::uint8_t>(
                tdr::room::CharacterId::Ranged) ||
            hostSnapshot.nickname !=
            "DisconnectedHost")
        {
            std::cerr
                << "[FAIL] Coordinator mapped "
                << "the host snapshot incorrectly."
                << std::endl;

            return 1;
        }

        const auto& guestSnapshot =
            snapshot.players[1];

        if (guestSnapshot.playerId !=
            guestSession.PlayerId() ||
            guestSnapshot.isHost ||
            !guestSnapshot.isReady ||
            guestSnapshot.characterId !=
            static_cast<std::uint8_t>(
                tdr::room::CharacterId::Melee) ||
            guestSnapshot.nickname !=
            "ConnectedGuest")
        {
            std::cerr
                << "[FAIL] Coordinator mapped "
                << "the guest snapshot incorrectly."
                << std::endl;

            return 1;
        }

        server.RemoveConnection(rawSocket);

        if (server.ConnectionCount() != 1
            || server.SessionCount() != 1)
        {
            std::cerr
                << "[FAIL] Host disconnect removed "
                << "the wrong number of clients."
                << std::endl;

            return 1;
        }

        if (guestSession.HasRoom())
        {
            std::cerr
                << "[FAIL] Host disconnect left "
                << "the guest session referencing "
                << "a deleted room."
                << std::endl;

            return 1;
        }

        server.RemoveConnection(
            guestRawSocket
        );

        if (server.ConnectionCount() != 0
            || server.SessionCount() != 0)
        {
            std::cerr
                << "[FAIL] Final guest cleanup "
                << "did not empty the server."
                << std::endl;

            return 1;
        }

        bool disconnectedHostRoomRemoved = false;

        try
        {
            const auto& remainingRoom =
                server.Rooms().FindRoom(
                    roomId
                );

            (void)remainingRoom;
        }
        catch (const std::out_of_range&)
        {
            disconnectedHostRoomRemoved = true;
        }

        if (!disconnectedHostRoomRemoved)
        {
            std::cerr
                << "[FAIL] Disconnecting the host "
                << "left its room behind."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] Host disconnect closes the room "
            << "and invalidates guest room state."
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