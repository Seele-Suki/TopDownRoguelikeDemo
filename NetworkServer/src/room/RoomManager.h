#pragma once

#include "room/Room.h"

#include <cstdint>
#include <string>
#include <unordered_map>

namespace tdr::room
{
    class RoomManager final
    {
    public:
        [[nodiscard]]
        Room CreateRoom(
            std::uint32_t hostPlayerId,
            const std::string& hostNickname
        );

        void AddPlayer(
            const std::string& roomId,
            std::uint32_t playerId,
            const std::string& nickname
        );

        void RemovePlayer(
            const std::string& roomId,
            std::uint32_t playerId
        );

        [[nodiscard]]
        bool ContainsRoom(
            const std::string& roomId
        ) const noexcept;

        [[nodiscard]]
        Room& FindRoom(
            const std::string& roomId
        );

        [[nodiscard]]
        const Room& FindRoom(
            const std::string& roomId
        ) const;

    private:
        std::uint32_t nextRoomNumber_ = 1;
        std::unordered_map<std::string, Room> rooms_;
    };
}