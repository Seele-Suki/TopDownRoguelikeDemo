#include "room/PlayerIdAllocator.h"

#include <iostream>

int main()
{
    tdr::room::PlayerIdAllocator allocator;

    const auto first =
        allocator.Allocate();

    const auto second =
        allocator.Allocate();

    if (first != 1)
    {
        std::cerr
            << "[FAIL] First player ID is not 1."
            << std::endl;

        return 1;
    }

    if (second != 2)
    {
        std::cerr
            << "[FAIL] Second player ID is not 2."
            << std::endl;

        return 1;
    }

    if (first == second)
    {
        std::cerr
            << "[FAIL] Player IDs are not unique."
            << std::endl;

        return 1;
    }

    std::cout
        << "[PASS] Player IDs are allocated uniquely."
        << std::endl;

    return 0;
}