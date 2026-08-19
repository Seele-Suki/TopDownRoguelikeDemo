#include "net/TcpClientSession.h"
#include "protocol/PacketCodec.h"
#include "room/RoomManager.h"
#include "room/PlayerIdAllocator.h"
#include "room/SessionTokenGenerator.h"
#include "protocol/SessionTokenCodec.h"
#include "net/UdpEndpoint.h"

#include <cstdint>
#include <iostream>
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

    bool ExpectSingleChangedRoom(
        tdr::net::TcpClientSession& session,
        const std::string& expectedRoomId,
        const std::string& operation
    )
    {
        const auto changedRoomIds =
            session.TakeChangedRoomIds();

        if (changedRoomIds.size() != 1U ||
            changedRoomIds.front() != expectedRoomId)
        {
            std::cerr
                << "[FAIL] "
                << operation
                << " did not report exactly one "
                << "changed room."
                << std::endl;

            return false;
        }

        return true;
    }

    bool TestSuccessfulRoomChangesAreReported()
    {
        tdr::room::RoomManager roomManager;
        tdr::room::PlayerIdAllocator playerIdAllocator;
        tdr::room::SessionTokenGenerator tokenGenerator;

        tdr::net::TcpClientSession host(
            playerIdAllocator,
            tokenGenerator,
            roomManager
        );

        SendPacket(
            host,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Host")
        );

        SendPacket(
            host,
            tdr::protocol::MessageType::
            CreateRoomRequest,
            {}
        );

        static_cast<void>(
            host.TakeOutgoingPackets()
            );

        if (!ExpectSingleChangedRoom(
            host,
            "ROOM-1",
            "CreateRoomRequest"))
        {
            return false;
        }

        SendPacket(
            host,
            tdr::protocol::MessageType::
            SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Normal)
            }
        );

        if (!ExpectSingleChangedRoom(
            host,
            "ROOM-1",
            "SetPlayerSelection"))
        {
            return false;
        }

        SendPacket(
            host,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );

        if (!ExpectSingleChangedRoom(
            host,
            "ROOM-1",
            "SetReady"))
        {
            return false;
        }

        tdr::net::TcpClientSession guest(
            playerIdAllocator,
            tokenGenerator,
            roomManager
        );

        SendPacket(
            guest,
            tdr::protocol::MessageType::SetNickname,
            ToBytes("Guest")
        );

        SendPacket(
            guest,
            tdr::protocol::MessageType::
            JoinRoomRequest,
            ToBytes("ROOM-1")
        );

        static_cast<void>(
            guest.TakeOutgoingPackets()
            );

        if (!ExpectSingleChangedRoom(
            guest,
            "ROOM-1",
            "JoinRoomRequest"))
        {
            return false;
        }

        SendPacket(
            guest,
            tdr::protocol::MessageType::
            SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Melee),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::None)
            }
        );

        if (!ExpectSingleChangedRoom(
            guest,
            "ROOM-1",
            "Guest SetPlayerSelection"))
        {
            return false;
        }

        SendPacket(
            guest,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );

        if (!ExpectSingleChangedRoom(
            guest,
            "ROOM-1",
            "Guest SetReady"))
        {
            return false;
        }

        SendPacket(
            guest,
            tdr::protocol::MessageType::LeaveRoom,
            {}
        );

        if (!ExpectSingleChangedRoom(
            guest,
            "ROOM-1",
            "Guest LeaveRoom"))
        {
            return false;
        }

        return true;
    }

    bool ExpectLeaveRoomAcknowledgement(
        tdr::net::TcpClientSession& session,
        const std::string& operation
    )
    {
        const auto outgoingPackets =
            session.TakeOutgoingPackets();

        if (outgoingPackets.size() != 1U)
        {
            std::cerr
                << "[FAIL] "
                << operation
                << " did not produce exactly one "
                << "LeaveRoom acknowledgement."
                << std::endl;

            return false;
        }

        tdr::protocol::PacketCodec codec;

        codec.Append(
            outgoingPackets.front().data(),
            outgoingPackets.front().size()
        );

        const auto decodedPackets =
            codec.DecodeAvailable();

        if (decodedPackets.size() != 1U)
        {
            std::cerr
                << "[FAIL] "
                << operation
                << " acknowledgement could not "
                << "be decoded."
                << std::endl;

            return false;
        }

        const auto& acknowledgement =
            decodedPackets.front();

        if (acknowledgement.type !=
            tdr::protocol::MessageType::LeaveRoom ||
            !acknowledgement.payload.empty())
        {
            std::cerr
                << "[FAIL] "
                << operation
                << " produced an invalid "
                << "LeaveRoom acknowledgement."
                << std::endl;

            return false;
        }

        return true;
    }
}

int main()
{
    if (!TestSuccessfulRoomChangesAreReported())
    {
        return 1;
    }

    tdr::room::RoomManager roomManager;

    tdr::room::PlayerIdAllocator playerIdAllocator;

    tdr::room::SessionTokenGenerator tokenGenerator;

    tdr::net::TcpClientSession hostSession(
        playerIdAllocator,
        tokenGenerator,
        roomManager
    );

    if (hostSession.SessionToken().size() != 32)
    {
        std::cerr
            << "[FAIL] Host session token length is incorrect."
            << std::endl;

        return 1;
    }

    SendPacket(
        hostSession,
        tdr::protocol::MessageType::SetNickname,
        ToBytes("Seele")
    );

    SendPacket(
        hostSession,
        tdr::protocol::MessageType::CreateRoomRequest,
        {}
    );

    const auto outgoingPackets =
        hostSession.TakeOutgoingPackets();

    if (outgoingPackets.size() != 1)
    {
        std::cerr
            << "[FAIL] CreateRoomRequest did not "
            << "produce exactly one response."
            << std::endl;

        return 1;
    }

    tdr::protocol::PacketCodec responseCodec;

    responseCodec.Append(
        outgoingPackets.front().data(),
        outgoingPackets.front().size()
    );

    const auto decodedResponses =
        responseCodec.DecodeAvailable();

    if (decodedResponses.size() != 1)
    {
        std::cerr
            << "[FAIL] CreateRoomResponse could not "
            << "be decoded."
            << std::endl;

        return 1;
    }

    const auto& createRoomResponse =
        decodedResponses.front();

    if (createRoomResponse.type
        != tdr::protocol::MessageType::CreateRoomResponse)
    {
        std::cerr
            << "[FAIL] Server returned the wrong "
            << "response message type."
            << std::endl;

        return 1;
    }

    if (createRoomResponse.payload
        != ToBytes("ROOM-1"))
    {
        std::cerr
            << "[FAIL] CreateRoomResponse contained "
            << "the wrong room ID."
            << std::endl;

        return 1;
    }

    const auto changedRoomIds =
        hostSession.TakeChangedRoomIds();

    if (changedRoomIds.size() != 1U)
    {
        std::cerr
            << "[FAIL] CreateRoomRequest did not "
            << "report exactly one changed room."
            << std::endl;

        return 1;
    }

    if (changedRoomIds.front() != "ROOM-1")
    {
        std::cerr
            << "[FAIL] CreateRoomRequest reported "
            << "the wrong changed room ID."
            << std::endl;

        return 1;
    }

    bool selectionAccepted = true;

    try
    {
        SendPacket(
            hostSession,
            tdr::protocol::MessageType::SetPlayerSelection,
            {
                static_cast<std::uint8_t>(
                    tdr::room::CharacterId::Ranged),
                static_cast<std::uint8_t>(
                    tdr::room::DifficultyId::Normal)
            }
        );
    }
    catch (const std::exception& exception)
    {
        selectionAccepted = false;

        std::cerr
            << "[FAIL] SetPlayerSelection was rejected: "
            << exception.what()
            << std::endl;
    }

    if (!selectionAccepted)
    {
        return 1;
    }

    const auto& selectedRoom =
        hostSession.CurrentRoom();

    const auto& selectedHost =
        selectedRoom.PlayerAt(0);

    if (selectedHost.selectedCharacter
        != tdr::room::CharacterId::Ranged)
    {
        std::cerr
            << "[FAIL] TCP selection did not store "
            << "the selected character."
            << std::endl;

        return 1;
    }

    if (selectedRoom.SelectedDifficulty()
        != tdr::room::DifficultyId::Normal)
    {
        std::cerr
            << "[FAIL] TCP selection did not store "
            << "the selected difficulty."
            << std::endl;

        return 1;
    }

    bool readyAccepted = true;

    try
    {
        SendPacket(
            hostSession,
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(1)
            }
        );
    }
    catch (const std::exception& exception)
    {
        readyAccepted = false;

        std::cerr
            << "[FAIL] SetReady was rejected: "
            << exception.what()
            << std::endl;
    }

    if (!readyAccepted)
    {
        return 1;
    }

    if (!hostSession.CurrentRoom()
        .PlayerAt(0)
        .isReady)
    {
        std::cerr
            << "[FAIL] TCP SetReady did not update "
            << "the room player state."
            << std::endl;

        return 1;
    }

    SendPacket(
        hostSession,
        tdr::protocol::MessageType::SetReady,
        {
            static_cast<std::uint8_t>(0)
        }
    );

    if (hostSession.CurrentRoom()
        .PlayerAt(0)
        .isReady)
    {
        std::cerr
            << "[FAIL] TCP SetReady=false did not "
            << "clear the ready state."
            << std::endl;

        return 1;
    }

    SendPacket(
        hostSession,
        tdr::protocol::MessageType::SetReady,
        {
            static_cast<std::uint8_t>(1)
        }
    );

    if (!hostSession.HasRoom())
    {
        std::cerr
            << "[FAIL] Host session did not create a room."
            << std::endl;

        return 1;
    }

    const std::string roomId =
        hostSession.CurrentRoom().Id();

    tdr::net::TcpClientSession joiningSession(
        playerIdAllocator,
        tokenGenerator,
        roomManager
    );

    if (joiningSession.SessionToken().size() != 32)
    {
        std::cerr
            << "[FAIL] Joining session token length is incorrect."
            << std::endl;

        return 1;
    }

    if (!hostSession.MatchesSessionToken(
        hostSession.SessionToken()))
    {
        std::cerr
            << "[FAIL] Session did not accept "
            << "its own token."
            << std::endl;

        return 1;
    }

    if (hostSession.MatchesSessionToken(
        joiningSession.SessionToken()))
    {
        std::cerr
            << "[FAIL] Session accepted another "
            << "session token."
            << std::endl;

        return 1;
    }

    if (hostSession.MatchesSessionToken(
        "invalid-token"))
    {
        std::cerr
            << "[FAIL] Session accepted an invalid token."
            << std::endl;

        return 1;
    }

    if (hostSession.SessionToken()
        == joiningSession.SessionToken())
    {
        std::cerr
            << "[FAIL] Session tokens are not unique."
            << std::endl;

        return 1;
    }

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::SetNickname,
        ToBytes("Bronya")
    );

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::JoinRoomRequest,
        ToBytes(roomId)
    );

    const auto initialJoinResponses =
        joiningSession.TakeOutgoingPackets();

    if (initialJoinResponses.size() != 1)
    {
        std::cerr
            << "[FAIL] Initial JoinRoomRequest did not "
            << "produce exactly one response."
            << std::endl;

        return 1;
    }

    if (!joiningSession.HasRoom())
    {
        std::cerr
            << "[FAIL] Joining session has no room."
            << std::endl;

        return 1;
    }

    const auto& room =
        roomManager.FindRoom(roomId);

    if (room.PlayerCount() != 2)
    {
        std::cerr
            << "[FAIL] Shared room does not contain "
            << "two players."
            << std::endl;

        return 1;
    }

    if (room.PlayerAt(0).playerId
        != hostSession.PlayerId()
        || room.PlayerAt(0).nickname != "Seele"
        || !room.PlayerAt(0).isHost)
    {
        std::cerr
            << "[FAIL] Host player state is incorrect."
            << std::endl;

        return 1;
    }

    if (room.PlayerAt(1).playerId
        != joiningSession.PlayerId()
        || room.PlayerAt(1).nickname != "Bronya"
        || room.PlayerAt(1).isHost)
    {
        std::cerr
            << "[FAIL] Joining player state is incorrect."
            << std::endl;

        return 1;
    }

    if (joiningSession.CurrentRoom().Id() != roomId)
    {
        std::cerr
            << "[FAIL] Joining session references "
            << "the wrong room."
            << std::endl;

        return 1;
    }

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::LeaveRoom,
        {}
    );

    if (!ExpectLeaveRoomAcknowledgement(
        joiningSession,
        "Guest LeaveRoom"))
    {
        return 1;
    }

    if (!joiningSession.TakeClosedRoomIds().empty())
    {
        std::cerr
            << "[FAIL] Guest LeaveRoom incorrectly "
            << "reported the room as closed."
            << std::endl;

        return 1;
    }

    if (joiningSession.HasRoom())
    {
        std::cerr
            << "[FAIL] Guest session still references "
            << "the room after leaving."
            << std::endl;

        return 1;
    }

    if (hostSession.CurrentRoom().PlayerCount() != 1)
    {
        std::cerr
            << "[FAIL] Guest LeaveRoom did not remove "
            << "the guest from the room."
            << std::endl;

        return 1;
    }

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::JoinRoomRequest,
        ToBytes(roomId)
    );

    const auto joinResponses =
        joiningSession.TakeOutgoingPackets();

    if (joinResponses.size() != 1)
    {
        std::cerr
            << "[FAIL] JoinRoomRequest did not "
            << "produce exactly one response."
            << std::endl;

        return 1;
    }

    tdr::protocol::PacketCodec joinResponseCodec;

    joinResponseCodec.Append(
        joinResponses.front().data(),
        joinResponses.front().size()
    );

    const auto decodedJoinResponses =
        joinResponseCodec.DecodeAvailable();

    if (decodedJoinResponses.size() != 1)
    {
        std::cerr
            << "[FAIL] JoinRoomResponse could not "
            << "be decoded."
            << std::endl;

        return 1;
    }

    const auto& joinResponse =
        decodedJoinResponses.front();

    if (joinResponse.type
        != tdr::protocol::MessageType::JoinRoomResponse)
    {
        std::cerr
            << "[FAIL] JoinRoomRequest returned "
            << "the wrong response type."
            << std::endl;

        return 1;
    }

    if (joinResponse.payload
        != ToBytes(roomId))
    {
        std::cerr
            << "[FAIL] JoinRoomResponse contained "
            << "the wrong room ID."
            << std::endl;

        return 1;
    }

    if (!joiningSession.HasRoom()
        || hostSession.CurrentRoom().PlayerCount() != 2)
    {
        std::cerr
            << "[FAIL] Guest could not rejoin "
            << "after leaving."
            << std::endl;

        return 1;
    }

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::SetPlayerSelection,
        {
            static_cast<std::uint8_t>(
                tdr::room::CharacterId::Ranged),
            static_cast<std::uint8_t>(
                tdr::room::DifficultyId::None)
        }
    );

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::SetReady,
        {
            static_cast<std::uint8_t>(1)
        }
    );

    const auto rejectedStartPacket =
        tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::
            StartGameRequest,
            {}
        );

    const auto clearReadyPacket =
        tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::SetReady,
            {
                static_cast<std::uint8_t>(0)
            }
        );

    std::vector<std::uint8_t> combinedPackets =
        rejectedStartPacket;

    combinedPackets.insert(
        combinedPackets.end(),
        clearReadyPacket.begin(),
        clearReadyPacket.end()
    );

    bool requestErrorEscaped = false;

    try
    {
        joiningSession.ReceiveBytes(
            combinedPackets.data(),
            combinedPackets.size()
        );
    }
    catch (const std::exception& exception)
    {
        requestErrorEscaped = true;

        std::cerr
            << "[FAIL] Invalid room request escaped "
            << "the client session: "
            << exception.what()
            << std::endl;
    }

    if (requestErrorEscaped)
    {
        return 1;
    }

    const auto errorPackets =
        joiningSession.TakeOutgoingPackets();

    if (errorPackets.size() != 1U)
    {
        std::cerr
            << "[FAIL] Invalid room request did not "
            << "produce exactly one ErrorMessage."
            << std::endl;

        return 1;
    }

    tdr::protocol::PacketCodec errorResponseCodec;

    errorResponseCodec.Append(
        errorPackets.front().data(),
        errorPackets.front().size()
    );

    const auto decodedErrors =
        errorResponseCodec.DecodeAvailable();

    if (decodedErrors.size() != 1U)
    {
        std::cerr
            << "[FAIL] ErrorMessage response could "
            << "not be decoded."
            << std::endl;

        return 1;
    }

    const auto& errorResponse =
        decodedErrors.front();

    if (errorResponse.type !=
        tdr::protocol::MessageType::ErrorMessage)
    {
        std::cerr
            << "[FAIL] Invalid room request produced "
            << "the wrong response type."
            << std::endl;

        return 1;
    }

    if (errorResponse.payload != ToBytes(
        "Only the room host can start the game."))
    {
        std::cerr
            << "[FAIL] ErrorMessage contained "
            << "the wrong message."
            << std::endl;

        return 1;
    }

    if (hostSession.CurrentRoom()
        .PlayerAt(1U).isReady)
    {
        std::cerr
            << "[FAIL] Session did not process the "
            << "valid packet after the rejected packet."
            << std::endl;

        return 1;
    }

    SendPacket(
        joiningSession,
        tdr::protocol::MessageType::SetReady,
        {
            static_cast<std::uint8_t>(1)
        }
    );

    if (hostSession.CurrentRoom().Status()
        != tdr::room::RoomStatus::Waiting)
    {
        std::cerr
            << "[FAIL] Rejected start request "
            << "changed the room status."
            << std::endl;

        return 1;
    }

    static_cast<void>(
        hostSession.TakeChangedRoomIds()
        );

    bool hostStartAccepted = true;

    try
    {
        SendPacket(
            hostSession,
            tdr::protocol::MessageType::StartGameRequest,
            {}
        );
    }
    catch (const std::exception& exception)
    {
        hostStartAccepted = false;

        std::cerr
            << "[FAIL] Host StartGameRequest "
            << "was rejected: "
            << exception.what()
            << std::endl;
    }

    if (!hostStartAccepted)
    {
        return 1;
    }

    if (hostSession.CurrentRoom().Status()
        != tdr::room::RoomStatus::Started)
    {
        std::cerr
            << "[FAIL] Host StartGameRequest did not "
            << "start the room."
            << std::endl;

        return 1;
    }

    const auto startChangedRoomIds =
        hostSession.TakeChangedRoomIds();

    if (startChangedRoomIds.size() != 1U ||
        startChangedRoomIds.front() != roomId)
    {
        std::cerr
            << "[FAIL] StartGameRequest did not "
            << "report the started room as changed."
            << std::endl;

        return 1;
    }

    const auto startedRoomIds =
        hostSession.TakeStartedRoomIds();

    if (startedRoomIds.size() != 1U ||
        startedRoomIds.front() != roomId)
    {
        std::cerr
            << "[FAIL] StartGameRequest did not "
            << "report exactly one started room."
            << std::endl;

        return 1;
    }

    if (!hostSession.TakeStartedRoomIds().empty())
    {
        std::cerr
            << "[FAIL] Started room notifications "
            << "were not cleared after being taken."
            << std::endl;

        return 1;
    }

    bool hostLeaveAccepted = true;

    try
    {
        SendPacket(
            hostSession,
            tdr::protocol::MessageType::LeaveRoom,
            {}
        );
    }
    catch (const std::exception& exception)
    {
        hostLeaveAccepted = false;

        std::cerr
            << "[FAIL] Host LeaveRoom was rejected: "
            << exception.what()
            << std::endl;
    }

    if (!hostLeaveAccepted)
    {
        return 1;
    }

    if (!ExpectLeaveRoomAcknowledgement(
        hostSession,
        "Host LeaveRoom"))
    {
        return 1;
    }

    const auto closedRoomIds =
        hostSession.TakeClosedRoomIds();

    if (closedRoomIds.size() != 1U ||
        closedRoomIds.front() != roomId)
    {
        std::cerr
            << "[FAIL] Host LeaveRoom did not "
            << "report exactly one closed room."
            << std::endl;

        return 1;
    }

    if (!hostSession.TakeClosedRoomIds().empty())
    {
        std::cerr
            << "[FAIL] Closed room notifications "
            << "were not cleared after being taken."
            << std::endl;

        return 1;
    }

    if (hostSession.HasRoom())
    {
        std::cerr
            << "[FAIL] Host session still references "
            << "the room after leaving."
            << std::endl;

        return 1;
    }

    if (joiningSession.HasRoom())
    {
        std::cerr
            << "[FAIL] Host LeaveRoom left "
            << "the guest session referencing "
            << "a deleted room."
            << std::endl;

        return 1;
    }

    bool roomWasRemoved = false;

    try
    {
        const auto& removedRoom =
            roomManager.FindRoom(
                roomId
            );

        (void)removedRoom;
    }
    catch (const std::out_of_range&)
    {
        roomWasRemoved = true;
    }

    if (!roomWasRemoved)
    {
        std::cerr
            << "[FAIL] Host LeaveRoom did not "
            << "remove the room."
            << std::endl;

        return 1;
    }

    const auto expectedHostTokenBytes =
        tdr::protocol::SessionTokenCodec::DecodeHex(
            hostSession.SessionToken()
        );

    if (hostSession.SessionTokenBytes()
        != expectedHostTokenBytes)
    {
        std::cerr
            << "[FAIL] Session binary token does not "
            << "match its TCP text token."
            << std::endl;

        return 1;
    }

    if (!hostSession.MatchesSessionToken(
        hostSession.SessionTokenBytes()))
    {
        std::cerr
            << "[FAIL] Session rejected its own "
            << "binary UDP token."
            << std::endl;

        return 1;
    }

    if (hostSession.MatchesSessionToken(
        joiningSession.SessionTokenBytes()))
    {
        std::cerr
            << "[FAIL] Session accepted another "
            << "session's binary UDP token."
            << std::endl;

        return 1;
    }

    if (hostSession.HasUdpEndpoint())
    {
        std::cerr
            << "[FAIL] New session already has "
            << "a UDP endpoint."
            << std::endl;

        return 1;
    }

    const sockaddr_in6 firstUdpAddress =
        CreateUdpAddress(40000U, 1U);

    hostSession.BindUdpEndpoint(firstUdpAddress);

    if (!hostSession.HasUdpEndpoint())
    {
        std::cerr
            << "[FAIL] Session did not store "
            << "its UDP endpoint."
            << std::endl;

        return 1;
    }

    if (!hostSession.MatchesUdpEndpoint(
        firstUdpAddress))
    {
        std::cerr
            << "[FAIL] Session rejected its "
            << "bound UDP endpoint."
            << std::endl;

        return 1;
    }

    // 同一来源重复发送 BindRequest 时保持幂等。
    hostSession.BindUdpEndpoint(firstUdpAddress);

    const sockaddr_in6 wrongPortAddress =
        CreateUdpAddress(40001U, 1U);

    if (hostSession.MatchesUdpEndpoint(
        wrongPortAddress))
    {
        std::cerr
            << "[FAIL] Session accepted a different "
            << "UDP source port."
            << std::endl;

        return 1;
    }

    const sockaddr_in6 wrongIpAddress =
        CreateUdpAddress(40000U, 2U);

    if (hostSession.MatchesUdpEndpoint(
        wrongIpAddress))
    {
        std::cerr
            << "[FAIL] Session accepted a different "
            << "UDP source address."
            << std::endl;

        return 1;
    }

    bool rejectedEndpointReplacement = false;

    try
    {
        hostSession.BindUdpEndpoint(
            wrongPortAddress
        );
    }
    catch (const std::runtime_error&)
    {
        rejectedEndpointReplacement = true;
    }

    if (!rejectedEndpointReplacement)
    {
        std::cerr
            << "[FAIL] Session allowed its UDP "
            << "endpoint to be replaced."
            << std::endl;

        return 1;
    }

    std::cout
        << "[PASS] Active LeaveRoom keeps "
        << "all session room state consistent."
        << std::endl;

    return 0;
}