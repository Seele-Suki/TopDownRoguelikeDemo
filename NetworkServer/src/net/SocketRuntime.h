#pragma once

namespace tdr::net
{
    class SocketRuntime final
    {
    public:
        SocketRuntime();
        ~SocketRuntime() noexcept;

        SocketRuntime(const SocketRuntime&) = delete;
        SocketRuntime& operator=(const SocketRuntime&) = delete;

        SocketRuntime(SocketRuntime&&) = delete;
        SocketRuntime& operator=(SocketRuntime&&) = delete;
    };
}