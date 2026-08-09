using TMPro;
using UnityEngine;
using UnityEngine.Events;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Networking.Room;

namespace TopDownRoguelike.Menu.UI
{
    public sealed class PlayerSlotView : MonoBehaviour
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text roleText;
        [SerializeField] private TMP_Text nicknameText;
        [SerializeField] private TMP_Text readyStatusText;

        [Header("Character Cards")]
        [SerializeField]
        private SelectionCardView rangedCharacterCard;

        [SerializeField]
        private SelectionCardView meleeCharacterCard;

        [Header("Status Colors")]
        [SerializeField]
        private Color waitingColor =
            new Color32(170, 170, 170, 255);

        [SerializeField]
        private Color notReadyColor =
            new Color32(255, 190, 90, 255);

        [SerializeField]
        private Color readyColor =
            new Color32(100, 220, 130, 255);

        public void AddRangedCharacterListener(
            UnityAction callback)
        {
            if (rangedCharacterCard != null)
            {
                rangedCharacterCard.AddClickListener(callback);
            }
        }

        public void ShowEmpty(RoomRole expectedRole)
        {
            SetRoleText(expectedRole);

            if (nicknameText != null)
            {
                nicknameText.text = "等待玩家加入";
            }

            if (readyStatusText != null)
            {
                readyStatusText.text = "未连接";
                readyStatusText.color = waitingColor;
            }

            if (rangedCharacterCard != null)
            {
                rangedCharacterCard.SetAvailable(true);
                rangedCharacterCard.SetInteractable(false);
                rangedCharacterCard.SetSelected(false);
            }

            if (meleeCharacterCard != null)
            {
                meleeCharacterCard.SetAvailable(false);
                meleeCharacterCard.SetInteractable(false);
                meleeCharacterCard.SetSelected(false);
            }
        }

        public void Refresh(
            RoomPlayerState player,
            bool canEdit)
        {
            if (player == null || !player.IsOccupied)
            {
                ShowEmpty(RoomRole.Client);
                return;
            }

            SetRoleText(player.Role);

            if (nicknameText != null)
            {
                nicknameText.text =
                    $"玩家：{player.Nickname}";
            }

            if (rangedCharacterCard != null)
            {
                rangedCharacterCard.SetAvailable(true);
                rangedCharacterCard.SetInteractable(canEdit);
                rangedCharacterCard.SetSelected(
                    player.SelectedCharacter ==
                    CharacterId.Ranged);
            }

            if (meleeCharacterCard != null)
            {
                meleeCharacterCard.SetAvailable(false);
                meleeCharacterCard.SetInteractable(false);
                meleeCharacterCard.SetSelected(false);
            }

            if (readyStatusText == null)
            {
                return;
            }

            if (player.IsReady)
            {
                readyStatusText.text = "已准备";
                readyStatusText.color = readyColor;
            }
            else
            {
                readyStatusText.text = "未准备";
                readyStatusText.color = notReadyColor;
            }
        }

        private void SetRoleText(RoomRole role)
        {
            if (roleText == null)
            {
                return;
            }

            switch (role)
            {
                case RoomRole.Host:
                    roleText.text = "房主";
                    break;

                case RoomRole.Client:
                    roleText.text = "加入者";
                    break;

                default:
                    roleText.text = "空槽位";
                    break;
            }
        }
    }
}