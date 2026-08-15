#pragma once

#include <string>

namespace tdr::room
{
    class SessionTokenGenerator final
    {
    public:
        [[nodiscard]]
        std::string Generate() const;
    };
}