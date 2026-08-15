#include "protocol/SessionTokenCodec.h"

#include <stdexcept>

namespace
{
    std::uint8_t DecodeHexDigit(
        const char character
    )
    {
        if (character >= '0'
            && character <= '9')
        {
            return static_cast<std::uint8_t>(
                character - '0'
                );
        }

        if (character >= 'a'
            && character <= 'f')
        {
            return static_cast<std::uint8_t>(
                character - 'a' + 10
                );
        }

        throw std::invalid_argument(
            "Session token must use lowercase hexadecimal."
        );
    }
}

namespace tdr::protocol
{
    std::array<
        std::uint8_t,
        kUdpSessionTokenSize
    > SessionTokenCodec::DecodeHex(
        const std::string& hexToken
    )
    {
        if (hexToken.size()
            != kUdpSessionTokenSize * 2U)
        {
            throw std::invalid_argument(
                "Session token must contain 32 hex characters."
            );
        }

        std::array<
            std::uint8_t,
            kUdpSessionTokenSize
        > tokenBytes{};

        for (std::size_t index = 0;
            index < tokenBytes.size();
            ++index)
        {
            const std::uint8_t high =
                DecodeHexDigit(hexToken[index * 2U]);

            const std::uint8_t low =
                DecodeHexDigit(
                    hexToken[index * 2U + 1U]
                );

            tokenBytes[index] =
                static_cast<std::uint8_t>(
                    (high << 4U) | low
                    );
        }

        return tokenBytes;
    }

    std::string SessionTokenCodec::EncodeHex(
        const std::array<
        std::uint8_t,
        kUdpSessionTokenSize
        >& tokenBytes
    )
    {
        constexpr char hexDigits[] =
            "0123456789abcdef";

        std::string hexToken;
        hexToken.resize(tokenBytes.size() * 2U);

        for (std::size_t index = 0;
            index < tokenBytes.size();
            ++index)
        {
            const std::uint8_t value =
                tokenBytes[index];

            hexToken[index * 2U] =
                hexDigits[(value >> 4U) & 0x0FU];

            hexToken[index * 2U + 1U] =
                hexDigits[value & 0x0FU];
        }

        return hexToken;
    }
}