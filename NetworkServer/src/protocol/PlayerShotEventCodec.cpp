#include "protocol/PlayerShotEventCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <cmath>
#include <cstring>
#include <stdexcept>
#include <vector>

namespace
{
    using tdr::protocol::PlayerShotEvent;

    void ValidateShotEvent(
        const PlayerShotEvent& shotEvent
    )
    {
        if (shotEvent.playerId == 0U)
        {
            throw std::invalid_argument(
                "Player shot event ID must be non-zero."
            );
        }

        if (!std::isfinite(shotEvent.originX) ||
            !std::isfinite(shotEvent.originY) ||
            !std::isfinite(shotEvent.directionX) ||
            !std::isfinite(shotEvent.directionY))
        {
            throw std::invalid_argument(
                "Player shot event contains a non-finite value."
            );
        }

        if (shotEvent.directionX == 0.0F &&
            shotEvent.directionY == 0.0F)
        {
            throw std::invalid_argument(
                "Player shot event direction cannot be zero."
            );
        }
    }

    void AppendNetwork32(
        std::vector<std::uint8_t>& output,
        const std::uint32_t value
    )
    {
        const std::uint32_t networkValue =
            tdr::protocol::HostToNetwork32(value);

        const auto* bytes =
            reinterpret_cast<const std::uint8_t*>(
                &networkValue
                );

        output.insert(
            output.end(),
            bytes,
            bytes + sizeof(networkValue)
        );
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
            sizeof(bits)
        );

        AppendNetwork32(
            output,
            bits
        );
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
            sizeof(networkValue)
        );

        offset += sizeof(networkValue);

        return tdr::protocol::NetworkToHost32(
            networkValue
        );
    }

    float ReadNetworkFloat(
        const std::uint8_t* data,
        std::size_t& offset
    )
    {
        const std::uint32_t bits =
            ReadNetwork32(
                data,
                offset
            );

        float value = 0.0F;

        std::memcpy(
            &value,
            &bits,
            sizeof(value)
        );

        return value;
    }
}

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        PlayerShotEventCodec::Encode(
            const PlayerShotEvent& shotEvent
        )
    {
        ValidateShotEvent(
            shotEvent
        );

        std::vector<std::uint8_t> encoded;

        encoded.reserve(
            kPlayerShotEventPayloadSize
        );

        AppendNetwork32(
            encoded,
            shotEvent.playerId
        );

        AppendNetwork32(
            encoded,
            shotEvent.shotSequence
        );

        AppendNetworkFloat(
            encoded,
            shotEvent.originX
        );

        AppendNetworkFloat(
            encoded,
            shotEvent.originY
        );

        AppendNetworkFloat(
            encoded,
            shotEvent.directionX
        );

        AppendNetworkFloat(
            encoded,
            shotEvent.directionY
        );

        return encoded;
    }

    PlayerShotEvent
        PlayerShotEventCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "Player shot event payload cannot be null."
            );
        }

        if (size != kPlayerShotEventPayloadSize)
        {
            throw std::invalid_argument(
                "Player shot event payload has an invalid size."
            );
        }

        std::size_t offset = 0U;

        PlayerShotEvent shotEvent{};

        shotEvent.playerId =
            ReadNetwork32(
                data,
                offset
            );

        shotEvent.shotSequence =
            ReadNetwork32(
                data,
                offset
            );

        shotEvent.originX =
            ReadNetworkFloat(
                data,
                offset
            );

        shotEvent.originY =
            ReadNetworkFloat(
                data,
                offset
            );

        shotEvent.directionX =
            ReadNetworkFloat(
                data,
                offset
            );

        shotEvent.directionY =
            ReadNetworkFloat(
                data,
                offset
            );

        ValidateShotEvent(
            shotEvent
        );

        return shotEvent;
    }
}