#include "net/TcpClientSession.h"
#include "protocol/SessionTokenCodec.h"

#include <stdexcept>
#include <string>

namespace tdr::net
{
    TcpClientSession::TcpClientSession(
        tdr::room::PlayerIdAllocator& playerIdAllocator,
        tdr::room::SessionTokenGenerator& tokenGenerator,
        tdr::room::RoomManager& roomManager
    )
        : playerId_(
            playerIdAllocator.Allocate()),
        roomManager_(roomManager),
        sessionToken_(
            tokenGenerator.Generate())
    {
        sessionTokenBytes_ =
            tdr::protocol::SessionTokenCodec::DecodeHex(
                sessionToken_
            );
    }

    void TcpClientSession::ReceiveBytes(
        const std::uint8_t* const data,
        const std::size_t size
    )
    {
        codec_.Append(data, size);

        const auto packets =
            codec_.DecodeAvailable();

        for (const auto& packet : packets)
        {
            HandlePacket(packet);
        }
    }

    std::vector<std::vector<std::uint8_t>>
        TcpClientSession::TakeOutgoingPackets()
    {
        std::vector<std::vector<std::uint8_t>>
            packets;

        packets.swap(
            outgoingPackets_
        );

        return packets;
    }

    void TcpClientSession::LeaveRoom()
    {
        if (!HasRoom())
        {
            return;
        }

        roomManager_.RemovePlayer(
            roomId_,
            playerId_
        );

        roomId_.clear();
    }

    void TcpClientSession::InvalidateRoom(
        const std::string& roomId
    ) noexcept
    {
        if (roomId_ == roomId)
        {
            roomId_.clear();
        }
    }

    std::uint32_t
        TcpClientSession::PlayerId() const noexcept
    {
        return playerId_;
    }

    const std::string&
        TcpClientSession::SessionToken() const noexcept
    {
        return sessionToken_;
    }

    bool TcpClientSession::MatchesSessionToken(
        const std::string& token
    ) const noexcept
    {
        return !token.empty()
            && token == sessionToken_;
    }

    void TcpClientSession::BindUdpEndpoint(
        const sockaddr_in6& address
    )
    {
        if (udpEndpoint_.has_value())
        {
            if (!udpEndpoint_->Matches(address))
            {
                throw std::runtime_error(
                    "UDP endpoint is already bound "
                    "to a different address."
                );
            }

            return;
        }

        udpEndpoint_.emplace(address);
    }

    bool TcpClientSession::HasUdpEndpoint() const noexcept
    {
        return udpEndpoint_.has_value();
    }

    bool TcpClientSession::MatchesUdpEndpoint(
        const sockaddr_in6& address
    ) const noexcept
    {
        return udpEndpoint_.has_value()
            && udpEndpoint_->Matches(address);
    }

    const std::array<
        std::uint8_t,
        tdr::protocol::kUdpSessionTokenSize
    >& TcpClientSession::SessionTokenBytes() const noexcept
    {
        return sessionTokenBytes_;
    }

    bool TcpClientSession::MatchesSessionToken(
        const std::array<
        std::uint8_t,
        tdr::protocol::kUdpSessionTokenSize
        >& token
    ) const noexcept
    {
        return token == sessionTokenBytes_;
    }

    const std::string&
        TcpClientSession::Nickname() const noexcept
    {
        return state_.Nickname();
    }

    bool TcpClientSession::HasRoom() const noexcept
    {
        return !roomId_.empty()
            && roomManager_.ContainsRoom(
                roomId_
            );
    }

    const tdr::room::Room&
        TcpClientSession::CurrentRoom() const
    {
        if (!HasRoom())
        {
            throw std::runtime_error(
                "TCP client session has no room."
            );
        }

        return roomManager_.FindRoom(roomId_);
    }

    void TcpClientSession::HandlePacket(
        const tdr::protocol::DecodedPacket& packet
    )
    {
        if (packet.type
            == tdr::protocol::MessageType::SetNickname)
        {
            state_.HandlePacket(packet);
            return;
        }

        if (packet.type
            == tdr::protocol::MessageType::CreateRoomRequest)
        {
            if (HasRoom())
            {
                throw std::runtime_error(
                    "TCP client session is already in a room."
                );
            }

            if (state_.Nickname().empty())
            {
                throw std::runtime_error(
                    "Nickname must be set before "
                    "creating a room."
                );
            }

            const auto room =
                roomManager_.CreateRoom(
                    playerId_,
                    state_.Nickname()
                );

            roomId_ = room.Id();

            const std::vector<std::uint8_t>
                responsePayload(
                    roomId_.begin(),
                    roomId_.end()
                );

            outgoingPackets_.push_back(
                tdr::protocol::PacketCodec::Encode(
                    tdr::protocol::MessageType::
                    CreateRoomResponse,
                    responsePayload
                )
            );

            return;
        }

        if (packet.type
            == tdr::protocol::MessageType::JoinRoomRequest)
        {
            if (HasRoom())
            {
                throw std::runtime_error(
                    "TCP client session is already in a room."
                );
            }

            if (state_.Nickname().empty())
            {
                throw std::runtime_error(
                    "Nickname must be set before "
                    "joining a room."
                );
            }

            const std::string requestedRoomId(
                packet.payload.begin(),
                packet.payload.end()
            );

            if (requestedRoomId.empty())
            {
                throw std::invalid_argument(
                    "Join room request must contain "
                    "a room ID."
                );
            }

            roomManager_.AddPlayer(
                requestedRoomId,
                playerId_,
                state_.Nickname()
            );

            roomId_ = requestedRoomId;

            const std::vector<std::uint8_t>
                responsePayload(
                    roomId_.begin(),
                    roomId_.end()
                );

            outgoingPackets_.push_back(
                tdr::protocol::PacketCodec::Encode(
                    tdr::protocol::MessageType::
                    JoinRoomResponse,
                    responsePayload
                )
            );

            return;
        }

        if (packet.type
            == tdr::protocol::MessageType::SetPlayerSelection)
        {
            if (!HasRoom())
            {
                throw std::runtime_error(
                    "Player must join a room "
                    "before selecting."
                );
            }

            if (packet.payload.size() != 2)
            {
                throw std::invalid_argument(
                    "SetPlayerSelection payload "
                    "must contain exactly two bytes."
                );
            }

            const auto character =
                static_cast<tdr::room::CharacterId>(
                    packet.payload[0]
                    );

            const auto difficulty =
                static_cast<tdr::room::DifficultyId>(
                    packet.payload[1]
                    );

            if (character
                != tdr::room::CharacterId::Ranged
                && character
                != tdr::room::CharacterId::Melee)
            {
                throw std::invalid_argument(
                    "SetPlayerSelection contains "
                    "an invalid character."
                );
            }

            if (difficulty
                != tdr::room::DifficultyId::None
                && difficulty
                != tdr::room::DifficultyId::Normal
                && difficulty
                != tdr::room::DifficultyId::Hard
                && difficulty
                != tdr::room::DifficultyId::Hell)
            {
                throw std::invalid_argument(
                    "SetPlayerSelection contains "
                    "an invalid difficulty."
                );
            }

            auto& room =
                roomManager_.FindRoom(roomId_);

            room.SetPlayerCharacter(
                playerId_,
                character
            );

            if (difficulty
                != tdr::room::DifficultyId::None)
            {
                room.SetDifficulty(
                    playerId_,
                    difficulty
                );
            }

            return;
        }

        if (packet.type
            == tdr::protocol::MessageType::SetReady)
        {
            if (!HasRoom())
            {
                throw std::runtime_error(
                    "Player must join a room "
                    "before changing ready state."
                );
            }

            if (packet.payload.size() != 1)
            {
                throw std::invalid_argument(
                    "SetReady payload must contain "
                    "exactly one byte."
                );
            }

            if (packet.payload[0] != 0
                && packet.payload[0] != 1)
            {
                throw std::invalid_argument(
                    "SetReady payload must be 0 or 1."
                );
            }

            auto& room =
                roomManager_.FindRoom(roomId_);

            room.SetPlayerReady(
                playerId_,
                packet.payload[0] == 1
            );

            return;
        }

        if (packet.type
            == tdr::protocol::MessageType::StartGameRequest)
        {
            if (!HasRoom())
            {
                throw std::runtime_error(
                    "Player must join a room "
                    "before starting the game."
                );
            }

            if (!packet.payload.empty())
            {
                throw std::invalid_argument(
                    "StartGameRequest payload "
                    "must be empty."
                );
            }

            auto& room =
                roomManager_.FindRoom(
                    roomId_
                );

            room.Start(
                playerId_
            );

            return;
        }

        if (packet.type
            == tdr::protocol::MessageType::LeaveRoom)
        {
            if (!HasRoom())
            {
                throw std::runtime_error(
                    "Player must join a room "
                    "before leaving it."
                );
            }

            if (!packet.payload.empty())
            {
                throw std::invalid_argument(
                    "LeaveRoom payload must be empty."
                );
            }

            LeaveRoom();
            return;
        }

        throw std::invalid_argument(
            "TCP client session received "
            "an unsupported message."
        );
    }
}