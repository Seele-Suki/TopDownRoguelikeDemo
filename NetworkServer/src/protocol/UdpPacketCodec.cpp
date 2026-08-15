#include "protocol/UdpPacketCodec.h"
#include "protocol/NetworkByteOrder.h"

#include <algorithm>
#include <cstring>
#include <stdexcept>

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
        const std::uint8_t* const source,
        const std::size_t offset
    )
    {
        std::uint16_t networkValue{};

        std::memcpy(
            &networkValue,
            source + offset,
            sizeof(networkValue)
        );

        return tdr::protocol::NetworkToHost16(networkValue);
    }

    std::uint32_t ReadNetwork32(
        const std::uint8_t* const source,
        const std::size_t offset
    )
    {
        std::uint32_t networkValue{};

        std::memcpy(
            &networkValue,
            source + offset,
            sizeof(networkValue)
        );

        return tdr::protocol::NetworkToHost32(networkValue);
    }
}

namespace tdr::protocol
{
    UdpPacketDecodeException::UdpPacketDecodeException(
        const UdpPacketError code,
        const std::string& message
    )
        : std::runtime_error(message),
        code_(code)
    {
    }

    UdpPacketError
        UdpPacketDecodeException::Code() const noexcept
    {
        return code_;
    }

    std::vector<std::uint8_t> UdpPacketCodec::Encode(
        const UdpMessageHeader& header,
        const std::vector<std::uint8_t>& payload
    )
    {
        std::vector<std::uint8_t> packet(
            kUdpMessageHeaderSize + payload.size()
        );

        WriteNetwork32(
            packet,
            kUdpMagicOffset,
            header.magic
        );

        WriteNetwork16(
            packet,
            kUdpVersionOffset,
            header.version
        );

        WriteNetwork16(
            packet,
            kUdpMessageTypeOffset,
            static_cast<std::uint16_t>(header.type)
        );

        std::copy(
            header.sessionToken.begin(),
            header.sessionToken.end(),
            packet.begin() + kUdpSessionTokenOffset
        );

        WriteNetwork32(
            packet,
            kUdpPlayerIdOffset,
            header.playerId
        );

        WriteNetwork32(
            packet,
            kUdpSequenceOffset,
            header.sequence
        );

        std::copy(
            payload.begin(),
            payload.end(),
            packet.begin() + kUdpMessageHeaderSize
        );

        return packet;
    }

    DecodedUdpPacket UdpPacketCodec::Decode(
        const std::uint8_t* const data,
        const std::size_t size
    )
    {
        if (data == nullptr)
        {
            throw std::invalid_argument(
                "UDP datagram data cannot be null."
            );
        }

        if (size < kUdpMessageHeaderSize)
        {
            throw std::invalid_argument(
                "UDP datagram is smaller than its header."
            );
        }

        DecodedUdpPacket packet;

        packet.header.magic =
            ReadNetwork32(data, kUdpMagicOffset);

        if (packet.header.magic != kUdpProtocolMagic)
        {
            throw UdpPacketDecodeException(
                UdpPacketError::InvalidMagic,
                "Invalid UDP protocol magic."
            );
        }

        packet.header.version =
            ReadNetwork16(data, kUdpVersionOffset);

        if (packet.header.version != kUdpProtocolVersion)
        {
            throw UdpPacketDecodeException(
                UdpPacketError::UnsupportedVersion,
                "Unsupported UDP protocol version."
            );
        }

        packet.header.type =
            static_cast<MessageType>(
                ReadNetwork16(data, kUdpMessageTypeOffset)
                );

        if (!IsKnownMessageType(packet.header.type))
        {
            throw UdpPacketDecodeException(
                UdpPacketError::UnknownMessageType,
                "Unknown UDP message type."
            );
        }

        if (!IsUdpMessageType(packet.header.type))
        {
            throw UdpPacketDecodeException(
                UdpPacketError::NonUdpMessageType,
                "Message type is not valid for UDP."
            );
        }

        std::copy(
            data + kUdpSessionTokenOffset,
            data + kUdpSessionTokenOffset
            + kUdpSessionTokenSize,
            packet.header.sessionToken.begin()
        );

        packet.header.playerId =
            ReadNetwork32(data, kUdpPlayerIdOffset);

        packet.header.sequence =
            ReadNetwork32(data, kUdpSequenceOffset);

        packet.payload.assign(
            data + kUdpMessageHeaderSize,
            data + size
        );

        return packet;
    }
}