using System;
using TopDownRoguelike.Networking.Protocol;
using UnityEngine;

namespace TopDownRoguelike.Gameplay.Networking
{
    public sealed class HostPlayerShotPublisher
        : MonoBehaviour
    {
        private PlayerShooterShotEventSource
            shotEventSource;

        private Action<PlayerShotEvent>
            sendShotEvent;

        public void Configure(
            PlayerShooterShotEventSource
                newShotEventSource,
            Action<PlayerShotEvent>
                newSendShotEvent)
        {
            Unsubscribe();

            shotEventSource =
                newShotEventSource ??
                throw new ArgumentNullException(
                    nameof(newShotEventSource));

            sendShotEvent =
                newSendShotEvent ??
                throw new ArgumentNullException(
                    nameof(newSendShotEvent));

            shotEventSource.ShotGenerated +=
                HandleShotGenerated;

            enabled =
                true;
        }

        private void HandleShotGenerated(
            PlayerShotEvent shotEvent)
        {
            if (!isActiveAndEnabled ||
                sendShotEvent == null)
            {
                return;
            }

            Debug.Log(
                $"HostPlayerShotPublisher: sending shot " +
                $"player={shotEvent.PlayerId}, " +
                $"sequence={shotEvent.ShotSequence}",
                this);

            try
            {
                sendShotEvent(shotEvent);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"HostPlayerShotPublisher: send failed: {exception.Message}",
                    this);
            }
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
            if (shotEventSource != null)
            {
                shotEventSource.ShotGenerated -=
                    HandleShotGenerated;
            }

            shotEventSource =
                null;

            sendShotEvent =
                null;
        }
    }
}
