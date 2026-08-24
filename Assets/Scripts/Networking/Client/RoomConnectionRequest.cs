using System.Globalization;
using System;
using System.Net;

namespace TopDownRoguelike.Networking.Client
{
    public sealed class RoomConnectionRequest
    {
        private RoomConnectionRequest(
            string nickname,
            string address,
            int port)
        {
            Nickname =
                nickname;

            Address =
                address;

            Port =
                port;
        }

        public string Nickname
        {
            get;
        }

        public string Address
        {
            get;
        }

        public int Port
        {
            get;
        }

        public static RoomConnectionRequest CreateHost(
            string nickname,
            string address,
            int port)
        {
            string normalizedNickname =
                NormalizeRequired(
                    nickname,
                    nameof(nickname),
                    "Nickname cannot be empty.");

            string normalizedAddress =
                NormalizeRequired(
                    address,
                    nameof(address),
                    "Server address cannot be empty.");

            if (!IPAddress.TryParse(
                normalizedAddress,
                out _))
            {
                throw new ArgumentException(
                    "IP address format is invalid.",
                    nameof(address));
            }

            if (port < 1 ||
                port > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(port),
                    "Port must be between 1 and 65535.");
            }

            return new RoomConnectionRequest(
                normalizedNickname,
                normalizedAddress,
                port);
        }

        public static RoomConnectionRequest CreateJoin(
            string nickname,
            string address,
            string portText)
        {
            string normalizedNickname =
                NormalizeRequired(
                    nickname,
                    nameof(nickname),
                    "Nickname cannot be empty.");

            string normalizedAddress =
                NormalizeRequired(
                    address,
                    nameof(address),
                    "Server address cannot be empty.");

            if (!IPAddress.TryParse(
                normalizedAddress,
                out _))
            {
                throw new ArgumentException(
                    "IP address format is invalid.",
                    nameof(address));
            }

            string normalizedPortText =
                NormalizeRequired(
                    portText,
                    nameof(portText),
                    "Port cannot be empty.");

            if (!int.TryParse(
                    normalizedPortText,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out int port) ||
                port < 1 ||
                port > 65535)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(portText),
                    "Port must be between 1 and 65535.");
            }

            return new RoomConnectionRequest(
                normalizedNickname,
                normalizedAddress,
                port);
        }

        private static string NormalizeRequired(
            string value,
            string parameterName,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    errorMessage,
                    parameterName);
            }

            return value.Trim();
        }
    }
}