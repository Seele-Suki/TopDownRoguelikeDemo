#include "net/TcpConnection.h"

#include <stdexcept>
#include <utility>
#include <limits>
#include <string>

namespace tdr::net
{
    TcpConnection::TcpConnection(
        const SOCKET socket
    )
        : socket_(socket)
    {
        if (socket_ == INVALID_SOCKET)
        {
            throw std::invalid_argument(
                "Cannot create a TCP connection "
                "from an invalid socket."
            );
        }
    }

    TcpConnection::~TcpConnection() noexcept
    {
        Close();
    }

    TcpConnection::TcpConnection(
        TcpConnection&& other
    ) noexcept
        : socket_(
            std::exchange(
                other.socket_,
                INVALID_SOCKET))
    {
    }

    TcpConnection& TcpConnection::operator=(
        TcpConnection&& other
        ) noexcept
    {
        if (this != &other)
        {
            Close();

            socket_ = std::exchange(
                other.socket_,
                INVALID_SOCKET);
        }

        return *this;
    }

    bool TcpConnection::IsValid() const noexcept
    {
        return socket_ != INVALID_SOCKET;
    }

    SOCKET TcpConnection::NativeHandle() const noexcept
    {
        return socket_;
    }

    void TcpConnection::SendAll(
        const std::uint8_t* const data,
        const std::size_t size
    ) const
    {
        if (socket_ == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Cannot send through an invalid "
                "TCP connection."
            );
        }

        if (size == 0)
        {
            return;
        }

        if (data == nullptr)
        {
            throw std::invalid_argument(
                "TCP send data cannot be null "
                "when size is non-zero."
            );
        }

        std::size_t sentByteCount = 0;

        while (sentByteCount < size)
        {
            const std::size_t remainingByteCount =
                size - sentByteCount;

            const std::size_t maximumSendSize =
                static_cast<std::size_t>(
                    std::numeric_limits<int>::max()
                    );

            const int currentSendSize =
                static_cast<int>(
                    remainingByteCount > maximumSendSize
                    ? maximumSendSize
                    : remainingByteCount
                    );

            const int result =
                ::send(
                    socket_,
                    reinterpret_cast<const char*>(
                        data + sentByteCount),
                    currentSendSize,
                    0
                );

            if (result == SOCKET_ERROR)
            {
                const int errorCode =
                    ::WSAGetLastError();

                throw std::runtime_error(
                    "Failed to send TCP data. "
                    "WSA error code: "
                    + std::to_string(errorCode)
                );
            }

            if (result == 0)
            {
                throw std::runtime_error(
                    "TCP send made no progress."
                );
            }

            sentByteCount +=
                static_cast<std::size_t>(result);
        }
    }

    void TcpConnection::Close() noexcept
    {
        if (socket_ != INVALID_SOCKET)
        {
            ::closesocket(socket_);
            socket_ = INVALID_SOCKET;
        }
    }
}