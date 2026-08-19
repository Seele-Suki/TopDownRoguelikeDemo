#include "net/ServerLoop.h"

#include "net/ServerCoordinator.h"
#include "net/TcpConnection.h"
#include "net/TcpListener.h"
#include "net/UdpSocket.h"
#include "protocol/UdpBindingCredentialsCodec.h"
#include "protocol/UdpPacketCodec.h"

#include <stdexcept>
#include <utility>
#include <array>
#include <cstdint>
#include <vector>

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
        udpPingHandler_(coordinator)
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
            return;
        }

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

                    default:
                        throw std::invalid_argument(
                            "Unsupported UDP message type."
                        );
                    }
                }
                catch (const std::invalid_argument&)
                {
                    continue;
                }
                catch (const std::out_of_range&)
                {
                    continue;
                }
                catch (const std::runtime_error&)
                {
                    continue;
                }

                static_cast<void>(
                    udpSocket_.SendTo(
                        response.data(),
                        response.size(),
                        sourceAddress
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
