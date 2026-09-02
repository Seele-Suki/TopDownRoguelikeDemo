using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TopDownRoguelike.Networking.Gameplay;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class NetworkCombatTargetSelectorTests
    {
        [Test]
        public void SelectNearestTarget_PrefersClosestRegisteredPlayer()
        {
            var registry = new NetworkPlayerRegistry();
            var far = new GameObject("Far Player");
            var near = new GameObject("Near Player");
            far.transform.position = new Vector3(4f, 0f, 0f);
            near.transform.position = new Vector3(1f, 0f, 0f);
            registry.TryRegister(1u, far);
            registry.TryRegister(2u, near);

            object[] arguments = { registry, Vector2.zero, 0u, null };
            bool selected = (bool)SelectorMethod().Invoke(null, arguments);
            uint playerId = (uint)arguments[2];
            Transform target = (Transform)arguments[3];

            Assert.That(selected, Is.True);
            Assert.That(playerId, Is.EqualTo(2u));
            Assert.That(target, Is.EqualTo(near.transform));
            UnityEngine.Object.DestroyImmediate(far);
            UnityEngine.Object.DestroyImmediate(near);
        }

        [Test]
        public void SelectNearestTarget_UsesLowerPlayerIdOnEqualDistance()
        {
            var registry = new NetworkPlayerRegistry();
            var first = new GameObject("First Player");
            var second = new GameObject("Second Player");
            first.transform.position = new Vector3(-2f, 0f, 0f);
            second.transform.position = new Vector3(2f, 0f, 0f);
            registry.TryRegister(7u, first);
            registry.TryRegister(3u, second);

            object[] arguments = { registry, Vector2.zero, 0u, null };
            bool selected = (bool)SelectorMethod().Invoke(null, arguments);
            uint playerId = (uint)arguments[2];

            Assert.That(selected, Is.True);
            Assert.That(playerId, Is.EqualTo(3u));
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }

        private static MethodInfo SelectorMethod()
        {
            Type selectorType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(
                    "TopDownRoguelike.Gameplay.Networking.NetworkCombatTargetSelector"))
                .FirstOrDefault(type => type != null);

            Assert.That(selectorType, Is.Not.Null,
                "NetworkCombatTargetSelector must exist in Assembly-CSharp.");
            return selectorType.GetMethod(
                "TrySelectNearest",
                BindingFlags.Public | BindingFlags.Static);
        }
    }
}
