#pragma once

#include "protocol/MessageType.h"

#include <cstddef>
#include <cstdint>
#include <stdexcept>
#include <string>
#include <vector>

namespace tdr::protocol
{
    enum class PacketError : std::uint8_t
    {
        InvalidMagic,
        UnsupportedVersion,
        UnknownMessageType,
        PayloadTooLarge,
        ReceiveBufferOverflow
    };

    class PacketDecodeException final
        : public std::runtime_error
    {
    public:
        PacketDecodeException(
            PacketError code,
            const std::string& message
        );

        [[nodiscard]]
        PacketError Code() const noexcept;

    private:
        PacketError code_;
    };

    struct DecodedPacket final
    {
        MessageType type = MessageType::Invalid;
        std::vector<std::uint8_t> payload;
    };

    class PacketCodec final
    {
    public:
        [[nodiscard]]
        static std::vector<std::uint8_t> Encode(
            MessageType type,
            const std::vector<std::uint8_t>& payload
        );

        void Append(
            const std::uint8_t* data,
            std::size_t size
        );

        [[nodiscard]]
        bool TryDecode(DecodedPacket& packet);

        [[nodiscard]]
        std::vector<DecodedPacket> DecodeAvailable();

        [[nodiscard]]
        std::size_t BufferedByteCount() const noexcept;

        void Clear() noexcept;

    private:
        std::vector<std::uint8_t> receiveBuffer_;
    };
}