#pragma once

#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>

namespace tdr::protocol
{
    inline constexpr std::uint8_t
        kRoomPlayerHostFlag = 0x01U;

    inline constexpr std::uint8_t
        kRoomPlayerReadyFlag = 0x02U;

    inline constexpr std::uint8_t
        kKnownRoomPlayerFlags =
        kRoomPlayerHostFlag |
        kRoomPlayerReadyFlag;

    inline constexpr std::size_t
        kMaxRoomSnapshotPlayers = 4U;

    struct RoomPlayerSnapshot final
    {
        std::uint32_t playerId = 0U;
        bool isHost = false;
        bool isReady = false;
        std::uint8_t characterId = 0U;
        std::string nickname;
    };

    struct RoomStateSnapshot final
    {
        std::string roomId;
        std::uint8_t roomStatus = 0U;
        std::uint8_t difficultyId = 0U;
        std::vector<RoomPlayerSnapshot> players;
    };

    class RoomStateSnapshotCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const RoomStateSnapshot& snapshot
        );

        [[nodiscard]]
        static RoomStateSnapshot Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}