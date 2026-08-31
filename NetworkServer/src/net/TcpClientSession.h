#pragma once

#include "protocol/PacketCodec.h"
#include "room/ClientState.h"
#include "room/Room.h"
#include "room/RoomManager.h"
#include "room/PlayerIdAllocator.h"
#include "room/SessionTokenGenerator.h"
#include "protocol/UdpMessageHeader.h"
#include "protocol/UdpSequenceTracker.h"
#include "net/UdpEndpoint.h"

#include <optional>
#include <array>
#include <cstddef>
#include <cstdint>
#include <string>
#include <vector>

namespace tdr::net
{
    class TcpClientSession final
    {
    public:
        TcpClientSession(
            tdr::room::PlayerIdAllocator& playerIdAllocator,
            tdr::room::SessionTokenGenerator& tokenGenerator,
            tdr::room::RoomManager& roomManager
        );

        void ReceiveBytes(
            const std::uint8_t* data,
            std::size_t size
        );

        [[nodiscard]]
        std::vector<std::vector<std::uint8_t>>
            TakeOutgoingPackets();

        [[nodiscard]]
        std::vector<std::string>
            TakeChangedRoomIds();

        [[nodiscard]]
        std::vector<std::string>
            TakeStartedRoomIds();

        [[nodiscard]]
        std::vector<std::string>
            TakeClosedRoomIds();

        [[nodiscard]]
        std::vector<std::vector<std::uint8_t>>
            TakeWorldEntitySpawnPayloads();

        [[nodiscard]]
        std::vector<std::vector<std::uint8_t>>
            TakeWorldEntityRemovalPayloads();

        void LeaveRoom();

        [[nodiscard]]
        bool InvalidateRoom(
            const std::string& roomId
        ) noexcept;

        [[nodiscard]]
        std::uint32_t PlayerId() const noexcept;

        [[nodiscard]]
        const std::string& SessionToken() const noexcept;

        [[nodiscard]]
        bool MatchesSessionToken(
            const std::string& token
        ) const noexcept;

        void BindUdpEndpoint(
            const sockaddr_in6& address
        );

        [[nodiscard]]
        bool HasUdpEndpoint() const noexcept;

        [[nodiscard]]
        bool MatchesUdpEndpoint(
            const sockaddr_in6& address
        ) const noexcept;

        [[nodiscard]]
        const sockaddr_in6& UdpEndpointAddress() const;

        [[nodiscard]]
        const std::array<
            std::uint8_t,
            tdr::protocol::kUdpSessionTokenSize
        >& SessionTokenBytes() const noexcept;

        [[nodiscard]]
        bool MatchesSessionToken(
            const std::array<
            std::uint8_t,
            tdr::protocol::kUdpSessionTokenSize
            >& token
        ) const noexcept;

        [[nodiscard]]
        bool AcceptUdpSequence(
            std::uint32_t sequence
        ) noexcept;

        [[nodiscard]]
        bool AcceptPlayerInputSequence(
            std::uint32_t sequence
        ) noexcept;

        [[nodiscard]]
        bool AcceptPlayerStateSequence(
            std::uint32_t sequence
        ) noexcept;

        [[nodiscard]]
        const std::string& Nickname() const noexcept;

        [[nodiscard]]
        bool HasRoom() const noexcept;

        [[nodiscard]]
        const tdr::room::Room& CurrentRoom() const;

    private:
        void HandlePacket(
            const tdr::protocol::DecodedPacket& packet
        );

        tdr::protocol::PacketCodec codec_;
        tdr::room::ClientState state_;

        std::uint32_t playerId_ = 0;
        tdr::room::RoomManager& roomManager_;
        std::string roomId_;
        std::string sessionToken_;

        std::array<
            std::uint8_t,
            tdr::protocol::kUdpSessionTokenSize
        > sessionTokenBytes_{};

        std::optional<UdpEndpoint> udpEndpoint_;
        tdr::protocol::UdpSequenceTracker
            udpSequenceTracker_;

        tdr::protocol::UdpSequenceTracker
            playerInputSequenceTracker_;

        tdr::protocol::UdpSequenceTracker
            playerStateSequenceTracker_;

        std::vector<std::vector<std::uint8_t>>
            outgoingPackets_;

        std::vector<std::string>
            changedRoomIds_;

        std::vector<std::string>
            startedRoomIds_;

        std::vector<std::string>
            closedRoomIds_;

        std::vector<std::vector<std::uint8_t>>
            worldEntitySpawnPayloads_;

        std::vector<std::vector<std::uint8_t>>
            worldEntityRemovalPayloads_;
    };
}
