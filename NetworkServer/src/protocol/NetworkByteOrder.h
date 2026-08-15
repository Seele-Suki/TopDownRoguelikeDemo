#pragma once

#include <WinSock2.h>

#include <cstdint>

namespace tdr::protocol
{
    [[nodiscard]]
    inline std::uint16_t HostToNetwork16(
        const std::uint16_t value
    ) noexcept
    {
        return ::htons(value);
    }

    [[nodiscard]]
    inline std::uint32_t HostToNetwork32(
        const std::uint32_t value
    ) noexcept
    {
        return ::htonl(value);
    }

    [[nodiscard]]
    inline std::uint16_t NetworkToHost16(
        const std::uint16_t value
    ) noexcept
    {
        return ::ntohs(value);
    }

    [[nodiscard]]
    inline std::uint32_t NetworkToHost32(
        const std::uint32_t value
    ) noexcept
    {
        return ::ntohl(value);
    }
}