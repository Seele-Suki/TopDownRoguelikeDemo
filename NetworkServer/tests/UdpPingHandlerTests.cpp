#include "net/ServerCoordinator.h"
#include "net/SocketRuntime.h"
#include "net/TcpConnection.h"
#include "net/UdpPingHandler.h"
#include "protocol/UdpPacketCodec.h"

#include <WinSock2.h>

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <utility>

namespace
{
    sockaddr_in6 CreateUdpAddress()
    {
        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_addr = in6addr_loopback;
        address.sin6_port = ::htons(45001U);
        return address;
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;
        tdr::net::ServerCoordinator server;

        const SOCKET rawSocket =
            ::socket(
                AF_INET6,
                SOCK_STREAM,
                IPPROTO_TCP
            );

        if (rawSocket == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Could not create the test TCP socket."
            );
        }

        tdr::net::TcpConnection connection(
            rawSocket
        );

        auto& session =
            server.AttachConnection(
                std::move(connection)
            );

        const sockaddr_in6 udpAddress =
            CreateUdpAddress();

        tdr::protocol::UdpMessageHeader bindHeader{};
        bindHeader.type =
            tdr::protocol::MessageType::UdpBindRequest;
        bindHeader.sessionToken =
            session.SessionTokenBytes();
        bindHeader.playerId = session.PlayerId();
        bindHeader.sequence = 1U;

        server.BindUdpEndpoint(
            bindHeader,
            udpAddress
        );

        tdr::net::UdpPingHandler handler(
            server
        );

        auto pingHeader = bindHeader;
        pingHeader.type =
            tdr::protocol::MessageType::UdpPing;
        pingHeader.sequence = 2U;

        const auto encodedPing =
            tdr::protocol::UdpPacketCodec::Encode(
                pingHeader,
                {}
            );

        const auto encodedPong =
            handler.Handle(
                encodedPing.data(),
                encodedPing.size(),
                udpAddress
            );

        const auto decodedPong =
            tdr::protocol::UdpPacketCodec::Decode(
                encodedPong.data(),
                encodedPong.size()
            );

        if (decodedPong.header.type
                != tdr::protocol::MessageType::UdpPong
            || decodedPong.header.sessionToken
                != pingHeader.sessionToken
            || decodedPong.header.playerId
                != pingHeader.playerId
            || decodedPong.header.sequence
                != pingHeader.sequence
            || !decodedPong.payload.empty())
        {
            std::cerr
                << "[FAIL] Valid UDP Ping did not "
                << "produce the expected Pong."
                << std::endl;

            return 1;
        }

        auto expectRejected =
            [&handler](
                const tdr::protocol::UdpMessageHeader& header,
                const std::vector<std::uint8_t>& payload,
                const sockaddr_in6& sourceAddress,
                const char* const description
            )
            {
                const auto encodedRequest =
                    tdr::protocol::UdpPacketCodec::Encode(
                        header,
                        payload
                    );

                try
                {
                    static_cast<void>(
                        handler.Handle(
                            encodedRequest.data(),
                            encodedRequest.size(),
                            sourceAddress
                        )
                    );
                }
                catch (const std::invalid_argument&)
                {
                    return true;
                }

                std::cerr
                    << "[FAIL] UdpPingHandler accepted "
                    << description
                    << "."
                    << std::endl;

                return false;
            };

        auto invalidTokenHeader = pingHeader;
        invalidTokenHeader.sessionToken[0] ^= 0xFFU;
        invalidTokenHeader.sequence = 3U;

        if (!expectRejected(
                invalidTokenHeader,
                {},
                udpAddress,
                "an invalid session token"))
        {
            return 1;
        }

        if (!expectRejected(
                pingHeader,
                {},
                udpAddress,
                "a duplicate sequence"))
        {
            return 1;
        }

        auto wrongSourceHeader = pingHeader;
        wrongSourceHeader.sequence = 4U;

        auto wrongSource =
            CreateUdpAddress();
        wrongSource.sin6_port =
            ::htons(45002U);

        if (!expectRejected(
                wrongSourceHeader,
                {},
                wrongSource,
                "a wrong source address"))
        {
            return 1;
        }

        auto payloadHeader = pingHeader;
        payloadHeader.sequence = 4U;

        if (!expectRejected(
                payloadHeader,
                { 0x01U },
                udpAddress,
                "a non-empty payload"))
        {
            return 1;
        }

        server.RemoveConnection(rawSocket);

        std::cout
            << "[PASS] UDP Ping produced a matching Pong."
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
