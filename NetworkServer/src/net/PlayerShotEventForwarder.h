#pragma once

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::net
{
    class ServerCoordinator;

    struct ForwardedPlayerShotDatagram final
    {
        std::vector<std::uint8_t> bytes;
        sockaddr_in6 destination{};
    };

    class PlayerShotEventForwarder final
    {
    public:
        explicit PlayerShotEventForwarder(
            ServerCoordinator& coordinator
        ) noexcept;

        [[nodiscard]]
        ForwardedPlayerShotDatagram Forward(
            const std::uint8_t* data,
            std::size_t size,
            const sockaddr_in6& sourceAddress
        );

    private:
        ServerCoordinator& coordinator_;
    };
}