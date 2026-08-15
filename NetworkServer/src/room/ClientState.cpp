#include "room/ClientState.h"

#include <stdexcept>

namespace tdr::room
{
    void ClientState::HandlePacket(
        const tdr::protocol::DecodedPacket& packet
    )
    {
        if (packet.type
            != tdr::protocol::MessageType::SetNickname)
        {
            throw std::invalid_argument(
                "ClientState received an unsupported message."
            );
        }

        nickname_.assign(
            packet.payload.begin(),
            packet.payload.end()
        );
    }

    const std::string&
        ClientState::Nickname() const noexcept
    {
        return nickname_;
    }
}