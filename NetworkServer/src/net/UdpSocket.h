#pragma once

#include <WinSock2.h>
#include <WS2tcpip.h>
#include <cstddef>
#include <cstdint>

namespace tdr::net
{
    class UdpSocket final
    {
    public:
        UdpSocket();
        ~UdpSocket() noexcept;

        UdpSocket(const UdpSocket&) = delete;
        UdpSocket& operator=(const UdpSocket&) = delete;

        UdpSocket(UdpSocket&&) = delete;
        UdpSocket& operator=(UdpSocket&&) = delete;

        [[nodiscard]]
        bool IsValid() const noexcept;

        [[nodiscard]]
        bool IsDualStackEnabled() const;

        void Bind(
            unsigned short port
        );

        [[nodiscard]]
        bool IsBound() const noexcept;

        [[nodiscard]]
        unsigned short BoundPort() const noexcept;

        [[nodiscard]]
        std::size_t SendTo(
            const std::uint8_t* data,
            std::size_t size,
            const sockaddr_in6& destination
        );

        [[nodiscard]]
        std::size_t ReceiveFrom(
            std::uint8_t* buffer,
            std::size_t capacity,
            sockaddr_in6& sourceAddress
        );

        [[nodiscard]]
        SOCKET NativeHandle() const noexcept;

    private:
        SOCKET socket_ = INVALID_SOCKET;
        bool isBound_ = false;
        unsigned short boundPort_ = 0;
    };
}