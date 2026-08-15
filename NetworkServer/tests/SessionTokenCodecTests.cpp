#include "protocol/SessionTokenCodec.h"

#include <array>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>

namespace
{
    void Require(
        const bool condition,
        const char* const message
    )
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    template<typename Action>
    void RequireInvalidArgument(
        Action action,
        const char* const message
    )
    {
        try
        {
            action();
        }
        catch (const std::invalid_argument&)
        {
            return;
        }

        throw std::runtime_error(message);
    }

    void KnownHexTokenRoundTrips()
    {
        using tdr::protocol::SessionTokenCodec;

        const std::string hexToken =
            "00112233445566778899aabbccddeeff";

        const std::array<std::uint8_t, 16> expected
        {
            0x00, 0x11, 0x22, 0x33,
            0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xAA, 0xBB,
            0xCC, 0xDD, 0xEE, 0xFF
        };

        const auto bytes =
            SessionTokenCodec::DecodeHex(hexToken);

        Require(
            bytes == expected,
            "Hex token decoded to incorrect bytes."
        );

        Require(
            SessionTokenCodec::EncodeHex(bytes) == hexToken,
            "Token bytes encoded to incorrect hex."
        );
    }

    void InvalidHexTokensAreRejected()
    {
        using tdr::protocol::SessionTokenCodec;

        RequireInvalidArgument(
            []()
            {
                static_cast<void>(
                    SessionTokenCodec::DecodeHex(
                        "00112233445566778899aabbccddee"
                    )
                    );
            },
            "A short session token was accepted."
        );

        RequireInvalidArgument(
            []()
            {
                static_cast<void>(
                    SessionTokenCodec::DecodeHex(
                        "00112233445566778899aabbccddeefg"
                    )
                    );
            },
            "A session token with invalid hex was accepted."
        );
    }
}

int main()
{
    try
    {
        KnownHexTokenRoundTrips();
        InvalidHexTokensAreRejected();

        std::cout
            << "[PASS] Session token codec tests finished."
            << std::endl;

        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr
            << "[FAIL] "
            << exception.what()
            << std::endl;

        return 1;
    }
}