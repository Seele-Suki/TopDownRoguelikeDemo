#include "protocol/PlayerInputCodec.h"
#include "protocol/PlayerStateSnapshotCodec.h"

#include <cstdint>
#include <exception>
#include <iostream>
#include <limits>
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

    template<typename Action>
    void RequireInvalidArgument(
        Action action,
        const std::string& message
    )
    {
        bool rejected = false;

        try
        {
            action();
        }
        catch (const std::invalid_argument&)
        {
            rejected = true;
        }

        Require(rejected, message);
    }

    void PlayerInputUsesStableWireLayout()
    {
        tdr::protocol::PlayerInputPayload original{};
        original.moveX = 0.5F;
        original.moveY = -0.25F;
        original.aimX = 1.0F;
        original.aimY = -1.0F;

        const std::vector<std::uint8_t> expected{
            // Move X: 0.5F = 0x3F000000
            0x3FU, 0x00U, 0x00U, 0x00U,

            // Move Y: -0.25F = 0xBE800000
            0xBEU, 0x80U, 0x00U, 0x00U,

            // Aim X: 1.0F = 0x3F800000
            0x3FU, 0x80U, 0x00U, 0x00U,

            // Aim Y: -1.0F = 0xBF800000
            0xBFU, 0x80U, 0x00U, 0x00U,

            // Reserved: must be zero
            0x00U, 0x00U, 0x00U, 0x00U
        };

        const auto encoded =
            tdr::protocol::PlayerInputCodec::Encode(
                original
            );

        Require(
            encoded == expected,
            "Player input did not use the expected wire layout."
        );

        const auto decoded =
            tdr::protocol::PlayerInputCodec::Decode(
                encoded.data(),
                encoded.size()
            );

        Require(
            decoded.moveX == original.moveX,
            "Decoded move X did not match."
        );

        Require(
            decoded.moveY == original.moveY,
            "Decoded move Y did not match."
        );

        Require(
            decoded.aimX == original.aimX,
            "Decoded aim X did not match."
        );

        Require(
            decoded.aimY == original.aimY,
            "Decoded aim Y did not match."
        );
    }

    void PlayerInputRejectsInvalidValues()
    {
        using namespace tdr::protocol;

        PlayerInputPayload invalid{};
        invalid.moveX = 1.1F;
        invalid.moveY = 0.0F;
        invalid.aimX = 1.0F;
        invalid.aimY = 0.0F;

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerInputCodec::Encode(invalid)
                    );
            },
            "Move component outside [-1, 1] was accepted."
        );

        invalid.moveX = 0.8F;
        invalid.moveY = 0.8F;

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerInputCodec::Encode(invalid)
                    );
            },
            "Movement magnitude above one was accepted."
        );

        invalid.moveX = 0.0F;
        invalid.moveY = 0.0F;
        invalid.aimX =
            std::numeric_limits<float>::infinity();

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerInputCodec::Encode(invalid)
                    );
            },
            "Infinite aim value was accepted."
        );

        invalid.aimX =
            std::numeric_limits<float>::quiet_NaN();

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerInputCodec::Encode(invalid)
                    );
            },
            "NaN aim value was accepted."
        );
    }

    void PlayerInputRejectsMalformedPayloads()
    {
        using namespace tdr::protocol;

        PlayerInputPayload valid{};
        valid.moveX = 0.0F;
        valid.moveY = 0.0F;
        valid.aimX = 1.0F;
        valid.aimY = 0.0F;

        const auto encoded =
            PlayerInputCodec::Encode(valid);

        auto truncated = encoded;
        truncated.pop_back();

        RequireInvalidArgument(
            [&truncated]()
            {
                static_cast<void>(
                    PlayerInputCodec::Decode(
                        truncated.data(),
                        truncated.size()
                    )
                    );
            },
            "Truncated player input was accepted."
        );

        auto trailing = encoded;
        trailing.push_back(0U);

        RequireInvalidArgument(
            [&trailing]()
            {
                static_cast<void>(
                    PlayerInputCodec::Decode(
                        trailing.data(),
                        trailing.size()
                    )
                    );
            },
            "Player input with trailing bytes was accepted."
        );

        auto nonZeroReserved = encoded;
        nonZeroReserved[19] = 1U;

        RequireInvalidArgument(
            [&nonZeroReserved]()
            {
                static_cast<void>(
                    PlayerInputCodec::Decode(
                        nonZeroReserved.data(),
                        nonZeroReserved.size()
                    )
                    );
            },
            "Non-zero reserved field was accepted."
        );

        RequireInvalidArgument(
            []()
            {
                static_cast<void>(
                    PlayerInputCodec::Decode(
                        nullptr,
                        20U
                    )
                    );
            },
            "Null player input payload was accepted."
        );
    }

    void PlayerStateSnapshotUsesStableWireLayout()
    {
        using namespace tdr::protocol;

        PlayerStateSnapshotPayload original{};

        // 故意先放较大的 ID，验证编码器会按 ID 升序编码。
        original.players.push_back(
            PlayerStateRecord
            {
                0x01020304U,
                -3.5F,
                4.25F,
                -1.0F,
                0.0F
            }
        );

        original.players.push_back(
            PlayerStateRecord
            {
                1U,
                1.5F,
                -2.25F,
                0.0F,
                1.0F
            }
        );

        const std::vector<std::uint8_t> expected{
            // Player count: 2
            0x00U, 0x00U, 0x00U, 0x02U,

            // Player ID: 1
            0x00U, 0x00U, 0x00U, 0x01U,
            // Position X: 1.5
            0x3FU, 0xC0U, 0x00U, 0x00U,
            // Position Y: -2.25
            0xC0U, 0x10U, 0x00U, 0x00U,
            // Aim X: 0
            0x00U, 0x00U, 0x00U, 0x00U,
            // Aim Y: 1
            0x3FU, 0x80U, 0x00U, 0x00U,
            // Reserved
            0x00U, 0x00U, 0x00U, 0x00U,

            // Player ID: 0x01020304
            0x01U, 0x02U, 0x03U, 0x04U,
            // Position X: -3.5
            0xC0U, 0x60U, 0x00U, 0x00U,
            // Position Y: 4.25
            0x40U, 0x88U, 0x00U, 0x00U,
            // Aim X: -1
            0xBFU, 0x80U, 0x00U, 0x00U,
            // Aim Y: 0
            0x00U, 0x00U, 0x00U, 0x00U,
            // Reserved
            0x00U, 0x00U, 0x00U, 0x00U
        };

        const auto encoded =
            PlayerStateSnapshotCodec::Encode(original);

        Require(
            encoded == expected,
            "Player state snapshot used the wrong wire layout."
        );

        const auto decoded =
            PlayerStateSnapshotCodec::Decode(
                encoded.data(),
                encoded.size()
            );

        Require(
            decoded.players.size() == 2U,
            "Decoded snapshot player count did not match."
        );

        Require(
            decoded.players[0].playerId == 1U,
            "Decoded snapshot was not ordered by player ID."
        );

        Require(
            decoded.players[0].positionX == 1.5F &&
            decoded.players[0].positionY == -2.25F &&
            decoded.players[0].aimX == 0.0F &&
            decoded.players[0].aimY == 1.0F,
            "Decoded first player state did not match."
        );

        Require(
            decoded.players[1].playerId == 0x01020304U,
            "Decoded second player ID did not match."
        );

        Require(
            decoded.players[1].positionX == -3.5F &&
            decoded.players[1].positionY == 4.25F &&
            decoded.players[1].aimX == -1.0F &&
            decoded.players[1].aimY == 0.0F,
            "Decoded second player state did not match."
        );
    }

    void PlayerStateSnapshotRejectsMalformedPayloads()
    {
        using namespace tdr::protocol;

        PlayerStateSnapshotPayload snapshot{};
        snapshot.players.push_back(
            PlayerStateRecord{
                1U, 0.0F, 0.0F, 1.0F, 0.0F
            }
        );

        const auto encoded =
            PlayerStateSnapshotCodec::Encode(snapshot);

        auto truncated = encoded;
        truncated.pop_back();

        RequireInvalidArgument(
            [&truncated]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        truncated.data(),
                        truncated.size()
                    )
                    );
            },
            "Truncated state snapshot was accepted."
        );

        auto trailing = encoded;
        trailing.push_back(0U);

        RequireInvalidArgument(
            [&trailing]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        trailing.data(),
                        trailing.size()
                    )
                    );
            },
            "State snapshot with trailing bytes was accepted."
        );

        auto nonZeroReserved = encoded;
        nonZeroReserved[27] = 1U;

        RequireInvalidArgument(
            [&nonZeroReserved]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        nonZeroReserved.data(),
                        nonZeroReserved.size()
                    )
                    );
            },
            "Non-zero snapshot reserved field was accepted."
        );

        RequireInvalidArgument(
            []()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        nullptr,
                        28U
                    )
                    );
            },
            "Null state snapshot payload was accepted."
        );
    }

    void PlayerStateSnapshotRejectsInvalidRecords()
    {
        using namespace tdr::protocol;

        PlayerStateSnapshotPayload invalid{};
        invalid.players.push_back(
            PlayerStateRecord{
                0U, 0.0F, 0.0F, 1.0F, 0.0F
            }
        );

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Encode(
                        invalid
                    )
                    );
            },
            "Zero player ID was accepted during encoding."
        );

        invalid.players[0].playerId = 1U;
        invalid.players.push_back(
            PlayerStateRecord{
                1U, 1.0F, 2.0F, 0.0F, 1.0F
            }
        );

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Encode(
                        invalid
                    )
                    );
            },
            "Duplicate player IDs were accepted during encoding."
        );

        invalid.players.resize(1U);
        invalid.players[0].positionX =
            std::numeric_limits<float>::infinity();

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Encode(
                        invalid
                    )
                    );
            },
            "Infinite position was accepted during encoding."
        );

        invalid.players[0].positionX = 0.0F;
        invalid.players[0].aimY =
            std::numeric_limits<float>::quiet_NaN();

        RequireInvalidArgument(
            [&invalid]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Encode(
                        invalid
                    )
                    );
            },
            "NaN aim was accepted during encoding."
        );
    }

    void PlayerStateSnapshotRejectsInvalidWireRecords()
    {
        using namespace tdr::protocol;

        PlayerStateSnapshotPayload valid{};

        valid.players.push_back(
            PlayerStateRecord{
                1U, 0.0F, 0.0F, 1.0F, 0.0F
            }
        );

        valid.players.push_back(
            PlayerStateRecord{
                2U, 1.0F, 2.0F, 0.0F, 1.0F
            }
        );

        const auto encoded =
            PlayerStateSnapshotCodec::Encode(valid);

        auto zeroId = encoded;
        zeroId[4] = 0U;
        zeroId[5] = 0U;
        zeroId[6] = 0U;
        zeroId[7] = 0U;

        RequireInvalidArgument(
            [&zeroId]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        zeroId.data(),
                        zeroId.size()
                    )
                    );
            },
            "Zero player ID was accepted during decoding."
        );

        auto duplicateId = encoded;
        duplicateId[28] = 0U;
        duplicateId[29] = 0U;
        duplicateId[30] = 0U;
        duplicateId[31] = 1U;

        RequireInvalidArgument(
            [&duplicateId]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        duplicateId.data(),
                        duplicateId.size()
                    )
                    );
            },
            "Duplicate wire player IDs were accepted."
        );

        auto descendingIds = encoded;
        descendingIds[7] = 2U;
        descendingIds[31] = 1U;

        RequireInvalidArgument(
            [&descendingIds]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        descendingIds.data(),
                        descendingIds.size()
                    )
                    );
            },
            "Descending wire player IDs were accepted."
        );

        auto infinitePosition = encoded;
        infinitePosition[8] = 0x7FU;
        infinitePosition[9] = 0x80U;
        infinitePosition[10] = 0x00U;
        infinitePosition[11] = 0x00U;

        RequireInvalidArgument(
            [&infinitePosition]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        infinitePosition.data(),
                        infinitePosition.size()
                    )
                    );
            },
            "Infinite wire position was accepted."
        );

        auto nanAim = encoded;
        nanAim[16] = 0x7FU;
        nanAim[17] = 0xC0U;
        nanAim[18] = 0x00U;
        nanAim[19] = 0x00U;

        RequireInvalidArgument(
            [&nanAim]()
            {
                static_cast<void>(
                    PlayerStateSnapshotCodec::Decode(
                        nanAim.data(),
                        nanAim.size()
                    )
                    );
            },
            "NaN wire aim was accepted."
        );
    }
}

int main()
{
    try
    {
        PlayerInputUsesStableWireLayout();

        PlayerInputRejectsInvalidValues();
        PlayerInputRejectsMalformedPayloads();

        PlayerStateSnapshotUsesStableWireLayout();
        PlayerStateSnapshotRejectsMalformedPayloads();

        PlayerStateSnapshotRejectsInvalidRecords();
        PlayerStateSnapshotRejectsInvalidWireRecords();

        std::cout
            << "[PASS] Player input codec uses the "
            << "expected wire layout."
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