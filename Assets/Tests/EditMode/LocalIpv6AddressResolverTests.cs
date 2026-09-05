using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using NUnit.Framework;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class LocalIpv6AddressResolverTests
    {
        [Test]
        public void SelectPreferredAddress_ReturnsGlobalIpv6()
        {
            Type resolverType = FindType(
                "TopDownRoguelike.Menu.UI.LocalIpv6AddressResolver");
            string selected = (string)resolverType.GetMethod(
                    "SelectPreferredAddress",
                    BindingFlags.Public | BindingFlags.Static)
                .Invoke(
                    null,
                    new object[]
                    {
                        new[]
                        {
                            IPAddress.IPv6Loopback,
                            IPAddress.Parse("fe80::1"),
                            IPAddress.Parse("2001:db8::42")
                        }
                    });

            Assert.That(selected, Is.EqualTo("2001:db8::42"));
        }

        [Test]
        public void SelectPreferredAddress_FallsBackToLoopback()
        {
            Type resolverType = FindType(
                "TopDownRoguelike.Menu.UI.LocalIpv6AddressResolver");
            string selected = (string)resolverType.GetMethod(
                    "SelectPreferredAddress",
                    BindingFlags.Public | BindingFlags.Static)
                .Invoke(
                    null,
                    new object[]
                    {
                        new[]
                        {
                        IPAddress.Parse("fe80::1"),
                        IPAddress.Parse("fdfd::1"),
                        IPAddress.Parse("ff02::1"),
                            IPAddress.Parse("192.168.1.10")
                        }
                    });

            Assert.That(selected, Is.EqualTo("::1"));
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }

            Assert.Fail($"Type not found: {fullName}");
            return null;
        }
    }
}
