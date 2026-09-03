#include "protocol/PlayerStateSnapshotCodec.h"

#include <cstdint>
#include <iostream>
#include <stdexcept>

namespace
{
    void Require(
        const bool condition,
        const char* message)
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

        PlayerStateSnapshotPayload snapshot{};
        snapshot.players.push_back(
            PlayerStateRecord{
                1U, 0.0F, 0.0F, 1.0F, 0.0F,
                0U, 1U, 1U
            }
        );
        snapshot.players.push_back(
            PlayerStateRecord{
                2U, 0.0F, 0.0F, 1.0F, 0.0F,
                0U, 0xFFFFU, 0xFFFFU
            }
        );

        const auto encoded =
            PlayerStateSnapshotCodec::Encode(snapshot);
        const auto decoded =
            PlayerStateSnapshotCodec::Decode(
                encoded.data(),
                encoded.size()
            );

        Require(
            decoded.players.size() == 2U,
            "Health boundary snapshot lost a player record."
        );
        Require(
            decoded.players[0].currentHealth == 1U &&
            decoded.players[0].maxHealth == 1U,
            "Minimum valid health boundary was not preserved."
        );
        Require(
            decoded.players[1].currentHealth == 0xFFFFU &&
            decoded.players[1].maxHealth == 0xFFFFU,
            "Maximum uint16 health boundary was not preserved."
        );

        std::cout << "PlayerHealthBoundary tests passed.\n";
        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr << "[FAIL] " << exception.what() << '\n';
        return 1;
    }
}
