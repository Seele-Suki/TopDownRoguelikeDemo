using NUnit.Framework;
using TopDownRoguelike.Networking.Client;
using TopDownRoguelike.Networking.Room;
using UnityEngine;

namespace TopDownRoguelike.Tests.EditMode
{
    public sealed class DisconnectDialogViewTests
    {
        [Test]
        public void Show_WithNoAction_DoesNotOpen()
        {
            var gameObject = new GameObject("DisconnectDialogViewTests");
            try
            {
                var view = gameObject.AddComponent<DisconnectDialogView>();
                Assert.That(view.Show(new DisconnectContext(RoomRole.None, false, DisconnectReason.ServerClosed)), Is.False);
                Assert.That(view.IsVisible, Is.False);
            }
            finally { Object.DestroyImmediate(gameObject); }
        }

        [Test]
        public void Show_IsIdempotentWhileVisible()
        {
            var gameObject = new GameObject("DisconnectDialogViewTests");
            try
            {
                var view = gameObject.AddComponent<DisconnectDialogView>();
                var context = new DisconnectContext(RoomRole.Host, true, DisconnectReason.RemotePeerLeft);
                Assert.That(view.Show(context), Is.True);
                Assert.That(view.Show(context), Is.False);
            }
            finally { Object.DestroyImmediate(gameObject); }
        }
    }
}
