#include "room/RoomManager.h"

#include <stdexcept>
#include <string>
#include <utility>

namespace tdr::room
{
    Room RoomManager::CreateRoom(
        const std::uint32_t hostPlayerId,
        const std::string& hostNickname
    )
    {
        if (hostPlayerId == 0)
        {
            throw std::invalid_argument(
                "Host player ID must be non-zero."
            );
        }

        if (hostNickname.empty())
        {
            throw std::invalid_argument(
                "Host nickname cannot be empty."
            );
        }

        const std::string roomId =
            "ROOM-" + std::to_string(nextRoomNumber_++);

        auto result =
            rooms_.emplace(
                std::piecewise_construct,
                std::forward_as_tuple(roomId),
                std::forward_as_tuple(
                    roomId,
                    hostPlayerId,
                    hostNickname
                )
            );

        if (!result.second)
        {
            throw std::runtime_error(
                "Failed to create room."
            );
        }

        return result.first->second;
    }

    void RoomManager::AddPlayer(
        const std::string& roomId,
        const std::uint32_t playerId,
        const std::string& nickname
    )
    {
        Room& room = FindRoom(roomId);

        room.AddPlayer(
            playerId,
            nickname
        );
    }

    Room& RoomManager::FindSingleWaitingRoom()
    {
        Room* waitingRoom = nullptr;

        for (auto& entry : rooms_)
        {
            Room& candidate = entry.second;

            if (!candidate.CanAcceptPlayer())
            {
                continue;
            }

            if (waitingRoom != nullptr)
            {
                throw std::runtime_error(
                    "Multiple waiting rooms require "
                    "a room ID."
                );
            }

            waitingRoom = &candidate;
        }

        if (waitingRoom == nullptr)
        {
            throw std::runtime_error(
                "No waiting room is available."
            );
        }

        return *waitingRoom;
    }

    void RoomManager::RemovePlayer(
        const std::string& roomId,
        const std::uint32_t playerId
    )
    {
        Room& room =
            FindRoom(roomId);

        if (playerId == room.HostPlayerId())
        {
            rooms_.erase(
                roomId
            );

            return;
        }

        room.RemovePlayer(
            playerId
        );
    }

    bool RoomManager::ContainsRoom(
        const std::string& roomId
    ) const noexcept
    {
        return rooms_.find(roomId)
            != rooms_.end();
    }

    Room& RoomManager::FindRoom(
        const std::string& roomId
    )
    {
        const auto iterator =
            rooms_.find(roomId);

        if (iterator == rooms_.end())
        {
            throw std::out_of_range(
                "Room does not exist."
            );
        }

        return iterator->second;
    }

    const Room& RoomManager::FindRoom(
        const std::string& roomId
    ) const
    {
        const auto iterator =
            rooms_.find(roomId);

        if (iterator == rooms_.end())
        {
            throw std::out_of_range(
                "Room does not exist."
            );
        }

        return iterator->second;
    }
}