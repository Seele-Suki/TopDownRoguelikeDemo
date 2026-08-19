#include "protocol/RoomStateSnapshotCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <cstring>
#include <limits>
#include <stdexcept>
#include <utility>

namespace
{
    using tdr::protocol::RoomPlayerSnapshot;
    using tdr::protocol::RoomStateSnapshot;

    void RequireAvailable(
        const std::size_t offset,
        const std::size_t requiredSize,
        const std::size_t totalSize
    )
    {
        if (offset > totalSize ||
            requiredSize > totalSize - offset)
        {
            throw std::invalid_argument(
                "Room snapshot payload is truncated."
            );
        }
    }

    void AppendNetwork16(
        std::vector<std::uint8_t>& output,
        const std::uint16_t value
    )
    {
        const std::uint16_t networkValue =
            tdr::protocol::HostToNetwork16(value);

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

    void AppendString(
        std::vector<std::uint8_t>& output,
        const std::string& value
    )
    {
        if (value.size() >
            std::numeric_limits<std::uint16_t>::max())
        {
            throw std::invalid_argument(
                "Room snapshot string is too long."
            );
        }

        AppendNetwork16(
            output,
            static_cast<std::uint16_t>(
                value.size()
                )
        );

        output.insert(
            output.end(),
            value.begin(),
            value.end()
        );
    }

    std::uint8_t ReadByte(
        const std::uint8_t* const data,
        const std::size_t size,
        std::size_t& offset
    )
    {
        RequireAvailable(
            offset,
            sizeof(std::uint8_t),
            size
        );

        return data[offset++];
    }

    std::uint16_t ReadNetwork16(
        const std::uint8_t* const data,
        const std::size_t size,
        std::size_t& offset
    )
    {
        RequireAvailable(
            offset,
            sizeof(std::uint16_t),
            size
        );

        std::uint16_t networkValue{};

        std::memcpy(
            &networkValue,
            data + offset,
            sizeof(networkValue)
        );

        offset += sizeof(networkValue);

        return tdr::protocol::NetworkToHost16(
            networkValue
        );
    }

    std::uint32_t ReadNetwork32(
        const std::uint8_t* const data,
        const std::size_t size,
        std::size_t& offset
    )
    {
        RequireAvailable(
            offset,
            sizeof(std::uint32_t),
            size
        );

        std::uint32_t networkValue{};

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

    std::string ReadString(
        const std::uint8_t* const data,
        const std::size_t size,
        std::size_t& offset
    )
    {
        const std::uint16_t length =
            ReadNetwork16(
                data,
                size,
                offset
            );

        RequireAvailable(
            offset,
            length,
            size
        );

        std::string value(
            reinterpret_cast<const char*>(
                data + offset
                ),
            length
        );

        offset += length;

        return value;
    }

    void ValidateSnapshot(
        const RoomStateSnapshot& snapshot
    )
    {
        if (snapshot.roomId.empty())
        {
            throw std::invalid_argument(
                "Room snapshot room ID cannot be empty."
            );
        }

        if (snapshot.roomStatus > 1U)
        {
            throw std::invalid_argument(
                "Room snapshot contains an invalid status."
            );
        }

        if (snapshot.difficultyId > 3U)
        {
            throw std::invalid_argument(
                "Room snapshot contains an invalid difficulty."
            );
        }

        if (snapshot.players.empty() ||
            snapshot.players.size() >
            tdr::protocol::kMaxRoomSnapshotPlayers)
        {
            throw std::invalid_argument(
                "Room snapshot contains an invalid player count."
            );
        }

        std::size_t hostCount = 0U;

        for (std::size_t index = 0U;
            index < snapshot.players.size();
            ++index)
        {
            const RoomPlayerSnapshot& player =
                snapshot.players[index];

            if (player.isHost)
            {
                ++hostCount;
            }

            if (player.playerId == 0U)
            {
                throw std::invalid_argument(
                    "Room snapshot player ID must be non-zero."
                );
            }

            if (player.nickname.empty())
            {
                throw std::invalid_argument(
                    "Room snapshot nickname cannot be empty."
                );
            }

            if (player.characterId > 2U)
            {
                throw std::invalid_argument(
                    "Room snapshot contains an invalid character."
                );
            }

            for (std::size_t otherIndex = index + 1U;
                otherIndex < snapshot.players.size();
                ++otherIndex)
            {
                if (player.playerId ==
                    snapshot.players[otherIndex].playerId)
                {
                    throw std::invalid_argument(
                        "Room snapshot contains duplicate player IDs."
                    );
                }
            }
        }

        if (hostCount != 1U)
        {
            throw std::invalid_argument(
                "Room snapshot must contain exactly one host."
            );
        }
    }
}

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        RoomStateSnapshotCodec::Encode(
            const RoomStateSnapshot& snapshot
        )
    {
        ValidateSnapshot(snapshot);

        std::vector<std::uint8_t> encoded;

        AppendString(
            encoded,
            snapshot.roomId
        );

        encoded.push_back(
            snapshot.roomStatus
        );

        encoded.push_back(
            snapshot.difficultyId
        );

        encoded.push_back(
            static_cast<std::uint8_t>(
                snapshot.players.size()
                )
        );

        for (const RoomPlayerSnapshot& player :
            snapshot.players)
        {
            AppendNetwork32(
                encoded,
                player.playerId
            );

            std::uint8_t flags = 0U;

            if (player.isHost)
            {
                flags |= kRoomPlayerHostFlag;
            }

            if (player.isReady)
            {
                flags |= kRoomPlayerReadyFlag;
            }

            encoded.push_back(flags);
            encoded.push_back(
                player.characterId
            );

            AppendString(
                encoded,
                player.nickname
            );
        }

        return encoded;
    }

    RoomStateSnapshot
        RoomStateSnapshotCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "Room snapshot payload cannot be null."
            );
        }

        std::size_t offset = 0U;
        RoomStateSnapshot snapshot{};

        snapshot.roomId =
            ReadString(
                data,
                size,
                offset
            );

        snapshot.roomStatus =
            ReadByte(
                data,
                size,
                offset
            );

        snapshot.difficultyId =
            ReadByte(
                data,
                size,
                offset
            );

        const std::uint8_t playerCount =
            ReadByte(
                data,
                size,
                offset
            );

        snapshot.players.reserve(
            playerCount
        );

        for (std::uint8_t index = 0U;
            index < playerCount;
            ++index)
        {
            RoomPlayerSnapshot player{};

            player.playerId =
                ReadNetwork32(
                    data,
                    size,
                    offset
                );

            const std::uint8_t flags =
                ReadByte(
                    data,
                    size,
                    offset
                );

            if ((flags &
                static_cast<std::uint8_t>(
                    ~kKnownRoomPlayerFlags
                    )) != 0U)
            {
                throw std::invalid_argument(
                    "Room snapshot contains unknown player flags."
                );
            }

            player.isHost =
                (flags & kRoomPlayerHostFlag) != 0U;

            player.isReady =
                (flags & kRoomPlayerReadyFlag) != 0U;

            player.characterId =
                ReadByte(
                    data,
                    size,
                    offset
                );

            player.nickname =
                ReadString(
                    data,
                    size,
                    offset
                );

            snapshot.players.push_back(
                std::move(player)
            );
        }

        if (offset != size)
        {
            throw std::invalid_argument(
                "Room snapshot payload contains trailing bytes."
            );
        }

        ValidateSnapshot(snapshot);

        return snapshot;
    }
}