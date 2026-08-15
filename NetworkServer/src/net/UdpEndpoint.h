#pragma once

#include <WinSock2.h>
#include <WS2tcpip.h>

#include <cstring>
#include <stdexcept>

namespace tdr::net
{
    class UdpEndpoint final
    {
    public:
        explicit UdpEndpoint(
            const sockaddr_in6& address
        )
            : address_(address)
        {
            if (address_.sin6_family != AF_INET6)
            {
                throw std::invalid_argument(
                    "UDP endpoint must use AF_INET6."
                );
            }
        }

        [[nodiscard]]
        bool Matches(
            const sockaddr_in6& other
        ) const noexcept
        {
            return other.sin6_family == AF_INET6
                && address_.sin6_port
                == other.sin6_port
                && address_.sin6_scope_id
                == other.sin6_scope_id
                && std::memcmp(
                    &address_.sin6_addr,
                    &other.sin6_addr,
                    sizeof(IN6_ADDR)
                ) == 0;
        }

        [[nodiscard]]
        const sockaddr_in6& Address() const noexcept
        {
            return address_;
        }

    private:
        sockaddr_in6 address_{};
    };
}