#include "net/UdpSocket.h"

#include <stdexcept>
#include <string>
#include <limits>

namespace tdr::net
{
    UdpSocket::UdpSocket()
    {
        socket_ = ::socket(
            AF_INET6,
            SOCK_DGRAM,
            IPPROTO_UDP
        );

        if (socket_ == INVALID_SOCKET)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to create IPv6 UDP socket. "
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
                reinterpret_cast<const char*>(
                    &ipv6Only),
                sizeof(ipv6Only)
            );

        if (setOptionResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            ::closesocket(socket_);
            socket_ = INVALID_SOCKET;

            throw std::runtime_error(
                "Failed to enable IPv6 UDP "
                "dual-stack mode. WSA error code: "
                + std::to_string(errorCode)
            );
        }
    }

    UdpSocket::~UdpSocket() noexcept
    {
        if (socket_ != INVALID_SOCKET)
        {
            ::closesocket(socket_);
            socket_ = INVALID_SOCKET;
        }
    }

    bool UdpSocket::IsValid() const noexcept
    {
        return socket_ != INVALID_SOCKET;
    }

    bool UdpSocket::IsDualStackEnabled() const
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
                reinterpret_cast<char*>(
                    &ipv6Only),
                &optionSize
            );

        if (getOptionResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to query IPv6 UDP "
                "dual-stack mode. WSA error code: "
                + std::to_string(errorCode)
            );
        }

        return ipv6Only == 0;
    }

    void UdpSocket::Bind(
        const unsigned short port
    )
    {
        if (socket_ == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Cannot bind an invalid UDP socket."
            );
        }

        if (isBound_)
        {
            throw std::runtime_error(
                "UDP socket is already bound."
            );
        }

        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_addr = in6addr_any;
        address.sin6_port = ::htons(port);

        const int bindResult =
            ::bind(
                socket_,
                reinterpret_cast<const sockaddr*>(
                    &address),
                sizeof(address)
            );

        if (bindResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to bind IPv6 UDP socket. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        sockaddr_in6 boundAddress{};
        int boundAddressSize =
            sizeof(boundAddress);

        const int nameResult =
            ::getsockname(
                socket_,
                reinterpret_cast<sockaddr*>(
                    &boundAddress),
                &boundAddressSize
            );

        if (nameResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "Failed to query bound UDP port. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        boundPort_ =
            ::ntohs(boundAddress.sin6_port);

        isBound_ = true;
    }

    SOCKET UdpSocket::NativeHandle() const noexcept
    {
        return socket_;
    }

    bool UdpSocket::IsBound() const noexcept
    {
        return isBound_;
    }

    unsigned short UdpSocket::BoundPort() const noexcept
    {
        return boundPort_;
    }

    std::size_t UdpSocket::SendTo(
        const std::uint8_t* const data,
        const std::size_t size,
        const sockaddr_in6& destination
    )
    {
        if (socket_ == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Cannot send using an invalid UDP socket."
            );
        }

        if (data == nullptr && size != 0U)
        {
            throw std::invalid_argument(
                "UDP send data cannot be null."
            );
        }

        if (size > static_cast<std::size_t>(
            std::numeric_limits<int>::max()))
        {
            throw std::length_error(
                "UDP send size exceeds Winsock limit."
            );
        }

        const int sentBytes =
            ::sendto(
                socket_,
                reinterpret_cast<const char*>(data),
                static_cast<int>(size),
                0,
                reinterpret_cast<const sockaddr*>(
                    &destination),
                sizeof(destination)
            );

        if (sentBytes == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "UDP sendto failed. WSA error code: "
                + std::to_string(errorCode)
            );
        }

        if (static_cast<std::size_t>(sentBytes)
            != size)
        {
            throw std::runtime_error(
                "UDP sendto sent a partial datagram."
            );
        }

        return static_cast<std::size_t>(sentBytes);
    }

    std::size_t UdpSocket::ReceiveFrom(
        std::uint8_t* const buffer,
        const std::size_t capacity,
        sockaddr_in6& sourceAddress
    )
    {
        if (socket_ == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Cannot receive using an invalid UDP socket."
            );
        }

        if (buffer == nullptr)
        {
            throw std::invalid_argument(
                "UDP receive buffer cannot be null."
            );
        }

        if (capacity == 0U)
        {
            throw std::invalid_argument(
                "UDP receive capacity cannot be zero."
            );
        }

        if (capacity > static_cast<std::size_t>(
            std::numeric_limits<int>::max()))
        {
            throw std::length_error(
                "UDP receive capacity exceeds Winsock limit."
            );
        }

        sourceAddress = {};
        int sourceAddressSize =
            sizeof(sourceAddress);

        const int receivedBytes =
            ::recvfrom(
                socket_,
                reinterpret_cast<char*>(buffer),
                static_cast<int>(capacity),
                0,
                reinterpret_cast<sockaddr*>(
                    &sourceAddress),
                &sourceAddressSize
            );

        if (receivedBytes == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            throw std::runtime_error(
                "UDP recvfrom failed. WSA error code: "
                + std::to_string(errorCode)
            );
        }

        if (sourceAddress.sin6_family != AF_INET6)
        {
            throw std::runtime_error(
                "UDP source address is not AF_INET6."
            );
        }

        return static_cast<std::size_t>(
            receivedBytes
            );
    }
}