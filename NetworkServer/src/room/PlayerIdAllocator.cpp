#include "room/PlayerIdAllocator.h"

#include <limits>
#include <stdexcept>

namespace tdr::room
{
    std::uint32_t PlayerIdAllocator::Allocate()
    {
        if (nextPlayerId_
            == std::numeric_limits<std::uint32_t>::max())
        {
            throw std::overflow_error(
                "Player ID allocator is exhausted."
            );
        }

        return nextPlayerId_++;
    }
}