#include "net/PlayerInputForwarder.h"
#include "net/ServerCoordinator.h"
#include "net/PlayerStateForwarder.h"
#include "protocol/PlayerStateSnapshotCodec.h"
#include "protocol/PacketCodec.h"
#include "protocol/PlayerInputCodec.h"
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
        const std::vector<std::uint8_t>& payload)
    {
        const auto packet =
            tdr::protocol::PacketCodec::Encode(
                type,
                payload);

        session.ReceiveBytes(
            packet.data(),
            packet.size());

        static_cast<void>(
            session.TakeOutgoingPackets());
    }

    std::vector<std::uint8_t> ToBytes(
        const std::string& value)
    {
        return {
            value.begin(),
            value.end()
        };
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

    bool ExpectRejected(
        tdr::net::PlayerInputForwarder& forwarder,
        const std::vector<std::uint8_t>& datagram,
        const sockaddr_in6& sourceAddress,
        const char* const description)
    {
        try
        {
            static_cast<void>(
                forwarder.Forward(
                    datagram.data(),
                    datagram.size(),
                    sourceAddress)
                );
        }
        catch (const std::exception&)
        {
            return true;
        }

        std::cerr
            << "[FAIL] PlayerInputForwarder accepted "
            << description
            << "."
            << std::endl;

        return false;
    }

    bool ExpectStateRejected(
        tdr::net::PlayerStateForwarder& forwarder,
        const std::vector<std::uint8_t>& datagram,
        const sockaddr_in6& sourceAddress,
        const char* const description)
    {
        try
        {
            static_cast<void>(
                forwarder.Forward(
                    datagram.data(),
                    datagram.size(),
                    sourceAddress)
                );
        }
        catch (const std::exception&)
        {
            return true;
        }

        std::cerr
            << "[FAIL] PlayerStateForwarder accepted "
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

        auto& host =
            coordinator.CreateSession();

        auto& guest =
            coordinator.CreateSession();

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Host"));

        SendTcpPacket(
            host,
            tdr::protocol::MessageType::CreateRoomRequest,
            {});

        SendTcpPacket(
            guest,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Guest"));

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

        const sockaddr_in6 hostAddress =
            CreateAddress(46001U);

        const sockaddr_in6 guestAddress =
            CreateAddress(46002U);

        host.BindUdpEndpoint(hostAddress);
        guest.BindUdpEndpoint(guestAddress);

        const tdr::protocol::PlayerInputPayload input{
            0.5F,
            -0.25F,
            1.0F,
            0.0F
        };

        const auto payload =
            tdr::protocol::PlayerInputCodec::Encode(
                input);

        tdr::protocol::UdpMessageHeader header{};
        header.type =
            tdr::protocol::MessageType::PlayerInput;
        header.sessionToken =
            guest.SessionTokenBytes();
        header.playerId =
            guest.PlayerId();
        header.sequence = 10U;

        const auto request =
            tdr::protocol::UdpPacketCodec::Encode(
                header,
                payload);

        tdr::net::PlayerInputForwarder forwarder(
            coordinator);

        const auto forwarded =
            forwarder.Forward(
                request.data(),
                request.size(),
                guestAddress);

        const auto decoded =
            tdr::protocol::UdpPacketCodec::Decode(
                forwarded.bytes.data(),
                forwarded.bytes.size());

        if (!host.MatchesUdpEndpoint(
            forwarded.destination) ||
            decoded.header.type !=
            tdr::protocol::MessageType::PlayerInput ||
            decoded.header.playerId !=
            guest.PlayerId() ||
            decoded.header.sequence != 10U ||
            decoded.header.sessionToken !=
            host.SessionTokenBytes() ||
            decoded.payload != payload)
        {
            std::cerr
                << "[FAIL] PlayerInput was not "
                << "forwarded to the host correctly."
                << std::endl;

            return 1;
        }

        const tdr::protocol::PlayerStateSnapshotPayload
            stateSnapshot{
                {
                    {
                        host.PlayerId(),
                        -1.0F,
                        2.0F,
                        1.0F,
                        0.0F
                    },
                    {
                        guest.PlayerId(),
                        3.0F,
                        -2.0F,
                        0.0F,
                        1.0F
                    }
                }
        };

        const auto statePayload =
            tdr::protocol::
            PlayerStateSnapshotCodec::Encode(
                stateSnapshot);

        tdr::protocol::UdpMessageHeader
            stateHeader{};

        stateHeader.type =
            tdr::protocol::MessageType::
            PlayerStateSnapshot;

        stateHeader.sessionToken =
            host.SessionTokenBytes();

        stateHeader.playerId =
            host.PlayerId();

        stateHeader.sequence = 30U;

        const auto stateRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                stateHeader,
                statePayload);

        tdr::net::PlayerStateForwarder
            stateForwarder(
                coordinator);

        if (!host.AcceptUdpSequence(100U))
        {
            std::cerr
                << "[FAIL] Could not prepare the "
                << "independent host Ping sequence."
                << std::endl;

            return 1;
        }

        const auto forwardedState =
            stateForwarder.Forward(
                stateRequest.data(),
                stateRequest.size(),
                hostAddress);

        const auto decodedState =
            tdr::protocol::UdpPacketCodec::Decode(
                forwardedState.bytes.data(),
                forwardedState.bytes.size());

        if (!guest.MatchesUdpEndpoint(
            forwardedState.destination) ||
            decodedState.header.type !=
            tdr::protocol::MessageType::
            PlayerStateSnapshot ||
            decodedState.header.playerId !=
            host.PlayerId() ||
            decodedState.header.sequence != 30U ||
            decodedState.header.sessionToken !=
            guest.SessionTokenBytes() ||
            decodedState.payload != statePayload)
        {
            std::cerr
                << "[FAIL] PlayerStateSnapshot was "
                << "not forwarded to the guest correctly."
                << std::endl;

            return 1;
        }

        const bool rejectedDuplicateState =
            ExpectStateRejected(
                stateForwarder,
                stateRequest,
                hostAddress,
                "a duplicate state sequence");

        const tdr::protocol::PlayerStateSnapshotPayload
            incompleteSnapshot{
                {
                    {
                        host.PlayerId(),
                        -1.0F,
                        2.0F,
                        1.0F,
                        0.0F
                    }
                }
        };

        const auto incompletePayload =
            tdr::protocol::
            PlayerStateSnapshotCodec::Encode(
                incompleteSnapshot);

        auto nextStateHeader =
            stateHeader;

        nextStateHeader.sequence = 31U;

        const auto incompleteRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                nextStateHeader,
                incompletePayload);

        const bool rejectedIncompleteState =
            ExpectStateRejected(
                stateForwarder,
                incompleteRequest,
                hostAddress,
                "a snapshot missing the guest player");

        const tdr::protocol::PlayerStateSnapshotPayload
            unrelatedSnapshot{
                {
                    {
                        host.PlayerId(),
                        -1.0F,
                        2.0F,
                        1.0F,
                        0.0F
                    },
                    {
                        999U,
                        3.0F,
                        -2.0F,
                        0.0F,
                        1.0F
                    }
                }
        };

        const auto unrelatedPayload =
            tdr::protocol::
            PlayerStateSnapshotCodec::Encode(
                unrelatedSnapshot);

        const auto unrelatedRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                nextStateHeader,
                unrelatedPayload);

        const bool rejectedUnrelatedState =
            ExpectStateRejected(
                stateForwarder,
                unrelatedRequest,
                hostAddress,
                "a snapshot containing an unrelated player");

        auto guestStateHeader =
            stateHeader;

        guestStateHeader.sessionToken =
            guest.SessionTokenBytes();

        guestStateHeader.playerId =
            guest.PlayerId();

        guestStateHeader.sequence = 31U;

        const auto guestStateRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                guestStateHeader,
                statePayload);

        const bool rejectedGuestState =
            ExpectStateRejected(
                stateForwarder,
                guestStateRequest,
                guestAddress,
                "a state snapshot sent by the guest");

        const sockaddr_in6 wrongStateSource =
            CreateAddress(46998U);

        const auto nextStateRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                nextStateHeader,
                statePayload);

        const bool rejectedWrongStateSource =
            ExpectStateRejected(
                stateForwarder,
                nextStateRequest,
                wrongStateSource,
                "an unbound state source address");

        if (!rejectedDuplicateState ||
            !rejectedIncompleteState ||
            !rejectedUnrelatedState ||
            !rejectedGuestState ||
            !rejectedWrongStateSource)
        {
            return 1;
        }

        if (!guest.AcceptUdpSequence(100U))
        {
            std::cerr
                << "[FAIL] Could not prepare the "
                << "independent Ping sequence."
                << std::endl;

            return 1;
        }

        auto wrongSourceHeader =
            header;
        wrongSourceHeader.sequence = 11U;

        const auto wrongSourceRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                wrongSourceHeader,
                payload);

        const sockaddr_in6 wrongSourceAddress =
            CreateAddress(46999U);

        const bool rejectedWrongSource =
            ExpectRejected(
                forwarder,
                wrongSourceRequest,
                wrongSourceAddress,
                "an unbound source address");

        auto hostHeader =
            header;
        hostHeader.sessionToken =
            host.SessionTokenBytes();
        hostHeader.playerId =
            host.PlayerId();
        hostHeader.sequence = 11U;

        const auto hostRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                hostHeader,
                payload);

        const bool rejectedHostInput =
            ExpectRejected(
                forwarder,
                hostRequest,
                hostAddress,
                "input sent by the room host");

        tdr::net::ServerCoordinator
            waitingCoordinator;

        auto& waitingHost =
            waitingCoordinator.CreateSession();

        auto& waitingGuest =
            waitingCoordinator.CreateSession();

        SendTcpPacket(
            waitingHost,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("WaitingHost"));

        SendTcpPacket(
            waitingHost,
            tdr::protocol::MessageType::CreateRoomRequest,
            {});

        SendTcpPacket(
            waitingGuest,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("WaitingGuest"));

        SendTcpPacket(
            waitingGuest,
            tdr::protocol::MessageType::JoinRoomRequest,
            {});

        const sockaddr_in6 waitingHostAddress =
            CreateAddress(46101U);

        const sockaddr_in6 waitingGuestAddress =
            CreateAddress(46102U);

        waitingHost.BindUdpEndpoint(
            waitingHostAddress);

        waitingGuest.BindUdpEndpoint(
            waitingGuestAddress);

        auto waitingHeader =
            header;
        waitingHeader.sessionToken =
            waitingGuest.SessionTokenBytes();
        waitingHeader.playerId =
            waitingGuest.PlayerId();
        waitingHeader.sequence = 1U;

        const auto waitingRequest =
            tdr::protocol::UdpPacketCodec::Encode(
                waitingHeader,
                payload);

        tdr::net::PlayerInputForwarder
            waitingForwarder(
                waitingCoordinator);

        const bool rejectedWaitingRoom =
            ExpectRejected(
                waitingForwarder,
                waitingRequest,
                waitingGuestAddress,
                "input before the room started");

        const bool rejectedDuplicate =
            ExpectRejected(
                forwarder,
                request,
                guestAddress,
                "a duplicate input sequence");

        if (!rejectedWrongSource ||
            !rejectedHostInput ||
            !rejectedWaitingRoom ||
            !rejectedDuplicate)
        {
            return 1;
        }

        std::cout
            << "[PASS] Guest PlayerInput was "
            << "forwarded to the host."
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