#include "net/WorldEntitySpawnForwarder.h"
#include "net/WorldEntityRemovalForwarder.h"
#include "net/TcpClientSession.h"
#include "protocol/PacketCodec.h"
#include "protocol/WorldEntitySpawnedCodec.h"
#include "protocol/WorldEntityRemovedCodec.h"

#include <cstdint>
#include <exception>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace
{
    void Require(
        const bool condition,
        const char* const message
    )
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    template<typename Exception, typename Action>
    void RequireThrows(
        Action&& action,
        const char* const message
    )
    {
        try
        {
            action();
        }
        catch (const Exception&)
        {
            return;
        }

        throw std::runtime_error(message);
    }

    std::vector<std::uint8_t> ToBytes(
        const std::string& value
    )
    {
        return std::vector<std::uint8_t>(
            value.begin(),
            value.end());
    }

    void SendPacket(
        tdr::net::TcpClientSession& session,
        const tdr::protocol::MessageType type,
        const std::vector<std::uint8_t>& payload
    )
    {
        const auto packet =
            tdr::protocol::PacketCodec::Encode(
                type,
                payload);

        session.ReceiveBytes(
            packet.data(),
            packet.size());
    }

    std::vector<std::uint8_t> CreateSpawnPayload()
    {
        using namespace tdr::protocol;

        WorldEntityRecord record{};
        record.entityId = 0x10000001U;
        record.entityType = WorldEntityType::Enemy;
        record.lifecycle = WorldEntityLifecycle::Spawn;
        record.flags = WorldEntityFlags::Active;
        record.currentHealth = 3U;
        record.maxHealth = 3U;
        record.enemyArchetype =
            NetworkEnemyArchetype::Basic;

        return WorldEntitySpawnedCodec::Encode(record);
    }

    struct RoomFixture final
    {
        tdr::room::RoomManager roomManager;
        tdr::room::PlayerIdAllocator playerIdAllocator;
        tdr::room::SessionTokenGenerator tokenGenerator;
        tdr::net::TcpClientSession host;
        tdr::net::TcpClientSession guest;

        RoomFixture()
            : host(
                playerIdAllocator,
                tokenGenerator,
                roomManager),
              guest(
                playerIdAllocator,
                tokenGenerator,
                roomManager)
        {
            SendPacket(
                host,
                tdr::protocol::MessageType::SetNickname,
                ToBytes("Host"));

            SendPacket(
                host,
                tdr::protocol::MessageType::CreateRoomRequest,
                {});

            SendPacket(
                guest,
                tdr::protocol::MessageType::SetNickname,
                ToBytes("Guest"));

            SendPacket(
                guest,
                tdr::protocol::MessageType::JoinRoomRequest,
                {});

            static_cast<void>(host.TakeOutgoingPackets());
            static_cast<void>(guest.TakeOutgoingPackets());
            static_cast<void>(host.TakeChangedRoomIds());
            static_cast<void>(guest.TakeChangedRoomIds());
        }

        void StartRoom()
        {
            SendPacket(
                host,
                tdr::protocol::MessageType::SetPlayerSelection,
                {
                    static_cast<std::uint8_t>(
                        tdr::room::CharacterId::Ranged),
                    static_cast<std::uint8_t>(
                        tdr::room::DifficultyId::Normal)
                });

            SendPacket(
                guest,
                tdr::protocol::MessageType::SetPlayerSelection,
                {
                    static_cast<std::uint8_t>(
                        tdr::room::CharacterId::Melee),
                    static_cast<std::uint8_t>(
                        tdr::room::DifficultyId::None)
                });

            SendPacket(
                host,
                tdr::protocol::MessageType::SetReady,
                { 1U });

            SendPacket(
                guest,
                tdr::protocol::MessageType::SetReady,
                { 1U });

            SendPacket(
                host,
                tdr::protocol::MessageType::StartGameRequest,
                {});

            static_cast<void>(host.TakeChangedRoomIds());
            static_cast<void>(guest.TakeChangedRoomIds());
            static_cast<void>(host.TakeStartedRoomIds());
        }
    };

    void HostSpawnTargetsGuestAndPreservesPayload()
    {
        RoomFixture fixture;
        fixture.StartRoom();

        const auto payload = CreateSpawnPayload();
        const auto forwarded =
            tdr::net::WorldEntitySpawnForwarder::Forward(
                fixture.host,
                payload);

        Require(
            forwarded.targetPlayerId ==
                fixture.guest.PlayerId(),
            "Enemy spawn did not target the guest."
        );

        Require(
            forwarded.payload == payload,
            "Enemy spawn payload changed while forwarding."
        );
    }

    void GuestCannotForwardSpawn()
    {
        RoomFixture fixture;
        fixture.StartRoom();
        const auto payload = CreateSpawnPayload();

        RequireThrows<std::invalid_argument>(
            [&fixture, &payload]()
            {
                static_cast<void>(
                    tdr::net::WorldEntitySpawnForwarder::Forward(
                        fixture.guest,
                        payload));
            },
            "Guest was allowed to forward an enemy spawn."
        );
    }

    void WaitingRoomCannotForwardSpawn()
    {
        RoomFixture fixture;
        const auto payload = CreateSpawnPayload();

        RequireThrows<std::runtime_error>(
            [&fixture, &payload]()
            {
                static_cast<void>(
                    tdr::net::WorldEntitySpawnForwarder::Forward(
                        fixture.host,
                        payload));
            },
            "Waiting room forwarded an enemy spawn."
        );
    }

    void InvalidPayloadCannotForward()
    {
        RoomFixture fixture;
        fixture.StartRoom();
        auto payload = CreateSpawnPayload();
        payload[5] = static_cast<std::uint8_t>(
            tdr::protocol::WorldEntityLifecycle::Update);

        RequireThrows<std::invalid_argument>(
            [&fixture, &payload]()
            {
                static_cast<void>(
                    tdr::net::WorldEntitySpawnForwarder::Forward(
                        fixture.host,
                        payload));
            },
            "Invalid enemy spawn payload was forwarded."
        );
    }

    void TcpSessionQueuesSpawnPayload()
    {
        RoomFixture fixture;
        fixture.StartRoom();
        const auto payload = CreateSpawnPayload();

        SendPacket(
            fixture.host,
            tdr::protocol::MessageType::WorldEntitySpawned,
            payload);

        const auto queued =
            fixture.host.TakeWorldEntitySpawnPayloads();

        Require(
            queued.size() == 1U &&
            queued.front() == payload,
            "TCP session did not queue the enemy spawn payload."
        );

        Require(
            fixture.host.TakeWorldEntitySpawnPayloads().empty(),
            "TCP session did not clear queued spawn payloads."
        );
    }

    std::vector<std::uint8_t> CreateRemovalPayload()
    {
        using namespace tdr::protocol;

        return WorldEntityRemovedCodec::Encode({
            0x10000001U,
            WorldEntityType::Enemy,
            WorldEntityRemovalReason::Died
        });
    }

    void HostRemovalTargetsGuestAndPreservesPayload()
    {
        RoomFixture fixture;
        fixture.StartRoom();
        const auto payload = CreateRemovalPayload();

        const auto forwarded =
            tdr::net::WorldEntityRemovalForwarder::Forward(
                fixture.host,
                payload);

        Require(
            forwarded.targetPlayerId == fixture.guest.PlayerId() &&
            forwarded.payload == payload,
            "Enemy removal was not forwarded to the guest."
        );
    }

    void GuestCannotForwardRemoval()
    {
        RoomFixture fixture;
        fixture.StartRoom();
        const auto payload = CreateRemovalPayload();

        RequireThrows<std::invalid_argument>(
            [&fixture, &payload]()
            {
                static_cast<void>(
                    tdr::net::WorldEntityRemovalForwarder::Forward(
                        fixture.guest,
                        payload));
            },
            "Guest was allowed to forward enemy removal."
        );
    }

    void TcpSessionQueuesRemovalPayload()
    {
        RoomFixture fixture;
        fixture.StartRoom();
        const auto payload = CreateRemovalPayload();

        SendPacket(
            fixture.host,
            tdr::protocol::MessageType::WorldEntityRemoved,
            payload);

        const auto queued =
            fixture.host.TakeWorldEntityRemovalPayloads();

        Require(
            queued.size() == 1U && queued.front() == payload,
            "TCP session did not queue enemy removal."
        );
    }
}

int main()
{
    try
    {
        HostSpawnTargetsGuestAndPreservesPayload();
        GuestCannotForwardSpawn();
        WaitingRoomCannotForwardSpawn();
        InvalidPayloadCannotForward();
        TcpSessionQueuesSpawnPayload();
        HostRemovalTargetsGuestAndPreservesPayload();
        GuestCannotForwardRemoval();
        TcpSessionQueuesRemovalPayload();
        std::cout << "WorldEntitySpawnForwarder tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << exception.what() << '\n';
        return 1;
    }
}
