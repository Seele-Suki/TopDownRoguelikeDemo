#include "net/WorldEntitySpawnForwarder.h"

#include "net/TcpClientSession.h"
#include "protocol/WorldEntitySpawnedCodec.h"

#include <cstddef>
#include <stdexcept>

namespace tdr::net
{
    ForwardedWorldEntitySpawn
        WorldEntitySpawnForwarder::Forward(
            const TcpClientSession& sender,
            const std::vector<std::uint8_t>& payload
        )
    {
        if (!sender.HasRoom())
        {
            throw std::runtime_error(
                "WorldEntitySpawned sender is not in a room."
            );
        }

        const auto& room = sender.CurrentRoom();

        if (room.Status() !=
            tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "WorldEntitySpawned cannot be forwarded "
                "before the room starts."
            );
        }

        if (sender.PlayerId() !=
            room.HostPlayerId())
        {
            throw std::invalid_argument(
                "Only the room host can send "
                "WorldEntitySpawned."
            );
        }

        static_cast<void>(
            tdr::protocol::WorldEntitySpawnedCodec::Decode(
                payload.data(),
                payload.size()));

        std::uint32_t guestPlayerId = 0U;

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            const std::uint32_t candidateId =
                room.PlayerAt(index).playerId;

            if (candidateId != room.HostPlayerId())
            {
                guestPlayerId = candidateId;
                break;
            }
        }

        if (guestPlayerId == 0U)
        {
            throw std::runtime_error(
                "The room has no guest player."
            );
        }

        return ForwardedWorldEntitySpawn{
            guestPlayerId,
            payload
        };
    }
}
