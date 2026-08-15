#include "net/SocketRuntime.h"
#include "net/UdpSocket.h"

#include <exception>
#include <iostream>
#include <algorithm>
#include <array>
#include <cstdint>
#include <stdexcept>

namespace
{
    sockaddr_in6 CreateLoopbackAddress(
        const unsigned short port
    )
    {
        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_addr = in6addr_loopback;
        address.sin6_port = ::htons(port);

        return address;
    }

    void SetReceiveTimeout(
        const SOCKET socket
    )
    {
        const DWORD timeoutMilliseconds = 2000U;

        const int result =
            ::setsockopt(
                socket,
                SOL_SOCKET,
                SO_RCVTIMEO,
                reinterpret_cast<const char*>(
                    &timeoutMilliseconds),
                sizeof(timeoutMilliseconds)
            );

        if (result == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "Failed to set UDP receive timeout."
            );
        }
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;
        tdr::net::UdpSocket udpSocket;

        if (!udpSocket.IsValid())
        {
            std::cerr
                << "[FAIL] IPv6 UDP socket is invalid."
                << std::endl;

            return 1;
        }

        if (!udpSocket.IsDualStackEnabled())
        {
            std::cerr
                << "[FAIL] IPv6 UDP dual-stack mode "
                << "is disabled."
                << std::endl;

            return 1;
        }

        udpSocket.Bind(0);

        if (!udpSocket.IsBound())
        {
            std::cerr
                << "[FAIL] UDP socket did not enter bound state."
                << std::endl;

            return 1;
        }

        if (udpSocket.BoundPort() == 0)
        {
            std::cerr
                << "[FAIL] Windows did not assign a UDP port."
                << std::endl;

            return 1;
        }

        tdr::net::UdpSocket clientSocket;
        clientSocket.Bind(0);

        SetReceiveTimeout(
            udpSocket.NativeHandle()
        );

        SetReceiveTimeout(
            clientSocket.NativeHandle()
        );

        const sockaddr_in6 serverAddress =
            CreateLoopbackAddress(
                udpSocket.BoundPort()
            );

        const std::array<std::uint8_t, 4> ping
        {
            'P', 'I', 'N', 'G'
        };

        const std::size_t sentPingBytes =
            clientSocket.SendTo(
                ping.data(),
                ping.size(),
                serverAddress
            );

        if (sentPingBytes != ping.size())
        {
            std::cerr
                << "[FAIL] UDP client sent an "
                << "unexpected byte count."
                << std::endl;

            return 1;
        }

        std::array<std::uint8_t, 64> serverBuffer{};
        sockaddr_in6 clientAddress{};

        const std::size_t receivedPingBytes =
            udpSocket.ReceiveFrom(
                serverBuffer.data(),
                serverBuffer.size(),
                clientAddress
            );

        if (receivedPingBytes != ping.size()
            || !std::equal(
                ping.begin(),
                ping.end(),
                serverBuffer.begin()))
        {
            std::cerr
                << "[FAIL] UDP server received "
                << "incorrect Ping bytes."
                << std::endl;

            return 1;
        }

        const std::array<std::uint8_t, 4> pong
        {
            'P', 'O', 'N', 'G'
        };

        const std::size_t sentPongBytes =
            udpSocket.SendTo(
                pong.data(),
                pong.size(),
                clientAddress
            );

        if (sentPongBytes != pong.size())
        {
            std::cerr
                << "[FAIL] UDP server sent an "
                << "unexpected byte count."
                << std::endl;

            return 1;
        }

        std::array<std::uint8_t, 64> clientBuffer{};
        sockaddr_in6 pongSourceAddress{};

        const std::size_t receivedPongBytes =
            clientSocket.ReceiveFrom(
                clientBuffer.data(),
                clientBuffer.size(),
                pongSourceAddress
            );

        if (receivedPongBytes != pong.size()
            || !std::equal(
                pong.begin(),
                pong.end(),
                clientBuffer.begin()))
        {
            std::cerr
                << "[FAIL] UDP client received "
                << "incorrect Pong bytes."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] IPv6 dual-stack UDP socket "
            << "was bound on port "
            << udpSocket.BoundPort()
            << "."
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