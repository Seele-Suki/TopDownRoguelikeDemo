#include "protocol/PlayerInputCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <cmath>
#include <cstring>
#include <stdexcept>

namespace
{
    using tdr::protocol::PlayerInputPayload;

    static_assert(
        sizeof(float) == sizeof(std::uint32_t),
        "Player synchronization requires 32-bit floats."
        );

    void ValidateInput(
        const PlayerInputPayload& input
    )
    {
        if (!std::isfinite(input.moveX) ||
            !std::isfinite(input.moveY) ||
            !std::isfinite(input.aimX) ||
            !std::isfinite(input.aimY))
        {
            throw std::invalid_argument(
                "Player input contains a non-finite value."
            );
        }

        if (input.moveX < -1.0F ||
            input.moveX > 1.0F ||
            input.moveY < -1.0F ||
            input.moveY > 1.0F)
        {
            throw std::invalid_argument(
                "Player movement component is outside [-1, 1]."
            );
        }

        const float movementMagnitudeSquared =
            input.moveX * input.moveX +
            input.moveY * input.moveY;

        if (movementMagnitudeSquared > 1.0001F)
        {
            throw std::invalid_argument(
                "Player movement magnitude exceeds one."
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

        AppendNetwork32(output, bits);
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
            ReadNetwork32(data, offset);

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
        PlayerInputCodec::Encode(
            const PlayerInputPayload& input
        )
    {
        ValidateInput(input);

        std::vector<std::uint8_t> encoded;
        encoded.reserve(kPlayerInputPayloadSize);

        AppendNetworkFloat(encoded, input.moveX);
        AppendNetworkFloat(encoded, input.moveY);
        AppendNetworkFloat(encoded, input.aimX);
        AppendNetworkFloat(encoded, input.aimY);
        AppendNetwork32(encoded, 0U);

        return encoded;
    }

    PlayerInputPayload
        PlayerInputCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "Player input payload cannot be null."
            );
        }

        if (size != kPlayerInputPayloadSize)
        {
            throw std::invalid_argument(
                "Player input payload has an invalid size."
            );
        }

        std::size_t offset = 0U;
        PlayerInputPayload input{};

        input.moveX =
            ReadNetworkFloat(data, offset);

        input.moveY =
            ReadNetworkFloat(data, offset);

        input.aimX =
            ReadNetworkFloat(data, offset);

        input.aimY =
            ReadNetworkFloat(data, offset);

        const std::uint32_t reserved =
            ReadNetwork32(data, offset);

        if (reserved != 0U)
        {
            throw std::invalid_argument(
                "Player input reserved field must be zero."
            );
        }

        ValidateInput(input);

        return input;
    }
}