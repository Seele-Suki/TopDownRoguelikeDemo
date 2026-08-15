#pragma once

#include "protocol/PacketCodec.h"

#include <string>

namespace tdr::room
{
    class ClientState final
    {
    public:
        void HandlePacket(
            const tdr::protocol::DecodedPacket& packet
        );

        [[nodiscard]]
        const std::string& Nickname() const noexcept;

    private:
        std::string nickname_;
    };
}