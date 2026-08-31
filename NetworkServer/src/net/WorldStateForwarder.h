#pragma once

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::net
{
    class ServerCoordinator;

    struct ForwardedWorldStateDatagram final
    {
        std::vector<std::uint8_t> bytes;
        sockaddr_in6 destination{};
    };

    class WorldStateForwarder final
    {
    public:
        explicit WorldStateForwarder(
            ServerCoordinator& coordinator
        ) noexcept;

        [[nodiscard]]
        ForwardedWorldStateDatagram Forward(
            const std::uint8_t* data,
            std::size_t size,
            const sockaddr_in6& sourceAddress
        );

    private:
        ServerCoordinator& coordinator_;
    };
}
