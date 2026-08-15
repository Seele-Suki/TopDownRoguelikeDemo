#pragma once

#include "protocol/MessageType.h"

#include <cstddef>
#include <cstdint>

namespace tdr::protocol
{
    inline constexpr std::uint32_t kProtocolMagic =
        0x54445231;

    inline constexpr std::uint16_t kProtocolVersion =
        1;

    inline constexpr std::size_t kMagicOffset =
        0;

    inline constexpr std::size_t kVersionOffset =
        4;

    inline constexpr std::size_t kMessageTypeOffset =
        6;

    inline constexpr std::size_t kPayloadSizeOffset =
        8;

    inline constexpr std::size_t kMessageHeaderSize =
        12;

    inline constexpr std::uint32_t kMaxPayloadSize =
        64U * 1024U;

    inline constexpr std::size_t kMaxPacketSize =
        kMessageHeaderSize
        + static_cast<std::size_t>(kMaxPayloadSize);

    inline constexpr std::size_t kMaxReceiveBufferSize =
        kMaxPacketSize * 2U;

    [[nodiscard]]
    constexpr bool IsValidPayloadSize(
        const std::uint32_t payloadSize
    ) noexcept
    {
        return payloadSize <= kMaxPayloadSize;
    }

    struct MessageHeader final
    {
        std::uint32_t magic = kProtocolMagic;
        std::uint16_t version = kProtocolVersion;
        MessageType type = MessageType::Invalid;
        std::uint32_t payloadSize = 0;
    };
}