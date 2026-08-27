namespace TopDownRoguelike.Networking.Protocol
{
    public sealed class UdpSequenceTracker
    {
        private const uint HalfRange =
            0x80000000u;

        private bool hasSequence;
        private uint lastSequence;

        public bool HasSequence =>
            hasSequence;

        public uint LastSequence =>
            lastSequence;

        public bool Accept(
            uint sequence)
        {
            if (!hasSequence)
            {
                lastSequence = sequence;
                hasSequence = true;
                return true;
            }

            if (!IsNewer(
                sequence,
                lastSequence))
            {
                return false;
            }

            lastSequence = sequence;
            return true;
        }

        public static bool IsNewer(
            uint candidate,
            uint previous)
        {
            uint distance =
                unchecked(candidate - previous);

            return distance != 0u &&
                distance < HalfRange;
        }
    }
}