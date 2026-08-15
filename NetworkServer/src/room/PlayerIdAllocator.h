#pragma once

#include <cstdint>

namespace tdr::room
{
    class PlayerIdAllocator final
    {
    public:
        [[nodiscard]]
        std::uint32_t Allocate();

    private:
        std::uint32_t nextPlayerId_ = 1;
    };
}