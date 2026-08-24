#pragma once

#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>

namespace tdr::room
{
    enum class CharacterId : std::uint8_t
    {
        None = 0,
        Ranged = 1,
        Melee = 2
    };

    enum class DifficultyId : std::uint8_t
    {
        None = 0,
        Normal = 1,
        Hard = 2,
        Hell = 3
    };

    enum class RoomStatus : std::uint8_t
    {
        Waiting = 0,
        Started = 1
    };

    struct RoomPlayer final
    {
        std::uint32_t playerId = 0;
        std::string nickname;
        bool isHost = false;
        CharacterId selectedCharacter =
            CharacterId::None;
        bool isReady = false;
    };

    class Room final
    {
    public:
        Room(
            std::string id,
            std::uint32_t hostPlayerId,
            std::string hostNickname
        );

        void AddPlayer(
            std::uint32_t playerId,
            std::string nickname
        );

        void RemovePlayer(
            std::uint32_t playerId
        );

        void SetPlayerCharacter(
            std::uint32_t playerId,
            CharacterId character
        );

        void SetPlayerReady(
            std::uint32_t playerId,
            bool ready
        );

        void SetDifficulty(
            std::uint32_t requesterPlayerId,
            DifficultyId difficulty
        );

        [[nodiscard]]
        DifficultyId SelectedDifficulty() const noexcept;

        [[nodiscard]]
        bool CanStart() const noexcept;

        void Start(
            std::uint32_t requesterPlayerId
        );

        [[nodiscard]]
        const std::string& Id() const noexcept;

        [[nodiscard]]
        RoomStatus Status() const noexcept;

        [[nodiscard]]
        bool CanAcceptPlayer() const noexcept;

        [[nodiscard]]
        std::size_t PlayerCount() const noexcept;

        [[nodiscard]]
        std::uint32_t HostPlayerId() const noexcept;

        [[nodiscard]]
        const std::string& HostNickname() const noexcept;

        [[nodiscard]]
        const RoomPlayer& PlayerAt(
            std::size_t index
        ) const;

    private:
        static constexpr std::size_t kMaxPlayers = 2;

        std::string id_;
        RoomStatus status_ = RoomStatus::Waiting;
        DifficultyId selectedDifficulty_ = DifficultyId::None;
        std::uint32_t hostPlayerId_ = 0;
        std::string hostNickname_;
        std::vector<RoomPlayer> players_;
    };
}