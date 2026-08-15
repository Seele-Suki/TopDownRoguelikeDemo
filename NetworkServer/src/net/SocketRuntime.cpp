#include "net/SocketRuntime.h"

#include <WinSock2.h>

#include <stdexcept>
#include <string>

namespace tdr::net
{
    SocketRuntime::SocketRuntime()
    {
        WSADATA socketData{};

        const int result = ::WSAStartup(
            MAKEWORD(2, 2),
            &socketData
        );

        if (result != 0)
        {
            throw std::runtime_error(
                "WSAStartup failed with error code: "
                + std::to_string(result)
            );
        }
    }

    SocketRuntime::~SocketRuntime() noexcept
    {
        ::WSACleanup();
    }
}