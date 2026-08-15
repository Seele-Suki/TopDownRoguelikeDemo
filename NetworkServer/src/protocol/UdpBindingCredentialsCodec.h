#pragma once

#include "protocol/UdpMessageHeader.h"

#include <array>
#include <cstddef>
#include <cstdint>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::size_t
        kUdpBindingPlayerIdOffset = 0U;

    inline constexpr std::size_t
        kUdpBindingTokenOffset = 4U;

    inline constexpr std::size_t
        kUdpBindingCredentialsSize = 20U;

    struct UdpBindingCredentials final
    {
        std::uint32_t playerId = 0U;

        std::array<
            std::uint8_t,
            kUdpSessionTokenSize
        > sessionToken{};
    };

    class UdpBindingCredentialsCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const UdpBindingCredentials& credentials
        );

        [[nodiscard]]
        static UdpBindingCredentials Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}
