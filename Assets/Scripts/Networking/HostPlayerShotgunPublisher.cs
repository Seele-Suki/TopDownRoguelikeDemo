using System;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostPlayerShotgunPublisher
        : MonoBehaviour
    {
        private PlayerShotgunEventSource
            shotgunEventSource;

        private Action<PlayerShotgunEvent>
            sendShotgunEvent;

        public void Configure(
            PlayerShotgunEventSource
                newShotgunEventSource,
            Action<PlayerShotgunEvent>
                newSendShotgunEvent)
        {
            Unsubscribe();

            shotgunEventSource =
                newShotgunEventSource ??
                throw new ArgumentNullException(
                    nameof(newShotgunEventSource));

            sendShotgunEvent =
                newSendShotgunEvent ??
                throw new ArgumentNullException(
                    nameof(newSendShotgunEvent));

            shotgunEventSource.ShotgunGenerated +=
                HandleShotgunGenerated;

            enabled =
                true;
        }

        private void HandleShotgunGenerated(
            PlayerShotgunEvent shotgunEvent)
        {
            if (!isActiveAndEnabled ||
                sendShotgunEvent == null)
            {
                return;
            }

            if (shotgunEvent == null)
            {
                return;
            }

            Debug.Log(
                $"HostPlayerShotgunPublisher: " +
                $"sending shotgun event " +
                $"player={shotgunEvent.PlayerId}, " +
                $"sequence={shotgunEvent.VolleySequence}",
                this);

            sendShotgunEvent(
                shotgunEvent);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (shotgunEventSource != null)
            {
                shotgunEventSource.ShotgunGenerated -=
                    HandleShotgunGenerated;
            }

            shotgunEventSource =
                null;

            sendShotgunEvent =
                null;
        }
    }
}