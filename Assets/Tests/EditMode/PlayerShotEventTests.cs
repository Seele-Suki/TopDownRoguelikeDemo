using System;
using NUnit.Framework;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class PlayerShotEventTests
    {
        [Test]
        public void ConstructorStoresShotInformation()
        {
            var shot =
                new PlayerShotEvent(
                    22u,
                    7u,
                    1.5f,
                    -2.25f,
                    0f,
                    1f);

            Assert.That(
                shot.PlayerId,
                Is.EqualTo(22u));

            Assert.That(
                shot.ShotSequence,
                Is.EqualTo(7u));

            Assert.That(
                shot.OriginX,
                Is.EqualTo(1.5f));

            Assert.That(
                shot.OriginY,
                Is.EqualTo(-2.25f));

            Assert.That(
                shot.DirectionX,
                Is.EqualTo(0f));

            Assert.That(
                shot.DirectionY,
                Is.EqualTo(1f));
        }

        [Test]
        public void ZeroPlayerIdIsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new PlayerShotEvent(
                        0u,
                        1u,
                        0f,
                        0f,
                        1f,
                        0f));
        }

        [Test]
        public void ZeroDirectionIsRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new PlayerShotEvent(
                        1u,
                        1u,
                        0f,
                        0f,
                        0f,
                        0f));
        }

        [Test]
        public void NonFiniteValuesAreRejected()
        {
            Assert.Throws<ArgumentException>(
                () =>
                    new PlayerShotEvent(
                        1u,
                        1u,
                        float.NaN,
                        0f,
                        1f,
                        0f));

            Assert.Throws<ArgumentException>(
                () =>
                    new PlayerShotEvent(
                        1u,
                        1u,
                        0f,
                        0f,
                        float.PositiveInfinity,
                        0f));
        }
    }
}