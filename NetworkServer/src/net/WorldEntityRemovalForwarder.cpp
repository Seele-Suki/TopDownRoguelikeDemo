#include "net/WorldEntityRemovalForwarder.h"

#include "net/TcpClientSession.h"
#include "protocol/WorldEntityRemovedCodec.h"

#include <cstddef>
#include <stdexcept>

namespace tdr::net
{
    ForwardedWorldEntityRemoval
        WorldEntityRemovalForwarder::Forward(
            const TcpClientSession& sender,
            const std::vector<std::uint8_t>& payload
        )
    {
        if (!sender.HasRoom())
        {
            throw std::runtime_error(
                "WorldEntityRemoved sender is not in a room."
            );
        }

        const auto& room = sender.CurrentRoom();

        if (room.Status() != tdr::room::RoomStatus::Started)
        {
            throw std::runtime_error(
                "WorldEntityRemoved cannot be forwarded "
                "before the room starts."
            );
        }

        if (sender.PlayerId() != room.HostPlayerId())
        {
            throw std::invalid_argument(
                "Only the room host can send WorldEntityRemoved."
            );
        }

        const auto removed =
            tdr::protocol::WorldEntityRemovedCodec::Decode(
                payload.data(),
                payload.size());

        const bool isEnemyDeath =
            removed.entityType ==
                tdr::protocol::WorldEntityType::Enemy &&
            removed.reason ==
                tdr::protocol::WorldEntityRemovalReason::Died;

        const bool isExperienceOrbDespawn =
            removed.entityType ==
                tdr::protocol::WorldEntityType::ExperienceOrb &&
            removed.reason ==
                tdr::protocol::WorldEntityRemovalReason::Despawned;

        if (!isEnemyDeath && !isExperienceOrbDespawn)
        {
            throw std::invalid_argument(
                "World entity removal type and reason are invalid."
            );
        }

        std::uint32_t guestPlayerId = 0U;

        for (std::size_t index = 0U;
            index < room.PlayerCount();
            ++index)
        {
            const auto candidateId =
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

        return { guestPlayerId, payload };
    }
}
