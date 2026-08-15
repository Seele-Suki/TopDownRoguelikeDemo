#include "net/ServerCoordinator.h"
#include "net/ServerLoop.h"
#include "net/SocketRuntime.h"
#include "net/TcpListener.h"
#include "net/UdpSocket.h"

#include <conio.h>
#include <chrono>
#include <ctime>
#include <exception>
#include <iomanip>
#include <iostream>
#include <sstream>
#include <string>
#include <stdexcept>

namespace
{
    constexpr unsigned short kDefaultTcpPort = 7777;

    unsigned short ParseTcpPort(
        const int argumentCount,
        char* arguments[]
    )
    {
        if (argumentCount == 1)
        {
            return kDefaultTcpPort;
        }

        if (argumentCount != 2)
        {
            throw std::invalid_argument(
                "Usage: NetworkServer.exe [tcp-port]"
            );
        }

        const std::string text(arguments[1]);
        std::size_t parsedCharacterCount = 0;

        const unsigned long value =
            std::stoul(
                text,
                &parsedCharacterCount,
                10
            );

        if (parsedCharacterCount != text.size()
            || value == 0
            || value > 65535)
        {
            throw std::invalid_argument(
                "TCP port must be between 1 and 65535."
            );
        }

        return static_cast<unsigned short>(value);
    }

    std::string GetCurrentTimestamp()
    {
        const auto now = std::chrono::system_clock::now();
        const std::time_t currentTime =
            std::chrono::system_clock::to_time_t(now);

        std::tm localTime{};

        const errno_t result = ::localtime_s(
            &localTime,
            &currentTime
        );

        if (result != 0)
        {
            return "unknown-time";
        }

        std::ostringstream stream;

        stream << std::put_time(
            &localTime,
            "%Y-%m-%d %H:%M:%S"
        );

        return stream.str();
    }

    void LogInfo(const std::string& message)
    {
        std::cout
            << "[" << GetCurrentTimestamp() << "] "
            << "[INFO] "
            << message
            << std::endl;
    }

    void LogError(const std::string& message)
    {
        std::cerr
            << "[" << GetCurrentTimestamp() << "] "
            << "[ERROR] "
            << message
            << std::endl;
    }
}

int main(
    const int argumentCount,
    char* arguments[]
)
{
    try
    {
        const unsigned short tcpPort =
            ParseTcpPort(
                argumentCount,
                arguments
            );

        LogInfo("NetworkServer is starting.");

        {
            tdr::net::SocketRuntime socketRuntime;

            LogInfo(
                "Winsock initialized successfully."
            );

            tdr::net::TcpListener listener;
            listener.BindAndListen(tcpPort);

            LogInfo(
                "TCP server is listening on [::]:"
                + std::to_string(
                    listener.BoundPort())
            );

            tdr::net::UdpSocket udpSocket;
            udpSocket.Bind(tcpPort);

            LogInfo(
                "UDP server is listening on [::]:"
                + std::to_string(
                    udpSocket.BoundPort())
            );

            tdr::net::ServerCoordinator coordinator;

            tdr::net::ServerLoop serverLoop(
                listener,
                udpSocket,
                coordinator
            );

            LogInfo(
                "Press Enter to stop the server."
            );

            serverLoop.RunUntil(
                []()
                {
                    if (::_kbhit() == 0)
                    {
                        return false;
                    }

                    const int key = ::_getch();

                    return key == '\r'
                        || key == '\n';
                },
                std::chrono::milliseconds(50)
            );

            LogInfo(
                "Shutdown requested by console input."
            );
        }

        LogInfo(
            "Winsock cleaned up successfully."
        );

        LogInfo("NetworkServer stopped.");

        return 0;
    }
    catch (const std::exception& exception)
    {
        LogError(
            std::string("Fatal error: ")
            + exception.what()
        );

        return 1;
    }
}
