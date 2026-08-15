#pragma once

#include <WinSock2.h>

#include <chrono>
#include <vector>

namespace tdr::net
{
    class SelectResult final
    {
    public:
        SelectResult(
            std::vector<SOCKET> readableSockets,
            bool didTimeout,
            int errorCode
        );

        [[nodiscard]]
        const std::vector<SOCKET>&
            ReadableSockets() const noexcept;

        [[nodiscard]]
        bool DidTimeout() const noexcept;

        [[nodiscard]]
        bool HasError() const noexcept;

        [[nodiscard]]
        int ErrorCode() const noexcept;

    private:
        std::vector<SOCKET> readableSockets_;
        bool didTimeout_ = false;
        int errorCode_ = 0;
    };

    class SelectLoop final
    {
    public:
        void AddSocket(SOCKET socket);
        void RemoveSocket(SOCKET socket);

        [[nodiscard]]
        SelectResult Poll(
            std::chrono::milliseconds timeout
        ) const;

    private:
        std::vector<SOCKET> sockets_;
    };
}