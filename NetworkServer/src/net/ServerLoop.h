#pragma once

#include "net/SelectLoop.h"

#include <chrono>
#include <functional>

namespace tdr::net
{
    class ServerCoordinator;
    class TcpListener;

    class ServerLoop final
    {
    public:
        ServerLoop(
            TcpListener& listener,
            ServerCoordinator& coordinator
        );

        void PollOnce(
            std::chrono::milliseconds timeout
        );

        void RunUntil(
            const std::function<bool()>& shouldStop,
            std::chrono::milliseconds pollTimeout
        );

    private:
        TcpListener& listener_;
        ServerCoordinator& coordinator_;
        SelectLoop selectLoop_;
    };
}