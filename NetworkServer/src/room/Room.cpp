#include "room/Room.h"

#include <stdexcept>
#include <utility>
#include <algorithm>

namespace tdr::room
{
    Room::Room(
        std::string id,
        const std::uint32_t hostPlayerId,
        std::string hostNickname
    )
        : id_(std::move(id)),
        hostPlayerId_(hostPlayerId),
        hostNickname_(std::move(hostNickname))
    {
        if (hostPlayerId_ == 0)
        {
            throw std::invalid_argument(
                "Host player ID must be non-zero."
            );
        }

        if (hostNickname_.empty())
        {
            throw std::invalid_argument(
                "Host nickname cannot be empty."
            );
        }

        players_.push_back(
            RoomPlayer
            {
                hostPlayerId_,
                hostNickname_,
                true
            }
        );
    }

    void Room::AddPlayer(
        const std::uint32_t playerId,
        std::string nickname
    )
    {
        if (status_ != RoomStatus::Waiting)
        {
            throw std::runtime_error(
                "Cannot join a started room."
            );
        }

        if (playerId == 0)
        {
            throw std::invalid_argument(
                "Player ID must be non-zero."
            );
        }

        if (nickname.empty())
        {
            throw std::invalid_argument(
                "Player nickname cannot be empty."
            );
        }

        if (players_.size() >= kMaxPlayers)
        {
            throw std::runtime_error(
                "Room is full."
            );
        }

        for (const auto& player : players_)
        {
            if (player.playerId == playerId)
            {
                throw std::runtime_error(
                    "Player is already in the room."
                );
            }
        }

        players_.push_back(
            RoomPlayer
            {
                playerId,
                std::move(nickname),
                false
            }
        );
    }

    void Room::RemovePlayer(
        const std::uint32_t playerId
    )
    {
        if (playerId == hostPlayerId_)
        {
            throw std::runtime_error(
                "Room cannot remove its host directly."
            );
        }

        const auto playerIterator =
            std::find_if(
                players_.begin(),
                players_.end(),
                [playerId](const RoomPlayer& player)
                {
                    return player.playerId
                        == playerId;
                }
            );

        if (playerIterator == players_.end())
        {
            throw std::out_of_range(
                "Player does not exist in the room."
            );
        }

        players_.erase(
            playerIterator
        );
    }

    void Room::SetPlayerCharacter(
        const std::uint32_t playerId,
        const CharacterId character
    )
    {
        if (status_ != RoomStatus::Waiting)
        {
            throw std::runtime_error(
                "Cannot change character "
                "after the room has started."
            );
        }

        if (character == CharacterId::None)
        {
            throw std::invalid_argument(
                "Player character cannot be None."
            );
        }

        for (auto& player : players_)
        {
            if (player.playerId == playerId)
            {
                if (player.selectedCharacter
                    == character)
                {
                    return;
                }

                player.selectedCharacter =
                    character;

                player.isReady = false;

                return;
            }
        }

        throw std::out_of_range(
            "Player does not exist in the room."
        );
    }

    void Room::SetPlayerReady(
        const std::uint32_t playerId,
        const bool ready
    )
    {
        if (status_ != RoomStatus::Waiting)
        {
            throw std::runtime_error(
                "Cannot change ready state "
                "after the room has started."
            );
        }

        for (auto& player : players_)
        {
            if (player.playerId != playerId)
            {
                continue;
            }

            if (ready
                && player.selectedCharacter
                == CharacterId::None)
            {
                throw std::runtime_error(
                    "Player must select a character "
                    "before becoming ready."
                );
            }

            player.isReady = ready;
            return;
        }

        throw std::out_of_range(
            "Player does not exist in the room."
        );
    }

    void Room::SetDifficulty(
        const std::uint32_t requesterPlayerId,
        const DifficultyId difficulty
    )
    {
        if (status_ != RoomStatus::Waiting)
        {
            throw std::runtime_error(
                "Cannot change difficulty "
                "after the room has started."
            );
        }

        if (requesterPlayerId != hostPlayerId_)
        {
            throw std::runtime_error(
                "Only the room host can "
                "change the difficulty."
            );
        }

        if (difficulty == DifficultyId::None)
        {
            throw std::invalid_argument(
                "Room difficulty cannot be None."
            );
        }

        selectedDifficulty_ = difficulty;
    }

    const std::string& Room::Id() const noexcept
    {
        return id_;
    }

    RoomStatus Room::Status() const noexcept
    {
        return status_;
    }

    DifficultyId
        Room::SelectedDifficulty() const noexcept
    {
        return selectedDifficulty_;
    }

    bool Room::CanStart() const noexcept
    {
        if (status_ != RoomStatus::Waiting)
        {
            return false;
        }

        if (players_.size() != kMaxPlayers)
        {
            return false;
        }

        if (selectedDifficulty_
            == DifficultyId::None)
        {
            return false;
        }

        for (const auto& player : players_)
        {
            if (player.selectedCharacter
                == CharacterId::None)
            {
                return false;
            }

            if (!player.isReady)
            {
                return false;
            }
        }

        return true;
    }

    void Room::Start(
        const std::uint32_t requesterPlayerId
    )
    {
        if (status_ != RoomStatus::Waiting)
        {
            throw std::runtime_error(
                "Room has already started."
            );
        }

        if (requesterPlayerId != hostPlayerId_)
        {
            throw std::runtime_error(
                "Only the room host can "
                "start the game."
            );
        }

        if (!CanStart())
        {
            throw std::runtime_error(
                "Room does not satisfy "
                "the start requirements."
            );
        }

        status_ = RoomStatus::Started;
    }

    std::size_t Room::PlayerCount() const noexcept
    {
        return players_.size();
    }

    std::uint32_t Room::HostPlayerId() const noexcept
    {
        return hostPlayerId_;
    }

    const std::string& Room::HostNickname() const noexcept
    {
        return hostNickname_;
    }

    const RoomPlayer& Room::PlayerAt(
        const std::size_t index
    ) const
    {
        if (index >= players_.size())
        {
            throw std::out_of_range(
                "Room player index is out of range."
            );
        }

        return players_[index];
    }
}