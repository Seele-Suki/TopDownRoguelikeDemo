#include "protocol/PlayerStateSnapshotCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <algorithm>
#include <cmath>
#include <cstring>
#include <stdexcept>
#include <utility>

namespace
{
    using tdr::protocol::PlayerStateRecord;

    static_assert(
        sizeof(float) == sizeof(std::uint32_t),
        "Player synchronization requires 32-bit floats."
        );

    void ValidatePlayerCount(
        const std::size_t count
    )
    {
        if (count == 0U ||
            count >
            tdr::protocol::kMaxPlayerStateRecords)
        {
            throw std::invalid_argument(
                "Player state snapshot has an invalid player count."
            );
        }
    }

    void ValidateOrderedPlayers(
        const std::vector<PlayerStateRecord>& players
    )
    {
        for (std::size_t index = 0U;
            index < players.size();
            ++index)
        {
            const PlayerStateRecord& player =
                players[index];

            if (player.playerId == 0U)
            {
                throw std::invalid_argument(
                    "Player state ID must be non-zero."
                );
            }

            if (!std::isfinite(player.positionX) ||
                !std::isfinite(player.positionY) ||
                !std::isfinite(player.aimX) ||
                !std::isfinite(player.aimY))
            {
                throw std::invalid_argument(
                    "Player state contains a non-finite value."
                );
            }

            if (index == 0U)
            {
                continue;
            }

            const std::uint32_t previousId =
                players[index - 1U].playerId;

            if (previousId == player.playerId)
            {
                throw std::invalid_argument(
                    "Player state contains duplicate IDs."
                );
            }

            if (previousId > player.playerId)
            {
                throw std::invalid_argument(
                    "Player states are not ordered by ID."
                );
            }
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
        PlayerStateSnapshotCodec::Encode(
            const PlayerStateSnapshotPayload& snapshot
        )
    {
        ValidatePlayerCount(
            snapshot.players.size()
        );

        std::vector<PlayerStateRecord> orderedPlayers =
            snapshot.players;

        std::sort(
            orderedPlayers.begin(),
            orderedPlayers.end(),
            [](const PlayerStateRecord& left,
                const PlayerStateRecord& right)
            {
                return left.playerId < right.playerId;
            }
        );

        ValidateOrderedPlayers(
            orderedPlayers
        );

        const std::size_t payloadSize =
            kPlayerStateSnapshotPrefixSize +
            orderedPlayers.size() *
            kPlayerStateRecordSize;

        std::vector<std::uint8_t> encoded;
        encoded.reserve(payloadSize);

        AppendNetwork32(
            encoded,
            static_cast<std::uint32_t>(
                orderedPlayers.size()
                )
        );

        for (const PlayerStateRecord& player :
            orderedPlayers)
        {
            AppendNetwork32(
                encoded,
                player.playerId
            );

            AppendNetworkFloat(
                encoded,
                player.positionX
            );

            AppendNetworkFloat(
                encoded,
                player.positionY
            );

            AppendNetworkFloat(
                encoded,
                player.aimX
            );

            AppendNetworkFloat(
                encoded,
                player.aimY
            );

            AppendNetwork32(encoded, 0U);
        }

        return encoded;
    }

    PlayerStateSnapshotPayload
        PlayerStateSnapshotCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "Player state snapshot payload cannot be null."
            );
        }

        if (size < kPlayerStateSnapshotPrefixSize)
        {
            throw std::invalid_argument(
                "Player state snapshot payload is truncated."
            );
        }

        std::size_t offset = 0U;

        const std::uint32_t playerCount =
            ReadNetwork32(data, offset);

        ValidatePlayerCount(playerCount);

        const std::size_t expectedSize =
            kPlayerStateSnapshotPrefixSize +
            static_cast<std::size_t>(playerCount) *
            kPlayerStateRecordSize;

        if (size != expectedSize)
        {
            throw std::invalid_argument(
                "Player state snapshot payload has an invalid size."
            );
        }

        PlayerStateSnapshotPayload snapshot{};
        snapshot.players.reserve(playerCount);

        for (std::uint32_t index = 0U;
            index < playerCount;
            ++index)
        {
            PlayerStateRecord player{};

            player.playerId =
                ReadNetwork32(data, offset);

            player.positionX =
                ReadNetworkFloat(data, offset);

            player.positionY =
                ReadNetworkFloat(data, offset);

            player.aimX =
                ReadNetworkFloat(data, offset);

            player.aimY =
                ReadNetworkFloat(data, offset);

            const std::uint32_t reserved =
                ReadNetwork32(data, offset);

            if (reserved != 0U)
            {
                throw std::invalid_argument(
                    "Player state reserved field must be zero."
                );
            }

            snapshot.players.push_back(
                std::move(player)
            );
        }

        ValidateOrderedPlayers(
            snapshot.players
        );

        return snapshot;
    }
}