#pragma once

#include "protocol/UdpMessageHeader.h"

#include <cstdint>
#include <vector>
#include <cstddef>
#include <stdexcept>
#include <string>

namespace tdr::protocol
{
    enum class UdpPacketError : std::uint8_t
    {
        InvalidMagic,
        UnsupportedVersion,
        UnknownMessageType,
        NonUdpMessageType
    };

    class UdpPacketDecodeException final
        : public std::runtime_error
    {
    public:
        UdpPacketDecodeException(
            UdpPacketError code,
            const std::string& message
        );

        [[nodiscard]]
        UdpPacketError Code() const noexcept;

    private:
        UdpPacketError code_;
    };

    struct DecodedUdpPacket final
    {
        UdpMessageHeader header;
        std::vector<std::uint8_t> payload;
    };

    class UdpPacketCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            const UdpMessageHeader& header,
            const std::vector<std::uint8_t>& payload
        );

        [[nodiscard]]
        static DecodedUdpPacket Decode(
            const std::uint8_t* data,
            std::size_t size
        );
    };
}