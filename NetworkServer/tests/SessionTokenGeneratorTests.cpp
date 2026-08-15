#include "room/SessionTokenGenerator.h"

#include <cctype>
#include <iostream>
#include <string>

namespace
{
    bool IsLowercaseHex(
        const std::string& value
    )
    {
        for (const unsigned char character : value)
        {
            const bool isDigit =
                character >= '0'
                && character <= '9';

            const bool isLowerHexLetter =
                character >= 'a'
                && character <= 'f';

            if (!isDigit && !isLowerHexLetter)
            {
                return false;
            }
        }

        return true;
    }
}

int main()
{
    tdr::room::SessionTokenGenerator generator;

    const std::string first =
        generator.Generate();

    const std::string second =
        generator.Generate();

    if (first.size() != 32
        || second.size() != 32)
    {
        std::cerr
            << "[FAIL] Session token length is incorrect."
            << std::endl;

        return 1;
    }

    if (!IsLowercaseHex(first)
        || !IsLowercaseHex(second))
    {
        std::cerr
            << "[FAIL] Session token is not lowercase hex."
            << std::endl;

        return 1;
    }

    if (first == second)
    {
        std::cerr
            << "[FAIL] Session tokens are not unique."
            << std::endl;

        return 1;
    }

    std::cout
        << "[PASS] Session tokens are random 128-bit hex values."
        << std::endl;

    return 0;
}