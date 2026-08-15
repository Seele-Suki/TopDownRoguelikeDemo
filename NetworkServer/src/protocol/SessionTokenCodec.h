#pragma once

#include "protocol/UdpMessageHeader.h"

#include <array>
#include <cstdint>
#include <string>

namespace tdr::protocol
{
    class SessionTokenCodec final
    {
    public:
        [[nodiscard]]
        static std::array<
            std::uint8_t,
            kUdpSessionTokenSize
        > DecodeHex(
            const std::string& hexToken
        );

        [[nodiscard]]
        static std::string EncodeHex(
            const std::array<
            std::uint8_t,
            kUdpSessionTokenSize
            >& tokenBytes
        );
    };
}