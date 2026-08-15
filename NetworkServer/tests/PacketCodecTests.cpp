#include "protocol/MessageHeader.h"
#include "protocol/PacketCodec.h"

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <vector>
#include <cstddef>

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

    template<typename ExpectedException, typename Action>
    void RequireThrows(
        Action&& action,
        const char* const message
    )
    {
        try
        {
            action();
        }
        catch (const ExpectedException&)
        {
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

    template<typename Action>
    void RequirePacketError(
        Action&& action,
        const tdr::protocol::PacketError expectedCode,
        const char* const message
    )
    {
        try
        {
            action();
        }
        catch (
            const tdr::protocol::PacketDecodeException& exception
            )
        {
            Require(
                exception.Code() == expectedCode,
                "Packet error code does not match."
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

    void EncodeEmptyPayloadUsesExpectedBytes()
    {
        const std::vector<std::uint8_t> payload;

        const auto packet =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                payload
            );

        const std::vector<std::uint8_t> expected
        {
            0x54, 0x44, 0x52, 0x31,
            0x00, 0x01,
            0x00, 0x01,
            0x00, 0x00, 0x00, 0x00
        };

        Require(
            packet == expected,
            "Empty packet bytes do not match the protocol."
        );
    }

    void EncodeAppendsPayload()
    {
        const std::vector<std::uint8_t> payload
        {
            0xAA, 0xBB, 0xCC
        };

        const auto packet =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetNickname,
                payload
            );

        Require(
            packet.size()
            == tdr::protocol::kMessageHeaderSize
            + payload.size(),
            "Encoded packet size is incorrect."
        );

        Require(
            packet[8] == 0x00
            && packet[9] == 0x00
            && packet[10] == 0x00
            && packet[11] == 0x03,
            "Payload size is not encoded in network byte order."
        );

        Require(
            packet[12] == 0xAA
            && packet[13] == 0xBB
            && packet[14] == 0xCC,
            "Payload bytes were not appended correctly."
        );
    }

    void EncodeRejectsUnknownMessageType()
    {
        const std::vector<std::uint8_t> payload;

        RequireThrows<std::invalid_argument>(
            [&payload]()
            {
                static_cast<void>(
                    tdr::protocol::PacketCodec::Encode(
                        static_cast<tdr::protocol::MessageType>(9999),
                        payload
                    )
                );
            },
            "Unknown message type was not rejected."
        );
    }

    void EncodeRejectsOversizedPayload()
    {
        const std::vector<std::uint8_t> payload(
            static_cast<std::size_t>(
                tdr::protocol::kMaxPayloadSize
                ) + 1U
        );

        RequireThrows<std::length_error>(
            [&payload]()
            {
                static_cast<void>(
                    tdr::protocol::PacketCodec::Encode(
                        tdr::protocol::MessageType::ClientHello,
                        payload
                    )
                );
            },
            "Oversized payload was not rejected."
        );
    }

    void EncodeThenDecodeReturnsOriginalPacket()
    {
        const std::vector<std::uint8_t> payload
        {
            'S', 'e', 'e', 'l', 'e'
        };

        const auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetNickname,
                payload
            );

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), encoded.size());

        tdr::protocol::DecodedPacket decoded;

        Require(
            codec.TryDecode(decoded),
            "Complete packet was not decoded."
        );

        Require(
            decoded.type
            == tdr::protocol::MessageType::SetNickname,
            "Decoded message type does not match."
        );

        Require(
            decoded.payload == payload,
            "Decoded payload does not match."
        );

        Require(
            codec.BufferedByteCount() == 0,
            "Decoded packet was not removed from the buffer."
        );
    }

    void DecodeAvailableHandlesStickyPackets()
    {
        const std::vector<std::uint8_t> emptyPayload;
        const std::vector<std::uint8_t> readyPayload{ 1 };

        const auto first =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                emptyPayload
            );

        const auto second =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetReady,
                readyPayload
            );

        std::vector<std::uint8_t> combined;
        combined.reserve(first.size() + second.size());

        combined.insert(
            combined.end(),
            first.begin(),
            first.end()
        );

        combined.insert(
            combined.end(),
            second.begin(),
            second.end()
        );

        tdr::protocol::PacketCodec codec;
        codec.Append(combined.data(), combined.size());

        const auto decoded = codec.DecodeAvailable();

        Require(decoded.size() == 2, "Expected two packets.");
        Require(
            decoded[0].type
            == tdr::protocol::MessageType::ClientHello,
            "First sticky packet type is incorrect."
        );
        Require(
            decoded[1].type
            == tdr::protocol::MessageType::SetReady,
            "Second sticky packet type is incorrect."
        );
        Require(
            decoded[1].payload == readyPayload,
            "Second sticky packet payload is incorrect."
        );
        Require(
            codec.BufferedByteCount() == 0,
            "Sticky packets were not fully consumed."
        );
    }

    void HeaderHalfPacketWaitsForRemainingBytes()
    {
        const std::vector<std::uint8_t> payload;

        const auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                payload
            );

        constexpr std::size_t firstChunkSize = 5;

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), firstChunkSize);

        tdr::protocol::DecodedPacket decoded;

        Require(
            !codec.TryDecode(decoded),
            "Partial header was incorrectly decoded."
        );

        Require(
            codec.BufferedByteCount() == firstChunkSize,
            "Partial header bytes were not preserved."
        );

        codec.Append(
            encoded.data() + firstChunkSize,
            encoded.size() - firstChunkSize
        );

        Require(
            codec.TryDecode(decoded),
            "Completed header packet was not decoded."
        );

        Require(
            decoded.type
            == tdr::protocol::MessageType::ClientHello,
            "Decoded header packet type is incorrect."
        );

        Require(
            codec.BufferedByteCount() == 0,
            "Completed header packet was not consumed."
        );
    }

    void PayloadHalfPacketWaitsForRemainingBytes()
    {
        const std::vector<std::uint8_t> payload
        {
            1, 2, 3, 4, 5, 6, 7, 8
        };

        const auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::SetNickname,
                payload
            );

        const std::size_t firstChunkSize =
            tdr::protocol::kMessageHeaderSize + 3U;

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), firstChunkSize);

        tdr::protocol::DecodedPacket decoded;

        Require(
            !codec.TryDecode(decoded),
            "Partial payload was incorrectly decoded."
        );

        Require(
            codec.BufferedByteCount() == firstChunkSize,
            "Partial payload bytes were not preserved."
        );

        codec.Append(
            encoded.data() + firstChunkSize,
            encoded.size() - firstChunkSize
        );

        Require(
            codec.TryDecode(decoded),
            "Completed payload packet was not decoded."
        );

        Require(
            decoded.payload == payload,
            "Completed payload does not match."
        );

        Require(
            codec.BufferedByteCount() == 0,
            "Completed payload packet was not consumed."
        );
    }

    void DecodeRejectsInvalidMagic()
    {
        const std::vector<std::uint8_t> payload;

        auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                payload
            );

        encoded[tdr::protocol::kMagicOffset] = 0x00;

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), encoded.size());

        tdr::protocol::DecodedPacket decoded;

        RequirePacketError(
            [&codec, &decoded]()
            {
                static_cast<void>(
                    codec.TryDecode(decoded)
                    );
            },
            tdr::protocol::PacketError::InvalidMagic,
            "Invalid protocol magic was not rejected."
        );
    }

    void DecodeRejectsUnsupportedVersion()
    {
        const std::vector<std::uint8_t> payload;

        auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                payload
            );

        encoded[tdr::protocol::kVersionOffset] = 0x00;
        encoded[tdr::protocol::kVersionOffset + 1U] = 0x02;

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), encoded.size());

        tdr::protocol::DecodedPacket decoded;

        RequirePacketError(
            [&codec, &decoded]()
            {
                static_cast<void>(
                    codec.TryDecode(decoded)
                    );
            },
            tdr::protocol::PacketError::UnsupportedVersion,
            "Unsupported protocol version was not rejected."
        );
    }

    void DecodeRejectsUnknownMessageType()
    {
        const std::vector<std::uint8_t> payload;

        auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                payload
            );

        encoded[tdr::protocol::kMessageTypeOffset] = 0x27;
        encoded[
            tdr::protocol::kMessageTypeOffset + 1U
        ] = 0x0F;

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), encoded.size());

        tdr::protocol::DecodedPacket decoded;

        RequirePacketError(
            [&codec, &decoded]()
            {
                static_cast<void>(
                    codec.TryDecode(decoded)
                    );
            },
            tdr::protocol::PacketError::UnknownMessageType,
            "Unknown message type was not rejected."
        );
    }

    void DecodeRejectsOversizedPayloadLength()
    {
        const std::vector<std::uint8_t> payload;

        auto encoded =
            tdr::protocol::PacketCodec::Encode(
                tdr::protocol::MessageType::ClientHello,
                payload
            );

        encoded[tdr::protocol::kPayloadSizeOffset] =
            0x00;

        encoded[
            tdr::protocol::kPayloadSizeOffset + 1U
        ] = 0x01;

        encoded[
            tdr::protocol::kPayloadSizeOffset + 2U
        ] = 0x00;

        encoded[
            tdr::protocol::kPayloadSizeOffset + 3U
        ] = 0x01;

        tdr::protocol::PacketCodec codec;
        codec.Append(encoded.data(), encoded.size());

        tdr::protocol::DecodedPacket decoded;

        RequirePacketError(
            [&codec, &decoded]()
            {
                static_cast<void>(
                    codec.TryDecode(decoded)
                    );
            },
            tdr::protocol::PacketError::PayloadTooLarge,
            "Oversized payload length was not rejected."
        );
    }

    void AppendRejectsReceiveBufferOverflow()
    {
        const std::vector<std::uint8_t> oversizedData(
            tdr::protocol::kMaxReceiveBufferSize + 1U
        );

        tdr::protocol::PacketCodec codec;

        RequirePacketError(
            [&codec, &oversizedData]()
            {
                codec.Append(
                    oversizedData.data(),
                    oversizedData.size()
                );
            },
            tdr::protocol::PacketError::ReceiveBufferOverflow,
            "Receive buffer overflow was not rejected."
        );

        Require(
            codec.BufferedByteCount() == 0,
            "Rejected bytes were added to the receive buffer."
        );
    }

    bool RunTest(
        const char* const name,
        void (*test)()
    )
    {
        try
        {
            test();
            std::cout << "[PASS] " << name << std::endl;
            return true;
        }
        catch (const std::exception& exception)
        {
            std::cerr
                << "[FAIL] " << name
                << ": " << exception.what()
                << std::endl;

            return false;
        }
    }
}

int main()
{
    int failedTests = 0;

    failedTests += !RunTest(
        "EncodeEmptyPayloadUsesExpectedBytes",
        EncodeEmptyPayloadUsesExpectedBytes
    );

    failedTests += !RunTest(
        "EncodeAppendsPayload",
        EncodeAppendsPayload
    );

    failedTests += !RunTest(
        "EncodeRejectsUnknownMessageType",
        EncodeRejectsUnknownMessageType
    );

    failedTests += !RunTest(
        "EncodeRejectsOversizedPayload",
        EncodeRejectsOversizedPayload
    );

    failedTests += !RunTest(
        "EncodeThenDecodeReturnsOriginalPacket",
        EncodeThenDecodeReturnsOriginalPacket
    );

    failedTests += !RunTest(
        "DecodeAvailableHandlesStickyPackets",
        DecodeAvailableHandlesStickyPackets
    );

    failedTests += !RunTest(
        "HeaderHalfPacketWaitsForRemainingBytes",
        HeaderHalfPacketWaitsForRemainingBytes
    );

    failedTests += !RunTest(
        "PayloadHalfPacketWaitsForRemainingBytes",
        PayloadHalfPacketWaitsForRemainingBytes
    );

    failedTests += !RunTest(
        "DecodeRejectsInvalidMagic",
        DecodeRejectsInvalidMagic
    );

    failedTests += !RunTest(
        "DecodeRejectsUnsupportedVersion",
        DecodeRejectsUnsupportedVersion
    );

    failedTests += !RunTest(
        "DecodeRejectsUnknownMessageType",
        DecodeRejectsUnknownMessageType
    );

    failedTests += !RunTest(
        "DecodeRejectsOversizedPayloadLength",
        DecodeRejectsOversizedPayloadLength
    );

    failedTests += !RunTest(
        "AppendRejectsReceiveBufferOverflow",
        AppendRejectsReceiveBufferOverflow
    );

    std::cout
        << "PacketCodec tests finished. Failed: "
        << failedTests
        << std::endl;

    return failedTests == 0 ? 0 : 1;
}