#include "protocol/PlayerShotgunEventCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <cmath>
#include <cstring>
#include <stdexcept>

namespace
{
    using tdr::protocol::PlayerShotgunEvent;

    void Validate(
        const PlayerShotgunEvent& shotgunEvent
    )
    {
        if (shotgunEvent.playerId == 0U)
        {
            throw std::invalid_argument(
                "Player shotgun event ID must be non-zero."
            );
        }

        if (!std::isfinite(shotgunEvent.originX) ||
            !std::isfinite(shotgunEvent.originY) ||
            !std::isfinite(
                shotgunEvent.centerDirectionX) ||
            !std::isfinite(
                shotgunEvent.centerDirectionY) ||
            !std::isfinite(shotgunEvent.spreadAngle) ||
            !std::isfinite(
                shotgunEvent.effectiveCooldown))
        {
            throw std::invalid_argument(
                "Player shotgun event contains "
                "a non-finite value."
            );
        }

        const float directionMagnitudeSquared =
            shotgunEvent.centerDirectionX *
            shotgunEvent.centerDirectionX +
            shotgunEvent.centerDirectionY *
            shotgunEvent.centerDirectionY;

        if (directionMagnitudeSquared < 0.0001F)
        {
            throw std::invalid_argument(
                "Player shotgun event direction "
                "cannot be zero."
            );
        }

        if (shotgunEvent.projectileCount == 0U ||
            shotgunEvent.projectileCount >
            tdr::protocol::
            kMaxShotgunProjectileCount)
        {
            throw std::invalid_argument(
                "Player shotgun projectile count "
                "is outside the supported range."
            );
        }

        if (shotgunEvent.spreadAngle < 0.0F ||
            shotgunEvent.spreadAngle > 180.0F)
        {
            throw std::invalid_argument(
                "Player shotgun spread angle "
                "is outside [0, 180]."
            );
        }

        if (shotgunEvent.effectiveCooldown < 0.0F)
        {
            throw std::invalid_argument(
                "Player shotgun cooldown cannot be negative."
            );
        }
    }

    void AppendNetwork32(
        std::vector<std::uint8_t>& output,
        const std::uint32_t value
    )
    {
        const std::uint32_t networkValue =
            tdr::protocol::HostToNetwork32(
                value);

        const auto* bytes =
            reinterpret_cast<
            const std::uint8_t*>(
                &networkValue);

        output.insert(
            output.end(),
            bytes,
            bytes + sizeof(networkValue));
    }

    void AppendNetworkFloat(
        std::vector<std::uint8_t>& output,
        const float value
    )
    {
        std::uint32_t bits = 0U;

        std::memcpy(
            &bits,
            &value,
            sizeof(bits));

        AppendNetwork32(
            output,
            bits);
    }

    std::uint32_t ReadNetwork32(
        const std::uint8_t* data,
        std::size_t& offset
    )
    {
        std::uint32_t networkValue = 0U;

        std::memcpy(
            &networkValue,
            data + offset,
            sizeof(networkValue));

        offset += sizeof(networkValue);

        return tdr::protocol::NetworkToHost32(
            networkValue);
    }

    float ReadNetworkFloat(
        const std::uint8_t* data,
        std::size_t& offset
    )
    {
        const std::uint32_t bits =
            ReadNetwork32(
                data,
                offset);

        float value = 0.0F;

        std::memcpy(
            &value,
            &bits,
            sizeof(value));

        return value;
    }
}

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        PlayerShotgunEventCodec::Encode(
            const PlayerShotgunEvent& shotgunEvent
        )
    {
        Validate(shotgunEvent);

        std::vector<std::uint8_t> encoded;

        encoded.reserve(
            kPlayerShotgunEventPayloadSize);

        AppendNetwork32(
            encoded,
            shotgunEvent.playerId);

        AppendNetwork32(
            encoded,
            shotgunEvent.volleySequence);

        AppendNetworkFloat(
            encoded,
            shotgunEvent.originX);

        AppendNetworkFloat(
            encoded,
            shotgunEvent.originY);

        AppendNetworkFloat(
            encoded,
            shotgunEvent.centerDirectionX);

        AppendNetworkFloat(
            encoded,
            shotgunEvent.centerDirectionY);

        AppendNetwork32(
            encoded,
            shotgunEvent.projectileCount);

        AppendNetworkFloat(
            encoded,
            shotgunEvent.spreadAngle);

        AppendNetworkFloat(
            encoded,
            shotgunEvent.effectiveCooldown);

        return encoded;
    }

    PlayerShotgunEvent
        PlayerShotgunEventCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "Player shotgun event payload cannot be null."
            );
        }

        if (size != kPlayerShotgunEventPayloadSize)
        {
            throw std::invalid_argument(
                "Player shotgun event payload "
                "has an invalid size."
            );
        }

        std::size_t offset = 0U;
        PlayerShotgunEvent shotgunEvent{};

        shotgunEvent.playerId =
            ReadNetwork32(data, offset);

        shotgunEvent.volleySequence =
            ReadNetwork32(data, offset);

        shotgunEvent.originX =
            ReadNetworkFloat(data, offset);

        shotgunEvent.originY =
            ReadNetworkFloat(data, offset);

        shotgunEvent.centerDirectionX =
            ReadNetworkFloat(data, offset);

        shotgunEvent.centerDirectionY =
            ReadNetworkFloat(data, offset);

        shotgunEvent.projectileCount =
            ReadNetwork32(data, offset);

        shotgunEvent.spreadAngle =
            ReadNetworkFloat(data, offset);

        shotgunEvent.effectiveCooldown =
            ReadNetworkFloat(data, offset);

        Validate(shotgunEvent);

        return shotgunEvent;
    }
}