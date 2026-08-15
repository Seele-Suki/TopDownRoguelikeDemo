#include "net/SelectLoop.h"

#include <algorithm>
#include <stdexcept>
#include <utility>

namespace tdr::net
{
    SelectResult::SelectResult(
        std::vector<SOCKET> readableSockets,
        const bool didTimeout,
        const int errorCode
    )
        : readableSockets_(
            std::move(readableSockets)),
        didTimeout_(didTimeout),
        errorCode_(errorCode)
    {
    }

    const std::vector<SOCKET>&
        SelectResult::ReadableSockets() const noexcept
    {
        return readableSockets_;
    }

    bool SelectResult::DidTimeout() const noexcept
    {
        return didTimeout_;
    }

    bool SelectResult::HasError() const noexcept
    {
        return errorCode_ != 0;
    }

    int SelectResult::ErrorCode() const noexcept
    {
        return errorCode_;
    }

    void SelectLoop::AddSocket(
        const SOCKET socket
    )
    {
        if (socket == INVALID_SOCKET)
        {
            throw std::invalid_argument(
                "Cannot add an invalid socket to select."
            );
        }

        const auto existingSocket =
            std::find(
                sockets_.begin(),
                sockets_.end(),
                socket
            );

        if (existingSocket != sockets_.end())
        {
            throw std::invalid_argument(
                "Socket is already managed by select."
            );
        }

        if (sockets_.size() >= FD_SETSIZE)
        {
            throw std::length_error(
                "select socket limit has been reached."
            );
        }

        sockets_.push_back(socket);
    }

    void SelectLoop::RemoveSocket(
        const SOCKET socket
    )
    {
        const auto existingSocket =
            std::find(
                sockets_.begin(),
                sockets_.end(),
                socket
            );

        if (existingSocket == sockets_.end())
        {
            throw std::invalid_argument(
                "Socket is not managed by select."
            );
        }

        sockets_.erase(existingSocket);
    }

    SelectResult SelectLoop::Poll(
        const std::chrono::milliseconds timeout
    ) const
    {
        if (timeout.count() < 0)
        {
            throw std::invalid_argument(
                "select timeout cannot be negative."
            );
        }

        if (sockets_.empty())
        {
            return SelectResult(
                {},
                true,
                0
            );
        }

        fd_set readSet;
        FD_ZERO(&readSet);

        for (const SOCKET socket : sockets_)
        {
            FD_SET(socket, &readSet);
        }

        const auto totalMilliseconds =
            timeout.count();

        timeval timeoutValue{};
        timeoutValue.tv_sec =
            static_cast<long>(
                totalMilliseconds / 1000
                );

        timeoutValue.tv_usec =
            static_cast<long>(
                (totalMilliseconds % 1000) * 1000
                );

        const int selectResult =
            ::select(
                0,
                &readSet,
                nullptr,
                nullptr,
                &timeoutValue
            );

        if (selectResult == SOCKET_ERROR)
        {
            return SelectResult(
                {},
                false,
                ::WSAGetLastError()
            );
        }

        if (selectResult == 0)
        {
            return SelectResult(
                {},
                true,
                0
            );
        }

        std::vector<SOCKET> readableSockets;
        readableSockets.reserve(
            static_cast<std::size_t>(selectResult)
        );

        for (const SOCKET socket : sockets_)
        {
            if (FD_ISSET(socket, &readSet))
            {
                readableSockets.push_back(socket);
            }
        }

        return SelectResult(
            std::move(readableSockets),
            false,
            0
        );
    }
}