using System;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Client;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RoomConnectionRequestTests
    {
        [Test]
        public void RequestModel_DoesNotExposeRoomIdOrLegacyJoinFactory()
        {
            Type requestType =
                typeof(RoomConnectionRequest);

            Assert.That(
                requestType.GetProperty("RoomId"),
                Is.Null,
                "RoomConnectionRequest must not expose RoomId.");

            Assert.That(
                requestType.GetMethod(
                    "CreateJoin",
                    new[]
                    {
                typeof(string),
                typeof(string),
                typeof(string),
                typeof(string)
                    }),
                Is.Null,
                "The four-argument CreateJoin overload " +
                "must be removed.");
        }

        [TestCase(
            null,
            "::1",
            "7777",
            "nickname")]
        [TestCase(
            "   ",
            "::1",
            "7777",
            "nickname")]
        [TestCase(
            "Seele",
            "not-an-ip",
            "7777",
            "address")]
        [TestCase(
            "Seele",
            "::1",
            "abc",
            "portText")]
        [TestCase(
            "Seele",
            "::1",
            "0",
            "portText")]
        [TestCase(
            "Seele",
            "::1",
            "65536",
            "portText")]
        public void CreateJoin_InvalidInput_ReportsParameter(
            string nickname,
            string address,
            string portText,
            string expectedParameter)
        {
            ArgumentException exception =
                Assert.Catch<ArgumentException>(
                    () =>
                    RoomConnectionRequest.CreateJoin(
                        nickname,
                        address,
                        portText));

            Assert.That(
                exception.ParamName,
                Is.EqualTo(expectedParameter));
        }

        [Test]
        public void CreateHost_NormalizesEndpoint()
        {
            MethodInfo createHostMethod =
                typeof(RoomConnectionRequest).GetMethod(
                    "CreateHost",
                    BindingFlags.Public |
                    BindingFlags.Static);

            Assert.That(
                createHostMethod,
                Is.Not.Null,
                "RoomConnectionRequest must define CreateHost().");

            var request =
                (RoomConnectionRequest)
                    createHostMethod.Invoke(
                        null,
                        new object[]
                        {
                    " Seele ",
                    " ::1 ",
                    7777
                        });

            Assert.That(
                request.Nickname,
                Is.EqualTo("Seele"));

            Assert.That(
                request.Address,
                Is.EqualTo("::1"));

            Assert.That(
                request.Port,
                Is.EqualTo(7777));
        }

        [Test]
        public void CreateJoin_NormalizesValidInput()
        {
            Type requestType =
                typeof(NetworkClient).Assembly.GetType(
                    "TopDownRoguelike.Networking.Client." +
                    "RoomConnectionRequest");

            Assert.That(
                requestType,
                Is.Not.Null,
                "RoomConnectionRequest has not been created.");

            MethodInfo createJoinMethod =
                requestType.GetMethod(
                    "CreateJoin",
                    BindingFlags.Public |
                    BindingFlags.Static,
                    null,
                    new[]
                    {
                        typeof(string),
                        typeof(string),
                        typeof(string)
                    },
                    null);

            Assert.That(createJoinMethod, Is.Not.Null);

            object request =
                createJoinMethod.Invoke(
                    null,
                    new object[]
                    {
                        " Seele ",
                        " ::1 ",
                        " 7777 "
                    });

            Assert.That(
                GetProperty<string>(
                    requestType,
                    request,
                    "Nickname"),
                Is.EqualTo("Seele"));

            Assert.That(
                GetProperty<string>(
                    requestType,
                    request,
                    "Address"),
                Is.EqualTo("::1"));

            Assert.That(
                GetProperty<int>(
                    requestType,
                    request,
                    "Port"),
                Is.EqualTo(7777));
        }

        private static T GetProperty<T>(
            Type requestType,
            object request,
            string propertyName)
        {
            PropertyInfo property =
                requestType.GetProperty(
                    propertyName);

            Assert.That(
                property,
                Is.Not.Null,
                $"Missing property: {propertyName}");

            return (T)property.GetValue(
                request);
        }
    }
}