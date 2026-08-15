#include "protocol/UdpBindingCredentialsCodec.h"

#include "protocol/NetworkByteOrder.h"

#include <algorithm>
#include <cstring>
#include <stdexcept>

namespace tdr::protocol
{
    std::vector<std::uint8_t>
        UdpBindingCredentialsCodec::Encode(
            const UdpBindingCredentials& credentials
        )
    {
        std::vector<std::uint8_t> encoded(
            kUdpBindingCredentialsSize
        );

        const std::uint32_t networkPlayerId =
            HostToNetwork32(
                credentials.playerId
            );

        std::memcpy(
            encoded.data() + kUdpBindingPlayerIdOffset,
            &networkPlayerId,
            sizeof(networkPlayerId)
        );

        std::copy(
            credentials.sessionToken.begin(),
            credentials.sessionToken.end(),
            encoded.begin() + kUdpBindingTokenOffset
        );

        return encoded;
    }

    UdpBindingCredentials
        UdpBindingCredentialsCodec::Decode(
            const std::uint8_t* const data,
            const std::size_t size
        )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "UDP binding credentials cannot be null."
            );
        }

        if (size != kUdpBindingCredentialsSize)
        {
            throw std::invalid_argument(
                "UDP binding credentials have an invalid size."
            );
        }

        std::uint32_t networkPlayerId{};

        std::memcpy(
            &networkPlayerId,
            data + kUdpBindingPlayerIdOffset,
            sizeof(networkPlayerId)
        );

        UdpBindingCredentials credentials{};
        credentials.playerId =
            NetworkToHost32(
                networkPlayerId
            );

        std::copy_n(
            data + kUdpBindingTokenOffset,
            kUdpSessionTokenSize,
            credentials.sessionToken.begin()
        );

        return credentials;
    }
}
