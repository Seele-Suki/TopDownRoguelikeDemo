#include "protocol/MessageType.h"
#include "protocol/PlayerShotgunEventCodec.h"

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
        const char* const message
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
        const char* const message
    )
    {
        if (std::fabs(actual - expected) > 0.0001F)
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
        bool rejected = false;

        try
        {
            action();
        }
        catch (const std::invalid_argument&)
        {
            rejected = true;
        }

        Require(rejected, message);
    }

    void MessageTypeUsesValue37AndIsUdp()
    {
        using namespace tdr::protocol;

        Require(
            static_cast<std::uint16_t>(
                MessageType::PlayerShotgunEvent) == 37U,
            "PlayerShotgunEvent must use value 37."
        );

        Require(
            IsKnownMessageType(
                MessageType::PlayerShotgunEvent),
            "PlayerShotgunEvent was not recognized."
        );

        Require(
            IsUdpMessageType(
                MessageType::PlayerShotgunEvent),
            "PlayerShotgunEvent was not classified as UDP."
        );
    }

    void CodecUsesStableWireLayout()
    {
        using namespace tdr::protocol;

        PlayerShotgunEvent source{};
        source.playerId = 0x01020304U;
        source.volleySequence = 0x05060708U;
        source.originX = 1.5F;
        source.originY = -2.25F;
        source.centerDirectionX = 0.6F;
        source.centerDirectionY = 0.8F;
        source.projectileCount = 5U;
        source.spreadAngle = 40.0F;
        source.effectiveCooldown = 4.0F;

        const std::vector<std::uint8_t> expected{
            0x01U, 0x02U, 0x03U, 0x04U,
            0x05U, 0x06U, 0x07U, 0x08U,
            0x3FU, 0xC0U, 0x00U, 0x00U,
            0xC0U, 0x10U, 0x00U, 0x00U,
            0x3FU, 0x19U, 0x99U, 0x9AU,
            0x3FU, 0x4CU, 0xCCU, 0xCDU,
            0x00U, 0x00U, 0x00U, 0x05U,
            0x42U, 0x20U, 0x00U, 0x00U,
            0x40U, 0x80U, 0x00U, 0x00U
        };

        const auto encoded =
            PlayerShotgunEventCodec::Encode(
                source);

        Require(
            encoded == expected,
            "Shotgun event wire layout did not match."
        );

        const PlayerShotgunEvent decoded =
            PlayerShotgunEventCodec::Decode(
                encoded.data(),
                encoded.size());

        Require(decoded.playerId == source.playerId,
            "Player ID did not round trip.");

        Require(decoded.volleySequence == source.volleySequence,
            "Volley sequence did not round trip.");

        RequireNear(decoded.originX, source.originX,
            "Origin X did not round trip.");

        RequireNear(decoded.originY, source.originY,
            "Origin Y did not round trip.");

        RequireNear(
            decoded.centerDirectionX,
            source.centerDirectionX,
            "Center direction X did not round trip.");

        RequireNear(
            decoded.centerDirectionY,
            source.centerDirectionY,
            "Center direction Y did not round trip.");

        Require(
            decoded.projectileCount == source.projectileCount,
            "Projectile count did not round trip.");

        RequireNear(decoded.spreadAngle, source.spreadAngle,
            "Spread angle did not round trip.");

        RequireNear(
            decoded.effectiveCooldown,
            source.effectiveCooldown,
            "Effective cooldown did not round trip.");
    }

    void CodecRejectsInvalidValues()
    {
        using namespace tdr::protocol;

        PlayerShotgunEvent invalid{};
        invalid.playerId = 7U;
        invalid.centerDirectionX = 1.0F;
        invalid.projectileCount = 5U;
        invalid.spreadAngle = 40.0F;
        invalid.effectiveCooldown = 4.0F;

        invalid.projectileCount = 0U;

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Encode(invalid));
            },
            "Zero projectile count was accepted."
        );

        invalid.projectileCount = 33U;

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Encode(invalid));
            },
            "Excessive projectile count was accepted."
        );

        invalid.projectileCount = 5U;
        invalid.spreadAngle = 181.0F;

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Encode(invalid));
            },
            "Excessive spread angle was accepted."
        );

        invalid.spreadAngle = 40.0F;
        invalid.effectiveCooldown = -1.0F;

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Encode(invalid));
            },
            "Negative cooldown was accepted."
        );
    }

    void CodecRejectsMalformedPayloads()
    {
        using namespace tdr::protocol;

        const std::vector<std::uint8_t> tooShort(
            kPlayerShotgunEventPayloadSize - 1U,
            0U);

        const std::vector<std::uint8_t> trailing(
            kPlayerShotgunEventPayloadSize + 1U,
            0U);

        RequireInvalidArgument(
            [&tooShort]()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Decode(
                        tooShort.data(),
                        tooShort.size()));
            },
            "Truncated shotgun payload was accepted."
        );

        RequireInvalidArgument(
            [&trailing]()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Decode(
                        trailing.data(),
                        trailing.size()));
            },
            "Trailing shotgun bytes were accepted."
        );

        RequireInvalidArgument(
            []()
            {
                static_cast<void>(
                    PlayerShotgunEventCodec::Decode(
                        nullptr,
                        kPlayerShotgunEventPayloadSize));
            },
            "Null shotgun payload was accepted."
        );
    }
}

int main()
{
    try
    {
        MessageTypeUsesValue37AndIsUdp();
        CodecUsesStableWireLayout();
        CodecRejectsInvalidValues();
        CodecRejectsMalformedPayloads();

        std::cout
            << "[PASS] Player shotgun event codec tests."
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