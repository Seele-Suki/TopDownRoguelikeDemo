#include "net/SocketRuntime.h"
#include "net/TcpListener.h"

#include <exception>
#include <iostream>
#include <chrono>
#include <string>
#include <stdexcept>
#include <array>
#include <cstdint>

namespace
{
    class TestSocket final
    {
    public:
        explicit TestSocket(
            const SOCKET socket
        ) noexcept
            : socket_(socket)
        {
        }

        ~TestSocket() noexcept
        {
            if (socket_ != INVALID_SOCKET)
            {
                ::closesocket(socket_);
            }
        }

        TestSocket(const TestSocket&) = delete;
        TestSocket& operator=(const TestSocket&) = delete;

        TestSocket(TestSocket&& other) noexcept
            : socket_(other.socket_)
        {
            other.socket_ = INVALID_SOCKET;
        }

        TestSocket& operator=(TestSocket&& other) noexcept
        {
            if (this != &other)
            {
                if (socket_ != INVALID_SOCKET)
                {
                    ::closesocket(socket_);
                }

                socket_ = other.socket_;
                other.socket_ = INVALID_SOCKET;
            }

            return *this;
        }

        [[nodiscard]]
        SOCKET NativeHandle() const noexcept
        {
            return socket_;
        }

    private:
        SOCKET socket_ = INVALID_SOCKET;
    };

    TestSocket ConnectLoopbackClient(
        const unsigned short port
    )
    {
        const SOCKET socket =
            ::socket(
                AF_INET6,
                SOCK_STREAM,
                IPPROTO_TCP
            );

        if (socket == INVALID_SOCKET)
        {
            throw std::runtime_error(
                "Failed to create test client socket. "
                "WSA error code: "
                + std::to_string(::WSAGetLastError())
            );
        }

        sockaddr_in6 address{};
        address.sin6_family = AF_INET6;
        address.sin6_port = ::htons(port);

        const int addressResult =
            ::inet_pton(
                AF_INET6,
                "::1",
                &address.sin6_addr
            );

        if (addressResult != 1)
        {
            ::closesocket(socket);

            throw std::runtime_error(
                "Failed to parse IPv6 loopback address."
            );
        }

        const int connectResult =
            ::connect(
                socket,
                reinterpret_cast<const sockaddr*>(
                    &address),
                sizeof(address)
            );

        if (connectResult == SOCKET_ERROR)
        {
            const int errorCode =
                ::WSAGetLastError();

            ::closesocket(socket);

            throw std::runtime_error(
                "Failed to connect test client. "
                "WSA error code: "
                + std::to_string(errorCode)
            );
        }

        return TestSocket(socket);
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;
        tdr::net::TcpListener listener;

        if (!listener.IsValid())
        {
            std::cerr
                << "[FAIL] IPv6 TCP socket is invalid."
                << std::endl;

            return 1;
        }

        if (!listener.IsDualStackEnabled())
        {
            std::cerr
                << "[FAIL] IPv6 dual-stack mode is disabled."
                << std::endl;

            return 1;
        }

        listener.BindAndListen(0);

        if (!listener.IsListening())
        {
            std::cerr
                << "[FAIL] TCP listener did not enter listening state."
                << std::endl;

            return 1;
        }

        if (listener.BoundPort() == 0)
        {
            std::cerr
                << "[FAIL] Windows did not assign a TCP port."
                << std::endl;

            return 1;
        }

        const TestSocket testClient =
            ConnectLoopbackClient(
                listener.BoundPort()
            );

        auto connection =
            listener.Accept();

        if (!connection.IsValid())
        {
            std::cerr
                << "[FAIL] Accepted TCP connection is invalid."
                << std::endl;

            return 1;
        }

        const std::array<std::uint8_t, 5>
            expectedBytes
        {
            0x54,
            0x44,
            0x52,
            0x01,
            0x02
        };

        connection.SendAll(
            expectedBytes.data(),
            expectedBytes.size()
        );

        std::array<std::uint8_t, 5>
            receivedBytes{};

        std::size_t receivedByteCount = 0;

        while (receivedByteCount
            < receivedBytes.size())
        {
            const int result =
                ::recv(
                    testClient.NativeHandle(),
                    reinterpret_cast<char*>(
                        receivedBytes.data()
                        + receivedByteCount),
                    static_cast<int>(
                        receivedBytes.size()
                        - receivedByteCount),
                    0
                );

            if (result == 0)
            {
                std::cerr
                    << "[FAIL] Server closed the connection "
                    << "before sending all bytes."
                    << std::endl;

                return 1;
            }

            if (result == SOCKET_ERROR)
            {
                std::cerr
                    << "[FAIL] Test client recv failed. "
                    << "WSA error code: "
                    << ::WSAGetLastError()
                    << std::endl;

                return 1;
            }

            receivedByteCount +=
                static_cast<std::size_t>(result);
        }

        if (receivedBytes != expectedBytes)
        {
            std::cerr
                << "[FAIL] TCP client received "
                << "incorrect bytes."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] TcpConnection sent all bytes "
            << "to an IPv6 loopback client."
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