#include "protocol/PlayerShotEventCodec.h"

#include <cmath>
#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <vector>

namespace
{
    void Require(
        const bool condition,
        const char* message
    )
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    void RequireNear(
        const float actual,
        const float expected,
        const float epsilon,
        const char* message
    )
    {
        if (std::fabs(actual - expected) > epsilon)
        {
            throw std::runtime_error(message);
        }
    }
}

void EncodeDecodePreservesAllFields()
{
    tdr::protocol::PlayerShotEvent source{};
    source.playerId = 7U;
    source.shotSequence = 42U;
    source.originX = 1.25F;
    source.originY = -2.5F;
    source.directionX = 0.6F;
    source.directionY = 0.8F;

    const std::vector<std::uint8_t> encoded =
        tdr::protocol::PlayerShotEventCodec::Encode(
            source
        );

    Require(
        encoded.size() ==
        tdr::protocol::kPlayerShotEventPayloadSize,
        "Encoded shot event size did not match."
    );

    const tdr::protocol::PlayerShotEvent decoded =
        tdr::protocol::PlayerShotEventCodec::Decode(
            encoded.data(),
            encoded.size()
        );

    Require(
        decoded.playerId == 7U,
        "Decoded player ID did not match."
    );

    Require(
        decoded.shotSequence == 42U,
        "Decoded shot sequence did not match."
    );

    RequireNear(
        decoded.originX,
        1.25F,
        0.0001F,
        "Decoded origin X did not match."
    );

    RequireNear(
        decoded.originY,
        -2.5F,
        0.0001F,
        "Decoded origin Y did not match."
    );

    RequireNear(
        decoded.directionX,
        0.6F,
        0.0001F,
        "Decoded direction X did not match."
    );

    RequireNear(
        decoded.directionY,
        0.8F,
        0.0001F,
        "Decoded direction Y did not match."
    );
}

void DecodeRejectsInvalidSize()
{
    const std::vector<std::uint8_t> tooShort(
        tdr::protocol::kPlayerShotEventPayloadSize - 1U,
        0U
    );

    bool rejected = false;

    try
    {
        tdr::protocol::PlayerShotEventCodec::Decode(
            tooShort.data(),
            tooShort.size()
        );
    }
    catch (const std::invalid_argument&)
    {
        rejected = true;
    }

    Require(
        rejected,
        "Truncated shot event payload was accepted."
    );
}

int main()
{
    try
    {
        EncodeDecodePreservesAllFields();
        DecodeRejectsInvalidSize();

        std::cout
            << "PlayerShotEventCodecTests passed."
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