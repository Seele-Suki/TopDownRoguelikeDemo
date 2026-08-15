#pragma once

#include "protocol/MessageType.h"

#include <array>
#include <cstddef>
#include <cstdint>

namespace tdr::protocol
{
    inline constexpr std::uint32_t kUdpProtocolMagic =
        0x54445255U;

    inline constexpr std::uint16_t kUdpProtocolVersion =
        1U;

    inline constexpr std::size_t kUdpMagicOffset =
        0U;

    inline constexpr std::size_t kUdpVersionOffset =
        4U;

    inline constexpr std::size_t kUdpMessageTypeOffset =
        6U;

    inline constexpr std::size_t kUdpSessionTokenOffset =
        8U;

    inline constexpr std::size_t kUdpSessionTokenSize =
        16U;

    inline constexpr std::size_t kUdpPlayerIdOffset =
        24U;

    inline constexpr std::size_t kUdpSequenceOffset =
        28U;

    inline constexpr std::size_t kUdpMessageHeaderSize =
        32U;

    struct UdpMessageHeader final
    {
        std::uint32_t magic = kUdpProtocolMagic;
        std::uint16_t version = kUdpProtocolVersion;
        MessageType type = MessageType::Invalid;

        std::array<
            std::uint8_t,
            kUdpSessionTokenSize
        > sessionToken{};

        std::uint32_t playerId = 0U;
        std::uint32_t sequence = 0U;
    };

    static_assert(
        sizeof(UdpMessageHeader) == kUdpMessageHeaderSize,
        "UDP message header must remain 32 bytes."
        );
}