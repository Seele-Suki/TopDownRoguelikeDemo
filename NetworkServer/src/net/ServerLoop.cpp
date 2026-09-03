#include "net/ServerLoop.h"

#include "net/ServerCoordinator.h"
#include "net/TcpConnection.h"
#include "net/TcpListener.h"
#include "net/UdpSocket.h"
#include "net/WorldEntitySpawnForwarder.h"
#include "net/SharedExperienceForwarder.h"
#include "net/WorldEntityRemovalForwarder.h"
#include "protocol/UdpBindingCredentialsCodec.h"
#include "protocol/UdpPacketCodec.h"
#include "protocol/HeartbeatTiming.h"

#include <stdexcept>
#include <utility>
#include <array>
#include <cstdint>
#include <vector>
#include <iostream>
#include <cstring>

namespace tdr::net
{
    ServerLoop::ServerLoop(
        TcpListener& listener,
        UdpSocket& udpSocket,
        ServerCoordinator& coordinator
    )
        : listener_(listener),
        udpSocket_(udpSocket),
        coordinator_(coordinator),
        udpBindHandler_(coordinator),
        udpPingHandler_(coordinator),
        playerInputForwarder_(coordinator),
        playerStateForwarder_(coordinator),
        playerShotEventForwarder_(coordinator),
        playerShotgunEventForwarder_(coordinator)
        , worldStateForwarder_(coordinator)
    {
        if (!listener_.IsListening())
        {
            throw std::invalid_argument(
                "Server loop requires a listening "
                "TCP listener."
            );
        }

        if (!udpSocket_.IsBound())
        {
            throw std::invalid_argument(
                "Server loop requires a bound UDP socket."
            );
        }

        selectLoop_.AddSocket(
            listener_.NativeHandle()
        );

        selectLoop_.AddSocket(
            udpSocket_.NativeHandle()
        );
    }

    void ServerLoop::PollOnce(
        const std::chrono::milliseconds timeout
    )
    {
        const auto removeTimedOut = [&]()
        {
            const auto sockets = coordinator_.RemoveTimedOutConnections(
                TcpClientSession::Clock::now(),
                std::chrono::milliseconds(static_cast<int>(
                    tdr::protocol::kHeartbeatTimeoutSeconds * 1000.0)));
            for (const SOCKET socket : sockets)
            {
                std::cerr << "[TCP] heartbeat timeout; closing socket "
                          << static_cast<unsigned long long>(socket)
                          << std::endl;
                selectLoop_.RemoveSocket(socket);
            }
        };

        const SelectResult result =
            selectLoop_.Poll(timeout);

        if (result.HasError())
        {
            throw std::runtime_error(
                "select failed. WSA error code: "
                + std::to_string(
                    result.ErrorCode())
            );
        }

        if (result.DidTimeout())
        {
            removeTimedOut();
            return;
        }

        removeTimedOut();

        for (const SOCKET readableSocket :
        result.ReadableSockets())
        {
            if (readableSocket
                == udpSocket_.NativeHandle())
            {
                std::array<std::uint8_t, 4096>
                    receiveBuffer{};
                sockaddr_in6 sourceAddress{};

                const std::size_t receivedByteCount =
                    udpSocket_.ReceiveFrom(
                        receiveBuffer.data(),
                        receiveBuffer.size(),
                        sourceAddress
                    );

                std::vector<std::uint8_t> response;

                sockaddr_in6 responseDestination =
                    sourceAddress;

                try
                {
                    const auto packet =
                        tdr::protocol::UdpPacketCodec::Decode(
                            receiveBuffer.data(),
                            receivedByteCount
                        );

                    switch (packet.header.type)
                    {
                    case tdr::protocol::MessageType::UdpBindRequest:
                        response =
                            udpBindHandler_.Handle(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress
                            );
                        break;

                    case tdr::protocol::MessageType::UdpPing:
                        response =
                            udpPingHandler_.Handle(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress
                            );
                        break;

                    case tdr::protocol::MessageType::PlayerInput:
                    {
                        auto forwarded =
                            playerInputForwarder_.Forward(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress
                            );

                        response =
                            std::move(forwarded.bytes);

                        responseDestination =
                            forwarded.destination;

                        break;
                    }

                    case tdr::protocol::MessageType::
                    PlayerStateSnapshot:
                    {
                        auto forwarded =
                            playerStateForwarder_.Forward(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress
                            );

                        response =
                            std::move(
                                forwarded.bytes);

                        responseDestination =
                            forwarded.destination;

                        break;
                    }

                    case tdr::protocol::MessageType::
                    PlayerShotEvent:
                    {
                        auto forwarded =
                            playerShotEventForwarder_.Forward(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress
                            );

                        response =
                            std::move(
                                forwarded.bytes
                            );

                        responseDestination =
                            forwarded.destination;

                        break;
                    }

                    case tdr::protocol::MessageType::
                    PlayerShotgunEvent:
                    {
                        auto forwarded =
                            playerShotgunEventForwarder_.Forward(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress
                            );

                        response =
                            std::move(
                                forwarded.bytes
                            );

                        responseDestination =
                            forwarded.destination;

                        break;
                    }

                    case tdr::protocol::MessageType::
                    WorldStateSnapshot:
                    {
                        auto forwarded =
                            worldStateForwarder_.Forward(
                                receiveBuffer.data(),
                                receivedByteCount,
                                sourceAddress);

                        response =
                            std::move(
                                forwarded.bytes);

                        responseDestination =
                            forwarded.destination;

                        break;
                    }

                    default:
                        throw std::invalid_argument(
                            "Unsupported UDP message type."
                        );
                    }
                }
                catch (const std::exception& exception)
                {
                    std::cerr
                        << "[UDP] PlayerShotEvent, PlayerShotgunEvent, or other UDP packet was rejected: "
                        << exception.what()
                        << std::endl;

                    continue;
                }

                static_cast<void>(
                    udpSocket_.SendTo(
                        response.data(),
                        response.size(),
                        responseDestination
                    )
                );

                continue;
            }

            if (readableSocket
                == listener_.NativeHandle())
            {
                TcpConnection connection =
                    listener_.Accept();

                const SOCKET clientSocket =
                    connection.NativeHandle();

                TcpClientSession& session =
                    coordinator_.AttachConnection(
                        std::move(connection)
                    );

                try
                {
                    const tdr::protocol::
                        UdpBindingCredentials credentials{
                            session.PlayerId(),
                            session.SessionTokenBytes()
                        };

                    const auto credentialsPayload =
                        tdr::protocol::
                        UdpBindingCredentialsCodec::Encode(
                            credentials
                        );

                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(),
                        tdr::protocol::MessageType::ServerHello,
                        credentialsPayload
                    );

                    selectLoop_.AddSocket(
                        clientSocket
                    );
                }
                catch (...)
                {
                    coordinator_.RemoveConnection(
                        clientSocket
                    );

                    throw;
                }

                continue;
            }

            std::array<std::uint8_t, 4096>
                receiveBuffer{};

            const int receivedByteCount =
                ::recv(
                    readableSocket,
                    reinterpret_cast<char*>(
                        receiveBuffer.data()),
                    static_cast<int>(
                        receiveBuffer.size()),
                    0
                );

            if (receivedByteCount == 0)
            {
                selectLoop_.RemoveSocket(
                    readableSocket
                );

                coordinator_.RemoveConnection(
                    readableSocket
                );

                continue;
            }

            if (receivedByteCount == SOCKET_ERROR)
            {
                const int errorCode =
                    ::WSAGetLastError();

                if (errorCode == WSAECONNRESET)
                {
                    selectLoop_.RemoveSocket(
                        readableSocket
                    );

                    coordinator_.RemoveConnection(
                        readableSocket
                    );

                    continue;
                }

                throw std::runtime_error(
                    "Failed to receive TCP data. "
                    "WSA error code: "
                    + std::to_string(errorCode)
                );
            }

            TcpClientSession& session =
                coordinator_.FindSessionBySocket(
                    readableSocket
                );

            session.ReceiveBytes(
                receiveBuffer.data(),
                static_cast<std::size_t>(
                    receivedByteCount)
            );

            const auto spawnPayloads =
                session.TakeWorldEntitySpawnPayloads();

            for (const auto& spawnPayload :
                spawnPayloads)
            {
                try
                {
                    const auto forwarded =
                        WorldEntitySpawnForwarder::Forward(
                            session,
                            spawnPayload);

                    coordinator_.SendPacketToPlayer(
                        forwarded.targetPlayerId,
                        tdr::protocol::MessageType::
                            WorldEntitySpawned,
                        forwarded.payload);
                }
                catch (const std::exception& exception)
                {
                    const std::string errorMessage =
                        exception.what();

                    const std::vector<std::uint8_t>
                        errorPayload(
                            errorMessage.begin(),
                            errorMessage.end());

                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(),
                        tdr::protocol::MessageType::
                            ErrorMessage,
                        errorPayload);
                }
            }

            const auto removalPayloads =
                session.TakeWorldEntityRemovalPayloads();

            for (const auto& removalPayload : removalPayloads)
            {
                try
                {
                    const auto forwarded =
                        WorldEntityRemovalForwarder::Forward(
                            session,
                            removalPayload);

                    coordinator_.SendPacketToPlayer(
                        forwarded.targetPlayerId,
                        tdr::protocol::MessageType::
                            WorldEntityRemoved,
                        forwarded.payload);
                }
                catch (const std::exception& exception)
                {
                    const std::string errorMessage =
                        exception.what();
                    const std::vector<std::uint8_t> errorPayload(
                        errorMessage.begin(),
                        errorMessage.end());

                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(),
                        tdr::protocol::MessageType::ErrorMessage,
                        errorPayload);
                }
            }

            const auto sharedPayloads = session.TakeSharedExperiencePayloads();
            for (const auto& sharedPayload : sharedPayloads)
            {
                try
                {
                    const auto forwarded = SharedExperienceForwarder::Forward(session, sharedPayload);
                    coordinator_.SendPacketToPlayer(
                        forwarded.targetPlayerId,
                        tdr::protocol::MessageType::SharedExperienceSnapshot,
                        forwarded.payload);
                }
                catch (const std::exception& exception)
                {
                    const std::vector<std::uint8_t> errorPayload(
                        exception.what(), exception.what() + std::strlen(exception.what()));
                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(), tdr::protocol::MessageType::ErrorMessage, errorPayload);
                }
            }

            const auto bossStatePayloads =
                session.TakeBossCombatStatePayloads();
            for (const auto& payload : bossStatePayloads)
            {
                try
                {
                    if (!session.HasRoom() ||
                        session.CurrentRoom().Status() !=
                            tdr::room::RoomStatus::Started ||
                        session.PlayerId() !=
                            session.CurrentRoom().HostPlayerId() ||
                        payload.size() != 1U ||
                        payload[0] < 1U || payload[0] > 3U)
                    {
                        throw std::invalid_argument(
                            "Invalid Boss combat state message.");
                    }

                    const auto& room = session.CurrentRoom();
                    std::uint32_t guestPlayerId = 0U;
                    for (std::size_t i = 0; i < room.PlayerCount(); ++i)
                    {
                        if (room.PlayerAt(i).playerId != room.HostPlayerId())
                        {
                            guestPlayerId = room.PlayerAt(i).playerId;
                            break;
                        }
                    }
                    if (guestPlayerId == 0U)
                        throw std::runtime_error("Boss state room target is missing.");

                    coordinator_.SendPacketToPlayer(
                        guestPlayerId,
                        tdr::protocol::MessageType::BossCombatState,
                        payload);
                }
                catch (const std::exception& exception)
                {
                    const std::vector<std::uint8_t> errorPayload(
                        exception.what(), exception.what() + std::strlen(exception.what()));
                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(),
                        tdr::protocol::MessageType::ErrorMessage,
                        errorPayload);
                }
            }

            const auto gameResultPayloads = session.TakeGameResultPayloads();
            for (const auto& payload : gameResultPayloads)
            {
                try
                {
                    if (!session.HasRoom() ||
                        session.CurrentRoom().Status() != tdr::room::RoomStatus::Started ||
                        session.PlayerId() != session.CurrentRoom().HostPlayerId() ||
                        payload.size() != 1U || payload[0] < 1U || payload[0] > 2U)
                        throw std::invalid_argument("Invalid game result message.");

                    const auto& room = session.CurrentRoom();
                    std::uint32_t guestPlayerId = 0U;
                    for (std::size_t i = 0; i < room.PlayerCount(); ++i)
                    {
                        if (room.PlayerAt(i).playerId != room.HostPlayerId())
                        {
                            guestPlayerId = room.PlayerAt(i).playerId;
                            break;
                        }
                    }
                    if (guestPlayerId == 0U)
                        throw std::runtime_error("Game result room target is missing.");
                    coordinator_.SendPacketToPlayer(
                        guestPlayerId,
                        tdr::protocol::MessageType::GameResult,
                        payload);
                }
                catch (const std::exception& exception)
                {
                    const std::vector<std::uint8_t> errorPayload(
                        exception.what(), exception.what() + std::strlen(exception.what()));
                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(),
                        tdr::protocol::MessageType::ErrorMessage,
                        errorPayload);
                }
            }

            const auto playerDiedPayloads = session.TakePlayerDiedPayloads();
            for (const auto& payload : playerDiedPayloads)
            {
                try
                {
                    if (!session.HasRoom() ||
                        session.CurrentRoom().Status() != tdr::room::RoomStatus::Started ||
                        session.PlayerId() == session.CurrentRoom().HostPlayerId() ||
                        !payload.empty())
                        throw std::invalid_argument("Invalid PlayerDied message.");
                    coordinator_.SendPacketToPlayer(
                        session.CurrentRoom().HostPlayerId(),
                        tdr::protocol::MessageType::PlayerDied,
                        payload);
                }
                catch (const std::exception& exception)
                {
                    const std::vector<std::uint8_t> errorPayload(
                        exception.what(), exception.what() + std::strlen(exception.what()));
                    coordinator_.SendPacketToPlayer(
                        session.PlayerId(),
                        tdr::protocol::MessageType::ErrorMessage,
                        errorPayload);
                }
            }

            const auto forwardUpgradePayloads =
                [&](const auto& payloads,
                    tdr::protocol::MessageType messageType,
                    bool hostOnly)
            {
                for (const auto& payload : payloads)
                {
                    try
                    {
                        if (!session.HasRoom() ||
                            session.CurrentRoom().Status() !=
                                tdr::room::RoomStatus::Started)
                        {
                            throw std::runtime_error(
                                "Upgrade message requires a started room.");
                        }

                        const auto& room = session.CurrentRoom();
                        const bool senderIsHost =
                            session.PlayerId() == room.HostPlayerId();

                        if ((hostOnly && !senderIsHost) ||
                            (!hostOnly && senderIsHost))
                        {
                            throw std::invalid_argument(
                                "Upgrade message sender role is invalid.");
                        }

                        std::uint32_t targetPlayerId =
                            hostOnly ? 0U : room.HostPlayerId();

                        if (hostOnly)
                        {
                            for (std::size_t i = 0; i < room.PlayerCount(); ++i)
                            {
                                if (room.PlayerAt(i).playerId != room.HostPlayerId())
                                {
                                    targetPlayerId = room.PlayerAt(i).playerId;
                                    break;
                                }
                            }
                        }

                        if (targetPlayerId == 0U)
                            throw std::runtime_error("Upgrade room target is missing.");

                        coordinator_.SendPacketToPlayer(
                            targetPlayerId,
                            messageType,
                            payload);
                    }
                    catch (const std::exception& exception)
                    {
                        const std::vector<std::uint8_t> errorPayload(
                            exception.what(),
                            exception.what() + std::strlen(exception.what()));
                        coordinator_.SendPacketToPlayer(
                            session.PlayerId(),
                            tdr::protocol::MessageType::ErrorMessage,
                            errorPayload);
                    }
                }
            };

            forwardUpgradePayloads(
                session.TakeUpgradeStartedPayloads(),
                tdr::protocol::MessageType::UpgradeStarted,
                true);
            forwardUpgradePayloads(
                session.TakeUpgradeChoicePayloads(),
                tdr::protocol::MessageType::UpgradeChoiceSubmitted,
                false);
            forwardUpgradePayloads(
                session.TakeUpgradeCompletedPayloads(),
                tdr::protocol::MessageType::UpgradeCompleted,
                true);

            const auto outgoingPackets =
                session.TakeOutgoingPackets();

            TcpConnection& connection =
                coordinator_.FindConnectionByPlayerId(
                    session.PlayerId()
                );

            for (const auto& packet :
                outgoingPackets)
            {
                connection.SendAll(
                    packet.data(),
                    packet.size()
                );
            }

            const auto changedRoomIds =
                session.TakeChangedRoomIds();

            for (const std::string& roomId :
                changedRoomIds)
            {
                coordinator_.BroadcastRoomState(
                    roomId
                );
            }

            const auto startedRoomIds =
                session.TakeStartedRoomIds();

            for (const std::string& roomId :
                startedRoomIds)
            {
                coordinator_.BroadcastGameStarted(
                    roomId
                );
            }

            const auto closedRoomIds =
                session.TakeClosedRoomIds();

            for (const std::string& roomId :
                closedRoomIds)
            {
                coordinator_.NotifyRoomClosed(
                    roomId
                );
            }
        }
    }

    void ServerLoop::RunUntil(
        const std::function<bool()>& shouldStop,
        const std::chrono::milliseconds pollTimeout
    )
    {
        if (!shouldStop)
        {
            throw std::invalid_argument(
                "Server loop stop callback cannot be empty."
            );
        }

        while (!shouldStop())
        {
            PollOnce(pollTimeout);
        }
    }
}
