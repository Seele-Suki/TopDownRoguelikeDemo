#pragma once

#include <WinSock2.h>
#include <cstddef>
#include <cstdint>

namespace tdr::net
{
    class TcpConnection final
    {
    public:
        explicit TcpConnection(
            SOCKET socket
        );

        ~TcpConnection() noexcept;

        TcpConnection(const TcpConnection&) = delete;
        TcpConnection& operator=(const TcpConnection&) = delete;

        TcpConnection(TcpConnection&& other) noexcept;

        TcpConnection& operator=(
            TcpConnection&& other
            ) noexcept;

        [[nodiscard]]
        bool IsValid() const noexcept;

        [[nodiscard]]
        SOCKET NativeHandle() const noexcept;

        void SendAll(
            const std::uint8_t* data,
            std::size_t size
        ) const;
    private:
        void Close() noexcept;

        SOCKET socket_ = INVALID_SOCKET;
    };
}