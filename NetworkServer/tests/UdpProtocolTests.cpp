#include "protocol/MessageType.h"
#include "protocol/UdpMessageHeader.h"
#include "protocol/UdpPacketCodec.h"

#include <cstddef>
#include <vector>
#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>

namespace
{
    void Require(
        const bool condition,
        const char* const message
    )
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    void UdpMessageTypesAreClassifiedCorrectly()
    {
        using tdr::protocol::IsUdpMessageType;
        using tdr::protocol::MessageType;

        Require(
            IsUdpMessageType(MessageType::UdpBindRequest),
            "UdpBindRequest was not classified as UDP."
        );

        Require(
            IsUdpMessageType(MessageType::UdpBindAccepted),
            "UdpBindAccepted was not classified as UDP."
        );

        Require(
            IsUdpMessageType(MessageType::UdpPing),
            "UdpPing was not classified as UDP."
        );

        Require(
            IsUdpMessageType(MessageType::UdpPong),
            "UdpPong was not classified as UDP."
        );

        Require(
            !IsUdpMessageType(MessageType::ClientHello),
            "A TCP message was incorrectly classified as UDP."
        );

        Require(
            static_cast<std::uint16_t>(
                MessageType::UdpBindRequest) == 30,
            "UdpBindRequest wire value changed."
        );

        Require(
            static_cast<std::uint16_t>(
                MessageType::UdpPong) == 33,
            "UdpPong wire value changed."
        );
    }

    void UdpMessageHeaderUsesStableWireLayout()
    {
        using namespace tdr::protocol;

        Require(
            kUdpProtocolMagic == 0x54445255U,
            "UDP protocol magic must be TDRU."
        );

        Require(
            kUdpProtocolVersion == 1U,
            "UDP protocol version must be 1."
        );

        Require(kUdpMagicOffset == 0U, "Invalid magic offset.");
        Require(kUdpVersionOffset == 4U, "Invalid version offset.");
        Require(kUdpMessageTypeOffset == 6U, "Invalid type offset.");
        Require(kUdpSessionTokenOffset == 8U, "Invalid token offset.");
        Require(kUdpSessionTokenSize == 16U, "Invalid token size.");
        Require(kUdpPlayerIdOffset == 24U, "Invalid player ID offset.");
        Require(kUdpSequenceOffset == 28U, "Invalid sequence offset.");
        Require(kUdpMessageHeaderSize == 32U, "Invalid header size.");

        const UdpMessageHeader header{};

        Require(
            header.magic == kUdpProtocolMagic,
            "UDP header magic default is incorrect."
        );

        Require(
            header.version == kUdpProtocolVersion,
            "UDP header version default is incorrect."
        );

        Require(
            header.type == MessageType::Invalid,
            "UDP header type must default to Invalid."
        );

        Require(header.playerId == 0U, "Player ID must default to zero.");
        Require(header.sequence == 0U, "Sequence must default to zero.");
    }

    void UdpPingEncodesInNetworkByteOrder()
    {
        using namespace tdr::protocol;

        UdpMessageHeader header{};
        header.type = MessageType::UdpPing;
        header.playerId = 0x01020304U;
        header.sequence = 0xA1B2C3D4U;

        for (std::size_t index = 0;
            index < header.sessionToken.size();
            ++index)
        {
            header.sessionToken[index] =
                static_cast<std::uint8_t>(index);
        }

        const std::vector<std::uint8_t> payload;

        const auto packet =
            UdpPacketCodec::Encode(header, payload);

        const std::vector<std::uint8_t> expected
        {
            0x54, 0x44, 0x52, 0x55,
            0x00, 0x01,
            0x00, 0x20,

            0x00, 0x01, 0x02, 0x03,
            0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x0B,
            0x0C, 0x0D, 0x0E, 0x0F,

            0x01, 0x02, 0x03, 0x04,
            0xA1, 0xB2, 0xC3, 0xD4
        };

        Require(
            packet == expected,
            "UDP Ping bytes do not match the wire protocol."
        );
    }

    void UdpPongRoundTripPreservesAllFields()
    {
        using namespace tdr::protocol;

        UdpMessageHeader original{};
        original.type = MessageType::UdpPong;
        original.playerId = 42U;
        original.sequence = 0xFFFFFFFEU;

        for (std::size_t index = 0;
            index < original.sessionToken.size();
            ++index)
        {
            original.sessionToken[index] =
                static_cast<std::uint8_t>(0xF0U + index);
        }

        const std::vector<std::uint8_t> payload
        {
            0xAA, 0xBB, 0xCC
        };

        const auto encoded =
            UdpPacketCodec::Encode(original, payload);

        const auto decoded =
            UdpPacketCodec::Decode(
                encoded.data(),
                encoded.size()
            );

        Require(
            decoded.header.magic == kUdpProtocolMagic,
            "Decoded UDP magic is incorrect."
        );

        Require(
            decoded.header.version == kUdpProtocolVersion,
            "Decoded UDP version is incorrect."
        );

        Require(
            decoded.header.type == MessageType::UdpPong,
            "Decoded UDP type is incorrect."
        );

        Require(
            decoded.header.sessionToken == original.sessionToken,
            "Decoded session token is incorrect."
        );

        Require(
            decoded.header.playerId == original.playerId,
            "Decoded player ID is incorrect."
        );

        Require(
            decoded.header.sequence == original.sequence,
            "Decoded sequence is incorrect."
        );

        Require(
            decoded.payload == payload,
            "Decoded UDP payload is incorrect."
        );
    }

    void UdpDecodeRejectsInvalidBufferArguments()
    {
        using namespace tdr::protocol;

        bool rejectedShortDatagram = false;

        try
        {
            const std::vector<std::uint8_t> bytes(
                kUdpMessageHeaderSize - 1U
            );

            static_cast<void>(
                UdpPacketCodec::Decode(
                    bytes.data(),
                    bytes.size()
                )
                );
        }
        catch (const std::invalid_argument&)
        {
            rejectedShortDatagram = true;
        }

        Require(
            rejectedShortDatagram,
            "A short UDP datagram was not rejected."
        );
    }

    template<typename Action>
    void RequireUdpPacketError(
        Action action,
        const tdr::protocol::UdpPacketError expected,
        const char* const message
    )
    {
        try
        {
            action();
        }
        catch (
            const tdr::protocol::UdpPacketDecodeException& exception
            )
        {
            Require(
                exception.Code() == expected,
                "UDP packet error code does not match."
            );

            return;
        }
        catch (...)
        {
            throw std::runtime_error(
                "A different exception type was thrown."
            );
        }

        throw std::runtime_error(message);
    }

    std::vector<std::uint8_t> CreateValidUdpPingPacket()
    {
        tdr::protocol::UdpMessageHeader header{};
        header.type = tdr::protocol::MessageType::UdpPing;
        header.playerId = 1U;
        header.sequence = 2U;

        return tdr::protocol::UdpPacketCodec::Encode(
            header,
            {}
        );
    }

    void UdpDecodeRejectsInvalidProtocolHeader()
    {
        using namespace tdr::protocol;

        auto invalidMagic = CreateValidUdpPingPacket();
        invalidMagic[kUdpMagicOffset] = 0x00;

        RequireUdpPacketError(
            [&invalidMagic]()
            {
                static_cast<void>(
                    UdpPacketCodec::Decode(
                        invalidMagic.data(),
                        invalidMagic.size()
                    )
                    );
            },
            UdpPacketError::InvalidMagic,
            "Invalid UDP magic was not rejected."
        );

        auto invalidVersion = CreateValidUdpPingPacket();
        invalidVersion[kUdpVersionOffset] = 0x00;
        invalidVersion[kUdpVersionOffset + 1U] = 0x02;

        RequireUdpPacketError(
            [&invalidVersion]()
            {
                static_cast<void>(
                    UdpPacketCodec::Decode(
                        invalidVersion.data(),
                        invalidVersion.size()
                    )
                    );
            },
            UdpPacketError::UnsupportedVersion,
            "Unsupported UDP version was not rejected."
        );

        auto unknownType = CreateValidUdpPingPacket();
        unknownType[kUdpMessageTypeOffset] = 0x7F;
        unknownType[kUdpMessageTypeOffset + 1U] = 0xFF;

        RequireUdpPacketError(
            [&unknownType]()
            {
                static_cast<void>(
                    UdpPacketCodec::Decode(
                        unknownType.data(),
                        unknownType.size()
                    )
                    );
            },
            UdpPacketError::UnknownMessageType,
            "Unknown UDP message type was not rejected."
        );

        auto tcpType = CreateValidUdpPingPacket();
        tcpType[kUdpMessageTypeOffset] = 0x00;
        tcpType[kUdpMessageTypeOffset + 1U] = 0x01;

        RequireUdpPacketError(
            [&tcpType]()
            {
                static_cast<void>(
                    UdpPacketCodec::Decode(
                        tcpType.data(),
                        tcpType.size()
                    )
                    );
            },
            UdpPacketError::NonUdpMessageType,
            "TCP message type was accepted as UDP."
        );
    }
}

int main()
{
    try
    {
        UdpMessageTypesAreClassifiedCorrectly();

        UdpMessageHeaderUsesStableWireLayout();

        UdpPingEncodesInNetworkByteOrder();

        UdpPongRoundTripPreservesAllFields();

        UdpDecodeRejectsInvalidBufferArguments();

        UdpDecodeRejectsInvalidProtocolHeader();

        std::cout
            << "[PASS] UDP message types are classified correctly."
            << std::endl;

        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr
            << "[FAIL] "
            << exception.what()
            << std::endl;

        return 1;
    }
}