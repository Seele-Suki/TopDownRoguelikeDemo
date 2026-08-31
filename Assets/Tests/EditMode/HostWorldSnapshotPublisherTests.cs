using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class HostWorldSnapshotPublisherTests
    {
        private const string PublisherTypeName =
            "TopDownRoguelike.Gameplay.Networking." +
            "HostWorldSnapshotPublisher";

        [Test]
        public void HostWorldSnapshotPublisher_IsMonoBehaviourComponent()
        {
            Type publisherType =
                FindType(PublisherTypeName);

            Assert.That(
                publisherType,
                Is.Not.Null,
                "HostWorldSnapshotPublisher must exist.");

            Assert.That(
                typeof(MonoBehaviour).IsAssignableFrom(
                    publisherType),
                Is.True,
                "HostWorldSnapshotPublisher must inherit MonoBehaviour.");
        }

        private static Type FindType(
            string fullTypeName)
        {
            foreach (Assembly assembly in
                AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result =
                    assembly.GetType(
                        fullTypeName,
                        false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}