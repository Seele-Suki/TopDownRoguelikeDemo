using System;
using System.Reflection;
using NUnit.Framework;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class UpgradePanelPhase6GTests
    {
        private const string PanelTypeName =
            "TopDownRoguelike.Gameplay.UI.UpgradePanelView";

        [Test]
        public void UpgradePanelView_ExposesRemoteWaitingState()
        {
            Type panelType = FindType(PanelTypeName);
            Assert.That(panelType, Is.Not.Null);

            Assert.That(
                panelType.GetMethod(
                    "SetWaitingForRemotePlayer",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);

            Assert.That(
                panelType.GetProperty(
                    "IsWaitingForRemotePlayer",
                    BindingFlags.Instance |
                    BindingFlags.Public),
                Is.Not.Null);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly
                in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type result = assembly.GetType(fullName, false);

                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
