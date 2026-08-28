#pragma once

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::net
{
    class ServerCoordinator;

    struct ForwardedPlayerStateDatagram final
    {
        std::vector<std::uint8_t> bytes;
        sockaddr_in6 destination{};
    };

    class PlayerStateForwarder final
    {
    public:
        explicit PlayerStateForwarder(
            ServerCoordinator& coordinator
        ) noexcept;

        [[nodiscard]]
        ForwardedPlayerStateDatagram Forward(
            const std::uint8_t* data,
            std::size_t size,
            const sockaddr_in6& sourceAddress
        );

    private:
        ServerCoordinator& coordinator_;
    };
}