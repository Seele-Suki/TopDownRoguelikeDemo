#include "room/ClientState.h"
#include "protocol/PacketCodec.h"

#include <iostream>
#include <string>
#include <vector>

int main()
{
    const std::vector<std::uint8_t> nickname
    {
        'S', 'e', 'e', 'l', 'e'
    };

    const auto encoded =
        tdr::protocol::PacketCodec::Encode(
            tdr::protocol::MessageType::SetNickname,
            nickname
        );

    tdr::protocol::PacketCodec codec;
    codec.Append(
        encoded.data(),
        encoded.size()
    );

    tdr::protocol::DecodedPacket packet;

    if (!codec.TryDecode(packet))
    {
        std::cerr
            << "[FAIL] SetNickname packet was not decoded."
            << std::endl;

        return 1;
    }

    tdr::room::ClientState client;

    client.HandlePacket(packet);

    if (client.Nickname() != "Seele")
    {
        std::cerr
            << "[FAIL] Client nickname was not stored."
            << std::endl;

        return 1;
    }

    std::cout
        << "[PASS] ClientState stored the nickname."
        << std::endl;

    return 0;
}