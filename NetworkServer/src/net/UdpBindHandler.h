#pragma once

#include "net/ServerCoordinator.h"

#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::net
{
    class UdpBindHandler final
    {
    public:
        explicit UdpBindHandler(
            ServerCoordinator& coordinator
        ) noexcept;

        [[nodiscard]]
        std::vector<std::uint8_t> Handle(
            const std::uint8_t* data,
            std::size_t size,
            const sockaddr_in6& sourceAddress
        );

    private:
        ServerCoordinator& coordinator_;
    };
}