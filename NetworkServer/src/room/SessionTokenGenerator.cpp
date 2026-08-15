#include "room/SessionTokenGenerator.h"

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif

#ifndef NOMINMAX
#define NOMINMAX
#endif

#include <Windows.h>
#include <bcrypt.h>

#include <array>
#include <cstdint>
#include <stdexcept>

#pragma comment(lib, "bcrypt.lib")

namespace tdr::room
{
    std::string SessionTokenGenerator::Generate() const
    {
        std::array<std::uint8_t, 16> randomBytes{};

        const NTSTATUS result =
            ::BCryptGenRandom(
                nullptr,
                randomBytes.data(),
                static_cast<ULONG>(randomBytes.size()),
                BCRYPT_USE_SYSTEM_PREFERRED_RNG
            );

        if (result != 0)
        {
            throw std::runtime_error(
                "BCryptGenRandom failed."
            );
        }

        constexpr char hexDigits[] =
            "0123456789abcdef";

        std::string token;
        token.resize(randomBytes.size() * 2U);

        for (std::size_t index = 0;
            index < randomBytes.size();
            ++index)
        {
            const std::uint8_t value =
                randomBytes[index];

            token[index * 2U] =
                hexDigits[(value >> 4U) & 0x0fU];

            token[index * 2U + 1U] =
                hexDigits[value & 0x0fU];
        }

        return token;
    }
}