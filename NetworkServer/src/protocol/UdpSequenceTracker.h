#pragma once

#include <cstdint>

namespace tdr::protocol
{
    class UdpSequenceTracker final
    {
    public:
        [[nodiscard]]
        bool Accept(
            const std::uint32_t sequence
        ) noexcept
        {
            if (!hasSequence_)
            {
                lastSequence_ = sequence;
                hasSequence_ = true;
                return true;
            }

            if (!IsNewer(
                sequence,
                lastSequence_
            ))
            {
                return false;
            }

            lastSequence_ = sequence;
            return true;
        }

        [[nodiscard]]
        bool HasSequence() const noexcept
        {
            return hasSequence_;
        }

        [[nodiscard]]
        std::uint32_t LastSequence() const noexcept
        {
            return lastSequence_;
        }

        [[nodiscard]]
        static constexpr bool IsNewer(
            const std::uint32_t candidate,
            const std::uint32_t previous
        ) noexcept
        {
            constexpr std::uint32_t kHalfRange =
                0x80000000U;

            const std::uint32_t distance =
                candidate - previous;

            return distance != 0U
                && distance < kHalfRange;
        }

    private:
        bool hasSequence_ = false;
        std::uint32_t lastSequence_ = 0U;
    };
}
