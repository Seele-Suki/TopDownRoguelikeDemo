#pragma once

#include <WinSock2.h>
#include <WS2tcpip.h>
#include "net/TcpConnection.h"

namespace tdr::net
{
    class TcpListener final
    {
    public:
        TcpListener();
        ~TcpListener() noexcept;

        TcpListener(const TcpListener&) = delete;
        TcpListener& operator=(const TcpListener&) = delete;

        TcpListener(TcpListener&&) = delete;
        TcpListener& operator=(TcpListener&&) = delete;

        [[nodiscard]]
        bool IsValid() const noexcept;

        [[nodiscard]]
        SOCKET NativeHandle() const noexcept;

        [[nodiscard]]
        bool IsDualStackEnabled() const;

        void BindAndListen(
            unsigned short port
        );

        [[nodiscard]]
        TcpConnection Accept();

        [[nodiscard]]
        bool IsListening() const noexcept;

        [[nodiscard]]
        unsigned short BoundPort() const noexcept;

    private:
        SOCKET socket_ = INVALID_SOCKET;
        bool isListening_ = false;
        unsigned short boundPort_ = 0;
    };
}