#pragma once

#include "net/SelectLoop.h"
#include "net/UdpBindHandler.h"
#include "net/UdpPingHandler.h"
#include "net/PlayerInputForwarder.h"
#include "net/PlayerStateForwarder.h"
#include "net/PlayerShotEventForwarder.h"

#include <chrono>
#include <functional>

namespace tdr::net
{
    class ServerCoordinator;
    class TcpListener;
    class UdpSocket;

    class ServerLoop final
    {
    public:
        ServerLoop(
            TcpListener& listener,
            UdpSocket& udpSocket,
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
        UdpSocket& udpSocket_;
        ServerCoordinator& coordinator_;
        UdpBindHandler udpBindHandler_;
        UdpPingHandler udpPingHandler_;
        PlayerStateForwarder playerStateForwarder_;
        PlayerInputForwarder playerInputForwarder_;
        PlayerShotEventForwarder playerShotEventForwarder_;
        SelectLoop selectLoop_;
    };
}
