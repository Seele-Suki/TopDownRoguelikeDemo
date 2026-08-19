#include "protocol/RoomStateSnapshotCodec.h"

#include <cstdint>
#include <iostream>
#include <stdexcept>
#include <string>
#include <vector>

namespace
{
    void Require(
        const bool condition,
        const std::string& message
    )
    {
        if (!condition)
        {
            throw std::runtime_error(message);
        }
    }

    void RequireEncodeRejected(
        const tdr::protocol::RoomStateSnapshot& snapshot,
        const std::string& expectedError,
        const std::string& failureMessage
    )
    {
        bool rejected = false;

        try
        {
            static_cast<void>(
                tdr::protocol::
                RoomStateSnapshotCodec::Encode(
                    snapshot
                )
                );
        }
        catch (const std::invalid_argument& exception)
        {
            rejected =
                exception.what() == expectedError;
        }

        Require(
            rejected,
            failureMessage
        );
    }
}

int main()
{
    try
    {
        tdr::protocol::RoomStateSnapshot original{};
        original.roomId = "ROOM-7";
        original.roomStatus = 0U;
        original.difficultyId = 2U;

        original.players.push_back(
            tdr::protocol::RoomPlayerSnapshot
            {
                0x01020304U,
                true,
                true,
                1U,
                "Host"
            }
        );

        original.players.push_back(
            tdr::protocol::RoomPlayerSnapshot
            {
                0xA0B0C0D0U,
                false,
                false,
                2U,
                "Guest"
            }
        );

        const std::vector<std::uint8_t> expected{
            0x00U, 0x06U,
            0x52U, 0x4FU, 0x4FU,
            0x4DU, 0x2DU, 0x37U,
            0x00U,
            0x02U,
            0x02U,

            0x01U, 0x02U, 0x03U, 0x04U,
            0x03U,
            0x01U,
            0x00U, 0x04U,
            0x48U, 0x6FU, 0x73U, 0x74U,

            0xA0U, 0xB0U, 0xC0U, 0xD0U,
            0x00U,
            0x02U,
            0x00U, 0x05U,
            0x47U, 0x75U, 0x65U,
            0x73U, 0x74U
        };

        const auto encoded =
            tdr::protocol::
            RoomStateSnapshotCodec::Encode(
                original
            );

        Require(
            encoded == expected,
            "Room snapshot did not use the expected wire format."
        );

        const auto decoded =
            tdr::protocol::
            RoomStateSnapshotCodec::Decode(
                encoded.data(),
                encoded.size()
            );

        Require(
            decoded.roomId == original.roomId,
            "Decoded room ID did not match."
        );

        Require(
            decoded.roomStatus == original.roomStatus,
            "Decoded room status did not match."
        );

        Require(
            decoded.difficultyId == original.difficultyId,
            "Decoded difficulty did not match."
        );

        Require(
            decoded.players.size() == 2U,
            "Decoded player count did not match."
        );

        Require(
            decoded.players[0].playerId ==
            original.players[0].playerId,
            "Decoded host player ID did not match."
        );

        Require(
            decoded.players[0].isHost,
            "Decoded host flag did not match."
        );

        Require(
            decoded.players[0].isReady,
            "Decoded host ready flag did not match."
        );

        Require(
            decoded.players[0].characterId == 1U,
            "Decoded host character did not match."
        );

        Require(
            decoded.players[0].nickname == "Host",
            "Decoded host nickname did not match."
        );

        Require(
            decoded.players[1].playerId ==
            original.players[1].playerId,
            "Decoded guest player ID did not match."
        );

        Require(
            !decoded.players[1].isHost,
            "Decoded guest host flag did not match."
        );

        Require(
            !decoded.players[1].isReady,
            "Decoded guest ready flag did not match."
        );

        Require(
            decoded.players[1].characterId == 2U,
            "Decoded guest character did not match."
        );

        Require(
            decoded.players[1].nickname == "Guest",
            "Decoded guest nickname did not match."
        );

        tdr::protocol::RoomStateSnapshot
            snapshotWithoutHost = original;

        snapshotWithoutHost.players[0].isHost =
            false;

        RequireEncodeRejected(
            snapshotWithoutHost,
            "Room snapshot must contain exactly one host.",
            "Room snapshot without a host was not rejected."
        );

        tdr::protocol::RoomStateSnapshot
            snapshotWithTwoHosts = original;

        snapshotWithTwoHosts.players[1].isHost =
            true;

        RequireEncodeRejected(
            snapshotWithTwoHosts,
            "Room snapshot must contain exactly one host.",
            "Room snapshot with two hosts was not rejected."
        );

        std::cout
            << "[PASS] Room state snapshot uses the "
            << "expected network format and round-trips."
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