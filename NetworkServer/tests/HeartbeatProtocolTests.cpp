#include "protocol/MessageType.h"
#include "protocol/HeartbeatTiming.h"

#include <cstdint>
#include <iostream>
#include <stdexcept>

namespace
{
    void Require(const bool condition, const char* message)
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }
}

int main()
{
    try
    {
        using namespace tdr::protocol;

        Require(
            static_cast<std::uint16_t>(
                MessageType::TcpHeartbeatRequest) == 21U,
            "TCP heartbeat request wire value changed.");

        Require(
            static_cast<std::uint16_t>(
                MessageType::TcpHeartbeatResponse) == 22U,
            "TCP heartbeat response wire value changed.");

        Require(
            IsKnownMessageType(
                MessageType::TcpHeartbeatRequest),
            "TCP heartbeat request is not a known message.");

        Require(
            !IsUdpMessageType(
                MessageType::TcpHeartbeatRequest),
            "TCP heartbeat request was classified as UDP.");

        Require(
            kHeartbeatIntervalSeconds == 2.0,
            "Heartbeat interval changed.");

        Require(
            kHeartbeatTimeoutSeconds == 6.0,
            "Heartbeat timeout changed.");

        std::cout << "Heartbeat protocol tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << "[FAIL] " << exception.what() << '\n';
        return 1;
    }
}
