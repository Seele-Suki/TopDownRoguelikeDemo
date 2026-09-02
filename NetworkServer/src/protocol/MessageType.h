#pragma once

#include <cstdint>

namespace tdr::protocol
{
    enum class MessageType : std::uint16_t
    {
        Invalid = 0,

        ClientHello = 1,
        ServerHello = 2,
        SetNickname = 3,

        CreateRoomRequest = 10,
        CreateRoomResponse = 11,
        JoinRoomRequest = 12,
        JoinRoomResponse = 13,
        RoomStateSnapshot = 14,
        SetPlayerSelection = 15,
        SetReady = 16,
        StartGameRequest = 17,
        GameStarted = 18,
        LeaveRoom = 19,
        ErrorMessage = 20,

        UdpBindRequest = 30,
        UdpBindAccepted = 31,
        UdpPing = 32,
        UdpPong = 33,

        PlayerInput = 34,
        PlayerStateSnapshot = 35,
        PlayerShotEvent = 36,
        PlayerShotgunEvent = 37,
        WorldStateSnapshot = 40,

        WorldEntitySpawned = 41,
        WorldEntityRemoved = 42,
        PlayerDied = 43,
        ExperienceOrbSpawned = 44,
        ExperienceOrbCollected = 45,
        UpgradeStarted = 46,
        UpgradeChoiceSubmitted = 47,
        UpgradeCompleted = 48,
        BossPhaseChanged = 49,
        GameResult = 50,
        SharedExperienceSnapshot = 51
    };

    [[nodiscard]]
    constexpr bool IsKnownMessageType(
        const MessageType type
    ) noexcept
    {
        switch (type)
        {
        case MessageType::ClientHello:
        case MessageType::ServerHello:
        case MessageType::SetNickname:
        case MessageType::CreateRoomRequest:
        case MessageType::CreateRoomResponse:
        case MessageType::JoinRoomRequest:
        case MessageType::JoinRoomResponse:
        case MessageType::RoomStateSnapshot:
        case MessageType::SetPlayerSelection:
        case MessageType::SetReady:
        case MessageType::StartGameRequest:
        case MessageType::GameStarted:
        case MessageType::LeaveRoom:
        case MessageType::ErrorMessage:
        case MessageType::UdpBindRequest:
        case MessageType::UdpBindAccepted:
        case MessageType::UdpPing:
        case MessageType::UdpPong:
        case MessageType::PlayerInput:
        case MessageType::PlayerStateSnapshot:
        case MessageType::PlayerShotEvent:
        case MessageType::PlayerShotgunEvent:
        case MessageType::WorldStateSnapshot:
        case MessageType::WorldEntitySpawned:
        case MessageType::WorldEntityRemoved:
        case MessageType::PlayerDied:
        case MessageType::ExperienceOrbSpawned:
        case MessageType::ExperienceOrbCollected:
        case MessageType::UpgradeStarted:
        case MessageType::UpgradeChoiceSubmitted:
        case MessageType::UpgradeCompleted:
        case MessageType::BossPhaseChanged:
        case MessageType::GameResult:
        case MessageType::SharedExperienceSnapshot:
            return true;

        case MessageType::Invalid:
        default:
            return false;
        }
    }

    [[nodiscard]]
    constexpr bool IsUdpMessageType(
        const MessageType type
    ) noexcept
    {
        switch (type)
        {
        case MessageType::UdpBindRequest:
        case MessageType::UdpBindAccepted:
        case MessageType::UdpPing:
        case MessageType::UdpPong:
        case MessageType::PlayerInput:
        case MessageType::PlayerStateSnapshot:
        case MessageType::PlayerShotEvent:
        case MessageType::PlayerShotgunEvent:
        case MessageType::WorldStateSnapshot:
            return true;

        default:
            return false;
        }
    }

    static_assert(
        sizeof(MessageType) == sizeof(std::uint16_t),
        "MessageType must use a 16-bit underlying type."
        );
}
