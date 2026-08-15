#include "protocol/PacketCodec.h"

#include <cstdint>
#include <iostream>
#include <string>
#include <vector>

int main()
{
    const std::vector<std::uint8_t> nickname
    {
        'S', 'e', 'e', 'l', 'e'
    };

    const auto packet =
        tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::SetNickname,
            nickname
        );

    tdr::protocol::PacketCodec codec;
    codec.Append(
        packet.data(),
        packet.size()
    );

    tdr::protocol::DecodedPacket decoded;

    if (!codec.TryDecode(decoded))
    {
        std::cerr
            << "[FAIL] SetNickname packet was not decoded."
            << std::endl;

        return 1;
    }

    if (decoded.type
        != tdr::protocol::MessageType::SetNickname)
    {
        std::cerr
            << "[FAIL] Decoded message type is incorrect."
            << std::endl;

        return 1;
    }

    const std::string decodedNickname(
        decoded.payload.begin(),
        decoded.payload.end()
    );

    if (decodedNickname != "Seele")
    {
        std::cerr
            << "[FAIL] Decoded nickname is incorrect."
            << std::endl;

        return 1;
    }

    std::cout
        << "[PASS] SetNickname packet payload was decoded."
        << std::endl;

    return 0;
}