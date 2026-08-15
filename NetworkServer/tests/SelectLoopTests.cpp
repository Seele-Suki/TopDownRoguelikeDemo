#include "net/SelectLoop.h"
#include "net/SocketRuntime.h"
#include "net/TcpListener.h"

#include <chrono>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>

namespace
{
    class TestSocket final
    {
    public:
        explicit TestSocket(
            const SOCKET socket
        )
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
        SOCKET Handle() const noexcept
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
                + std::to_string(
                    ::WSAGetLastError())
            );
        }

        TestSocket client(socket);

        sockaddr_in6 serverAddress{};
        serverAddress.sin6_family = AF_INET6;
        serverAddress.sin6_port = ::htons(port);

        const int addressResult =
            ::inet_pton(
                AF_INET6,
                "::1",
                &serverAddress.sin6_addr
            );

        if (addressResult != 1)
        {
            throw std::runtime_error(
                "Failed to parse IPv6 loopback address."
            );
        }

        const int connectResult =
            ::connect(
                client.Handle(),
                reinterpret_cast<const sockaddr*>(
                    &serverAddress),
                sizeof(serverAddress)
            );

        if (connectResult == SOCKET_ERROR)
        {
            throw std::runtime_error(
                "Failed to connect test client. "
                "WSA error code: "
                + std::to_string(
                    ::WSAGetLastError())
            );
        }

        return client;
    }
}

int main()
{
    try
    {
        tdr::net::SocketRuntime socketRuntime;

        tdr::net::TcpListener listener;
        listener.BindAndListen(0);

        tdr::net::SelectLoop selectLoop;
        selectLoop.AddSocket(
            listener.NativeHandle());

        selectLoop.RemoveSocket(
            listener.NativeHandle());

        const auto removedResult =
            selectLoop.Poll(
                std::chrono::milliseconds(0)
            );

        if (removedResult.HasError())
        {
            std::cerr
                << "[FAIL] select returned an error "
                << "after removing the listener."
                << std::endl;

            return 1;
        }

        if (!removedResult.DidTimeout())
        {
            std::cerr
                << "[FAIL] Empty select loop did not time out."
                << std::endl;

            return 1;
        }

        selectLoop.AddSocket(
            listener.NativeHandle());

        const auto emptyResult =
            selectLoop.Poll(
                std::chrono::milliseconds(0)
            );

        if (emptyResult.HasError())
        {
            std::cerr
                << "[FAIL] Initial select returned error "
                << emptyResult.ErrorCode()
                << "."
                << std::endl;

            return 1;
        }

        if (!emptyResult.DidTimeout())
        {
            std::cerr
                << "[FAIL] Initial select did not time out."
                << std::endl;

            return 1;
        }

        const TestSocket client =
            ConnectLoopbackClient(
                listener.BoundPort());

        const auto connectionResult =
            selectLoop.Poll(
                std::chrono::milliseconds(1000)
            );

        if (connectionResult.HasError())
        {
            std::cerr
                << "[FAIL] Connection select returned error "
                << connectionResult.ErrorCode()
                << "."
                << std::endl;

            return 1;
        }

        if (connectionResult.DidTimeout())
        {
            std::cerr
                << "[FAIL] select did not detect "
                << "the pending connection."
                << std::endl;

            return 1;
        }

        const auto& readableSockets =
            connectionResult.ReadableSockets();

        if (readableSockets.size() != 1
            || readableSockets.front()
            != listener.NativeHandle())
        {
            std::cerr
                << "[FAIL] Listener was not reported "
                << "as readable."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] select detected an IPv6 "
            << "loopback connection."
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