using System;
using TopDownRoguelike.Networking.Protocol;

namespace TopDownRoguelike.Networking.Transport
{
    public enum NetworkTransportKind
    {
        Tcp,
        Udp
    }

    public enum NetworkTransportEventType
    {
        Connected,
        PacketReceived,
        Disconnected,
        Error
    }

    public sealed class NetworkTransportEvent
    {
        private NetworkTransportEvent(
            NetworkTransportKind transportKind,
            NetworkTransportEventType eventType,
            MessageType packetType,
            uint sequence,
            byte[] payload,
            string errorMessage)
        {
            TransportKind = transportKind;
            EventType = eventType;
            PacketType = packetType;
            Sequence = sequence;
            Payload = payload;
            ErrorMessage = errorMessage;
        }

        public NetworkTransportKind TransportKind { get; }

        public NetworkTransportEventType EventType { get; }

        public MessageType PacketType { get; }

        public uint Sequence { get; }

        public byte[] Payload { get; }

        public string ErrorMessage { get; }

        public static NetworkTransportEvent Connected(
            NetworkTransportKind transportKind)
        {
            return new NetworkTransportEvent(
                transportKind,
                NetworkTransportEventType.Connected,
                MessageType.Invalid,
                0u,
                Array.Empty<byte>(),
                string.Empty);
        }

        public static NetworkTransportEvent PacketReceived(
            NetworkTransportKind transportKind,
            MessageType packetType,
            byte[] payload)
        {
            return new NetworkTransportEvent(
                transportKind,
                NetworkTransportEventType.PacketReceived,
                packetType,
                0u,
                payload,
                string.Empty);
        }

        public static NetworkTransportEvent UdpPacketReceived(
            MessageType packetType,
            uint sequence,
            byte[] payload)
        {
            return new NetworkTransportEvent(
                NetworkTransportKind.Udp,
                NetworkTransportEventType.PacketReceived,
                packetType,
                sequence,
                payload,
                string.Empty);
        }

        public static NetworkTransportEvent Disconnected(
            NetworkTransportKind transportKind)
        {
            return new NetworkTransportEvent(
                transportKind,
                NetworkTransportEventType.Disconnected,
                MessageType.Invalid,
                0u,
                Array.Empty<byte>(),
                string.Empty);
        }

        public static NetworkTransportEvent Error(
            NetworkTransportKind transportKind,
            string errorMessage)
        {
            return new NetworkTransportEvent(
                transportKind,
                NetworkTransportEventType.Error,
                MessageType.Invalid,
                0u,
                Array.Empty<byte>(),
                errorMessage);
        }
    }
}