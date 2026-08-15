#include "net/ServerLoop.h"

#include "net/ServerCoordinator.h"
#include "net/TcpConnection.h"
#include "net/TcpListener.h"

#include <stdexcept>
#include <utility>
#include <array>
#include <cstdint>

namespace tdr::net
{
    ServerLoop::ServerLoop(
        TcpListener& listener,
        ServerCoordinator& coordinator
    )
        : listener_(listener),
        coordinator_(coordinator)
    {
        if (!listener_.IsListening())
        {
            throw std::invalid_argument(
                "Server loop requires a listening "
                "TCP listener."
            );
        }

        selectLoop_.AddSocket(
            listener_.NativeHandle()
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

                (void)session;

                try
                {
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