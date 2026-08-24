using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class RoomLobbyViewTests
    {
        [Test]
        public void ApplyNetworkRoomState_DisplaysPlayersByRealRole()
        {
            Type lobbyType =
                FindType("RoomLobbyView");

            Type playerSlotType =
                FindType(
                    "TopDownRoguelike.Menu.UI." +
                    "PlayerSlotView");

            Type textType =
                FindType("TMPro.TextMeshProUGUI");

            Assert.That(lobbyType, Is.Not.Null);
            Assert.That(playerSlotType, Is.Not.Null);
            Assert.That(textType, Is.Not.Null);

            var lobbyObject =
                new GameObject("RoomLobbyViewTests");

            try
            {
                Component lobby =
                    lobbyObject.AddComponent(lobbyType);

                Component hostSlot =
                    CreatePlayerSlot(
                        lobbyObject.transform,
                        playerSlotType,
                        textType,
                        "HostSlot",
                        out Component hostNicknameText);

                Component clientSlot =
                    CreatePlayerSlot(
                        lobbyObject.transform,
                        playerSlotType,
                        textType,
                        "ClientSlot",
                        out Component clientNicknameText);

                SetPrivateField(
                    lobby,
                    "hostPlayerSlot",
                    hostSlot);

                SetPrivateField(
                    lobby,
                    "clientPlayerSlot",
                    clientSlot);

                MethodInfo applyMethod =
                    lobbyType.GetMethod(
                        "ApplyNetworkRoomState",
                        BindingFlags.Instance |
                        BindingFlags.Public);

                Assert.That(
                    applyMethod,
                    Is.Not.Null,
                    "RoomLobbyView must expose " +
                    "ApplyNetworkRoomState().");

                var players =
                    new List<RoomPlayerSnapshot>
                    {
                        new RoomPlayerSnapshot(
                            77u,
                            false,
                            false,
                            CharacterId.Ranged,
                            "RealClient"),
                        new RoomPlayerSnapshot(
                            42u,
                            true,
                            true,
                            CharacterId.Ranged,
                            "RealHost")
                    };

                var snapshot =
                    new RoomStateSnapshot(
                        "ROOM-9",
                        RoomStateStatus.Waiting,
                        DifficultyId.Normal,
                        players);

                applyMethod.Invoke(
                    lobby,
                    new object[]
                    {
                        snapshot,
                        77u
                    });

                Assert.That(
                    GetText(hostNicknameText),
                    Does.Contain("RealHost"));

                Assert.That(
                    GetText(clientNicknameText),
                    Does.Contain("RealClient"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(
                    lobbyObject);
            }
        }

        [Test]
        public void RoomLobbyView_DoesNotExposePrototypeRoomCreation()
        {
            Type lobbyType =
                FindType("RoomLobbyView");

            Assert.That(
                lobbyType,
                Is.Not.Null);

            Assert.That(
                lobbyType.GetMethod(
                    "CreateLocalHostRoom",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Null,
                "RoomLobbyView must not expose the " +
                "Phase 3 host simulation.");

            Assert.That(
                lobbyType.GetMethod(
                    "CreateLocalClientRoom",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Null,
                "RoomLobbyView must not expose the " +
                "Phase 3 client simulation.");
        }

        private static Component CreatePlayerSlot(
            Transform parent,
            Type playerSlotType,
            Type textType,
            string objectName,
            out Component nicknameText)
        {
            var slotObject =
                new GameObject(objectName);

            slotObject.transform.SetParent(
                parent);

            Component slot =
                slotObject.AddComponent(
                    playerSlotType);

            var nicknameObject =
                new GameObject("NicknameText");

            nicknameObject.transform.SetParent(
                slotObject.transform);

            nicknameText =
                nicknameObject.AddComponent(
                    textType);

            SetPrivateField(
                slot,
                "nicknameText",
                nicknameText);

            return slot;
        }

        private static void SetPrivateField(
            object owner,
            string fieldName,
            object value)
        {
            FieldInfo field =
                owner.GetType().GetField(
                    fieldName,
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);

            Assert.That(
                field,
                Is.Not.Null,
                $"Missing field: {fieldName}");

            field.SetValue(
                owner,
                value);
        }

        private static string GetText(
            Component textComponent)
        {
            PropertyInfo textProperty =
                textComponent.GetType().GetProperty(
                    "text",
                    BindingFlags.Instance |
                    BindingFlags.Public);

            Assert.That(
                textProperty,
                Is.Not.Null);

            return (string)textProperty.GetValue(
                textComponent);
        }

        private static Type FindType(
            string fullName)
        {
            foreach (var assembly
                in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type =
                    assembly.GetType(fullName);

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}