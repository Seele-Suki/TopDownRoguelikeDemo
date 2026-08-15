#include "protocol/PacketCodec.h"

#include "protocol/MessageHeader.h"
#include "protocol/NetworkByteOrder.h"

#include <algorithm>
#include <cstring>
#include <stdexcept>
#include <utility>

namespace
{
    void WriteNetwork16(
        std::vector<std::uint8_t>& destination,
        const std::size_t offset,
        const std::uint16_t value
    )
    {
        const std::uint16_t networkValue =
            tdr::protocol::HostToNetwork16(value);

        std::memcpy(
            destination.data() + offset,
            &networkValue,
            sizeof(networkValue)
        );
    }

    void WriteNetwork32(
        std::vector<std::uint8_t>& destination,
        const std::size_t offset,
        const std::uint32_t value
    )
    {
        const std::uint32_t networkValue =
            tdr::protocol::HostToNetwork32(value);

        std::memcpy(
            destination.data() + offset,
            &networkValue,
            sizeof(networkValue)
        );
    }

    std::uint16_t ReadNetwork16(
        const std::vector<std::uint8_t>& source,
        const std::size_t offset
    )
    {
        std::uint16_t networkValue{};

        std::memcpy(
            &networkValue,
            source.data() + offset,
            sizeof(networkValue)
        );

        return tdr::protocol::NetworkToHost16(
            networkValue
        );
    }

    std::uint32_t ReadNetwork32(
        const std::vector<std::uint8_t>& source,
        const std::size_t offset
    )
    {
        std::uint32_t networkValue{};

        std::memcpy(
            &networkValue,
            source.data() + offset,
            sizeof(networkValue)
        );

        return tdr::protocol::NetworkToHost32(
            networkValue
        );
    }
}

namespace tdr::protocol
{
    PacketDecodeException::PacketDecodeException(
        const PacketError code,
        const std::string& message
    )
        : std::runtime_error(message),
        code_(code)
    {
    }

    PacketError PacketDecodeException::Code() const noexcept
    {
        return code_;
    }

    std::vector<std::uint8_t> PacketCodec::Encode(
        const MessageType type,
        const std::vector<std::uint8_t>& payload
    )
    {
        if (!IsKnownMessageType(type))
        {
            throw std::invalid_argument(
                "Cannot encode an Invalid message type."
            );
        }

        if (payload.size() > kMaxPayloadSize)
        {
            throw std::length_error(
                "Payload exceeds the maximum allowed size."
            );
        }

        const std::uint32_t payloadSize =
            static_cast<std::uint32_t>(payload.size());

        std::vector<std::uint8_t> packet(
            kMessageHeaderSize + payload.size()
        );

        WriteNetwork32(
            packet,
            kMagicOffset,
            kProtocolMagic
        );

        WriteNetwork16(
            packet,
            kVersionOffset,
            kProtocolVersion
        );

        WriteNetwork16(
            packet,
            kMessageTypeOffset,
            static_cast<std::uint16_t>(type)
        );

        WriteNetwork32(
            packet,
            kPayloadSizeOffset,
            payloadSize
        );

        if (!payload.empty())
        {
            std::copy(
                payload.begin(),
                payload.end(),
                packet.begin() + kMessageHeaderSize
            );
        }

        return packet;
    }

    void PacketCodec::Append(
        const std::uint8_t* const data,
        const std::size_t size
    )
    {
        if (size == 0)
        {
            return;
        }

        if (data == nullptr)
        {
            throw std::invalid_argument(
                "Receive data cannot be null when size is non-zero."
            );
        }

        if (size > kMaxReceiveBufferSize - receiveBuffer_.size())
        {
            throw PacketDecodeException(
                PacketError::ReceiveBufferOverflow,
                "TCP receive buffer exceeds the allowed size."
            );
        }

        receiveBuffer_.insert(
            receiveBuffer_.end(),
            data,
            data + size
        );
    }

    bool PacketCodec::TryDecode(DecodedPacket& packet)
    {
        if (receiveBuffer_.size() < kMessageHeaderSize)
        {
            return false;
        }

        const std::uint32_t magic =
            ReadNetwork32(
                receiveBuffer_,
                kMagicOffset
            );

        if (magic != kProtocolMagic)
        {
            throw PacketDecodeException(
                PacketError::InvalidMagic,
                "Invalid protocol magic."
            );
        }

        const std::uint16_t version =
            ReadNetwork16(
                receiveBuffer_,
                kVersionOffset
            );

        if (version != kProtocolVersion)
        {
            throw PacketDecodeException(
                PacketError::UnsupportedVersion,
                "Unsupported protocol version."
            );
        }

        const std::uint16_t rawMessageType =
            ReadNetwork16(
                receiveBuffer_,
                kMessageTypeOffset
            );

        const MessageType messageType =
            static_cast<MessageType>(rawMessageType);

        if (!IsKnownMessageType(messageType))
        {
            throw PacketDecodeException(
                PacketError::UnknownMessageType,
                "Unknown message type."
            );
        }

        const std::uint32_t payloadSize =
            ReadNetwork32(
                receiveBuffer_,
                kPayloadSizeOffset
            );

        if (!IsValidPayloadSize(payloadSize))
        {
            throw PacketDecodeException(
                PacketError::PayloadTooLarge,
                "Received payload exceeds the maximum allowed size."
            );
        }

        const std::size_t completePacketSize =
            kMessageHeaderSize
            + static_cast<std::size_t>(payloadSize);

        if (receiveBuffer_.size() < completePacketSize)
        {
            return false;
        }

        DecodedPacket decodedPacket;
        decodedPacket.type = messageType;

        decodedPacket.payload.assign(
            receiveBuffer_.begin() + kMessageHeaderSize,
            receiveBuffer_.begin() + completePacketSize
        );

        receiveBuffer_.erase(
            receiveBuffer_.begin(),
            receiveBuffer_.begin() + completePacketSize
        );

        packet = std::move(decodedPacket);

        return true;
    }

    std::vector<DecodedPacket> PacketCodec::DecodeAvailable()
    {
        std::vector<DecodedPacket> packets;
        DecodedPacket packet;

        while (TryDecode(packet))
        {
            packets.push_back(
                std::move(packet)
            );
        }

        return packets;
    }

    std::size_t PacketCodec::BufferedByteCount() const noexcept
    {
        return receiveBuffer_.size();
    }

    void PacketCodec::Clear() noexcept
    {
        receiveBuffer_.clear();
    }
}