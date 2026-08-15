#include "net/TcpListener.h"

#include <stdexcept>
#include <string>

namespace tdr::net
{
    TcpListener::TcpListener()
    {
        socket_ = ::socket(
            AF_INET6,
            SOCK_STREAM,
            IPPROTO_TCP
        );

        if (socket_ == INVALID_SOCKET)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to create IPv6 TCP socket. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        const DWORD ipv6Only = 0;

        const int setOptionResult =
            ::setsockopt(
                socket_,
                IPPROTO_IPV6,
                IPV6_V6ONLY,
                reinterpret_cast<const char*>(&ipv6Only),
                sizeof(ipv6Only)
            );

        if (setOptionResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            ::closesocket(socket_);
            socket_ = INVALID_SOCKET;

            throw std::runtime_error(
                "Failed to enable IPv6 dual-stack mode. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }
    }

    TcpListener::~TcpListener() noexcept
    {
        if (socket_ != INVALID_SOCKET)
        {
            ::closesocket(socket_);
            socket_ = INVALID_SOCKET;
        }
    }

    bool TcpListener::IsValid() const noexcept
    {
        return socket_ != INVALID_SOCKET;
    }

    SOCKET TcpListener::NativeHandle() const noexcept
    {
        return socket_;
    }

    bool TcpListener::IsDualStackEnabled() const
    {
        if (socket_ == INVALID_SOCKET)
        {
            return false;
        }

        DWORD ipv6Only = 1;
        int optionSize = sizeof(ipv6Only);

        const int getOptionResult =
            ::getsockopt(
                socket_,
                IPPROTO_IPV6,
                IPV6_V6ONLY,
                reinterpret_cast<char*>(&ipv6Only),
                &optionSize
            );

        if (getOptionResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to query IPv6 dual-stack mode. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        return ipv6Only == 0;
    }

    void TcpListener::BindAndListen(
        const unsigned short port
    )
    {
        if (socket_ == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Cannot bind an invalid TCP socket."
            );
        }

        if (isListening_)
        {
            throw std::runtime_error(
                "TCP listener is already listening."
            );
        }

        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_addr = in6addr_any;
        address.sin6_port = ::htons(port);

        const int bindResult =
            ::bind(
                socket_,
                reinterpret_cast<const sockaddr*>(&address),
                sizeof(address)
            );

        if (bindResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to bind IPv6 TCP listener. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        const int listenResult =
            ::listen(
                socket_,
                SOMAXCONN
            );

        if (listenResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to start TCP listener. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        sockaddr_in6 boundAddress{};
        int boundAddressSize = sizeof(boundAddress);

        const int nameResult =
            ::getsockname(
                socket_,
                reinterpret_cast<sockaddr*>(&boundAddress),
                &boundAddressSize
            );

        if (nameResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to query bound TCP port. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        boundPort_ =
            ::ntohs(boundAddress.sin6_port);

        isListening_ = true;
    }

    TcpConnection TcpListener::Accept()
    {
        if (!isListening_)
        {
            throw std::runtime_error(
                "Cannot accept a connection "
                "before the listener starts."
            );
        }

        sockaddr_in6 clientAddress{};
        int clientAddressSize =
            sizeof(clientAddress);

        const SOCKET clientSocket =
            ::accept(
                socket_,
                reinterpret_cast<sockaddr*>(
                    &clientAddress),
                &clientAddressSize
            );

        if (clientSocket == INVALID_SOCKET)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to accept TCP connection. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        return TcpConnection(clientSocket);
    }

    bool TcpListener::IsListening() const noexcept
    {
        return isListening_;
    }

    unsigned short TcpListener::BoundPort() const noexcept
    {
        return boundPort_;
    }
}