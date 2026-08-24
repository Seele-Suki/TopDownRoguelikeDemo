using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Menu.UI;
using TopDownRoguelike.Networking.Room;
using TopDownRoguelike.Networking.Protocol;

public class RoomLobbyView : MonoBehaviour
{
    [Header("Room Information")]
    [SerializeField] private TMP_Text lobbyTitleText;
    [SerializeField] private TMP_Text addressText;
    [SerializeField] private TMP_Text roomMessageText;

    [Header("Player Slots")]
    [SerializeField] private PlayerSlotView hostPlayerSlot;
    [SerializeField] private PlayerSlotView clientPlayerSlot;

    [Header("Difficulty")]
    [SerializeField] private SelectionCardView normalDifficultyCard;
    [SerializeField] private SelectionCardView hardDifficultyCard;
    [SerializeField] private SelectionCardView hellDifficultyCard;

    [Header("Actions")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TMP_Text readyButtonText;

    private RoomState roomState;

    private int displayedHostPlayerId;
    private int displayedClientPlayerId;

    private int localPlayerId;
    private RoomRole localRole = RoomRole.None;
    private string connectedAddress = string.Empty;
    private int connectedPort;

    private void Awake()
    {
        if (hostPlayerSlot != null)
        {
            hostPlayerSlot.AddRangedCharacterListener(
                SelectLocalRangedCharacter);
        }

        if (clientPlayerSlot != null)
        {
            clientPlayerSlot.AddRangedCharacterListener(
                SelectLocalRangedCharacter);
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        if (hardDifficultyCard != null)
        {
            hardDifficultyCard.SetInteractable(false);
        }

        if (hellDifficultyCard != null)
        {
            hellDifficultyCard.SetInteractable(false);
        }
    }

    public void ApplyNetworkRoomState(
        RoomStateSnapshot snapshot,
        uint networkLocalPlayerId)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(
                nameof(snapshot));
        }

        roomState =
            new RoomState();

        displayedHostPlayerId =
            0;

        displayedClientPlayerId =
            0;

        foreach (RoomPlayerSnapshot player
            in snapshot.Players)
        {
            int playerId =
                checked((int)player.PlayerId);

            RoomRole role =
                player.IsHost
                    ? RoomRole.Host
                    : RoomRole.Client;

            roomState.TryAddPlayer(
                playerId,
                player.Nickname,
                role);

            if (player.Character !=
                CharacterId.None)
            {
                roomState.TrySelectCharacter(
                    playerId,
                    player.Character);
            }

            if (player.IsReady)
            {
                roomState.TrySetReady(
                    playerId,
                    true);
            }

            if (player.IsHost)
            {
                displayedHostPlayerId =
                    playerId;
            }
            else if (displayedClientPlayerId == 0)
            {
                displayedClientPlayerId =
                    playerId;
            }
        }

        if (displayedHostPlayerId != 0 &&
            snapshot.SelectedDifficulty !=
                DifficultyId.None)
        {
            roomState.TrySelectDifficulty(
                displayedHostPlayerId,
                snapshot.SelectedDifficulty);
        }

        localPlayerId =
            checked((int)networkLocalPlayerId);

        RoomPlayerState localPlayer =
            roomState.GetPlayer(
                localPlayerId);

        localRole =
            localPlayer == null
                ? RoomRole.None
                : localPlayer.Role;

        if (readyButton != null)
        {
            readyButton.interactable =
                localPlayer != null;
        }

        RefreshView();
    }

    private void SelectLocalRangedCharacter()
    {
        if (roomState == null ||
            localRole == RoomRole.None)
        {
            ShowMessage("请先创建或加入房间");
            return;
        }

        bool selected = roomState.TrySelectCharacter(
            localPlayerId,
            CharacterId.Ranged);

        if (!selected)
        {
            ShowMessage("角色选择失败");
            return;
        }

        RefreshView();
        ShowMessage("已选择远程角色");
    }

    public void ToggleLocalPlayerReady()
    {
        if (roomState == null ||
            localRole == RoomRole.None)
        {
            ShowMessage("请先创建或加入房间");
            return;
        }

        RoomPlayerState localPlayer =
            roomState.GetPlayer(localPlayerId);

        if (localPlayer == null)
        {
            ShowMessage("没有找到本机玩家数据");
            return;
        }

        bool nextReadyState = !localPlayer.IsReady;

        if (nextReadyState &&
            localPlayer.SelectedCharacter ==
            CharacterId.None)
        {
            ShowMessage("请先选择角色");
            return;
        }

        bool changed = roomState.TrySetReady(
            localPlayerId,
            nextReadyState);

        if (!changed)
        {
            ShowMessage("切换准备状态失败");
            return;
        }

        RefreshView();

        if (nextReadyState)
        {
            ShowMessage("本机玩家已准备");
        }
        else
        {
            ShowMessage("本机玩家已取消准备");
        }
    }

    private void RefreshView()
    {
        if (roomState == null)
        {
            return;
        }

        if (lobbyTitleText != null)
        {
            lobbyTitleText.text =
                localRole == RoomRole.Host
                    ? "创建房间"
                    : "加入房间";
        }

        if (addressText != null)
        {
            if (localRole == RoomRole.Host)
            {
                addressText.text = "等待生成服务器地址";
            }
            else
            {
                bool isIpv6 =
                    connectedAddress.Contains(":");

                addressText.text = isIpv6
                    ? $"[{connectedAddress}]:{connectedPort}"
                    : $"{connectedAddress}:{connectedPort}";
            }
        }

        RoomPlayerState hostPlayer =
            roomState.GetPlayer(
                displayedHostPlayerId);

        RoomPlayerState clientPlayer =
            roomState.GetPlayer(
                displayedClientPlayerId);

        RoomPlayerState localPlayer =
            roomState.GetPlayer(localPlayerId);

        if (readyButtonText != null)
        {
            readyButtonText.text =
                localPlayer != null &&
                localPlayer.IsReady
                    ? "取消准备"
                    : "准备";
        }

        bool canEditHost =
            localPlayerId ==
                displayedHostPlayerId &&
            hostPlayer != null &&
            !hostPlayer.IsReady;

        bool canEditClient =
            localPlayerId ==
                displayedClientPlayerId &&
            clientPlayer != null &&
            !clientPlayer.IsReady;

        if (hostPlayerSlot != null)
        {
            hostPlayerSlot.Refresh(
                hostPlayer,
                canEditHost);
        }

        if (clientPlayerSlot != null)
        {
            clientPlayerSlot.Refresh(
                clientPlayer,
                canEditClient);
        }

        bool localIsHost =
            localRole == RoomRole.Host;

        if (normalDifficultyCard != null)
        {
            normalDifficultyCard.SetAvailable(true);
            normalDifficultyCard.SetInteractable(localIsHost);
            normalDifficultyCard.SetSelected(
                roomState.SelectedDifficulty ==
                DifficultyId.Normal);
        }

        if (hardDifficultyCard != null)
        {
            hardDifficultyCard.SetAvailable(false);
            hardDifficultyCard.SetInteractable(false);
        }

        if (hellDifficultyCard != null)
        {
            hellDifficultyCard.SetAvailable(false);
            hellDifficultyCard.SetInteractable(false);
        }

        if (startGameButton != null)
        {
            startGameButton.interactable =
                localIsHost &&
                roomState.CanStartGame;
        }
    }

    public void HandleStartGamePrototype()
    {
        if (roomState == null)
        {
            ShowMessage("房间数据不存在");
            return;
        }

        if (localRole != RoomRole.Host)
        {
            ShowMessage("只有房主可以开始游戏");
            return;
        }

        if (!roomState.CanStartGame)
        {
            ShowMessage("双方选择角色并准备后才能开始");
            return;
        }

        ShowMessage("联机游戏启动将在后续阶段接入");
    }

    public void ResetLocalRoom()
    {
        if (roomState != null)
        {
            roomState.Reset();
            roomState = null;
        }

        displayedHostPlayerId =
            0;

        displayedClientPlayerId =
            0;

        localPlayerId = 0;
        localRole = RoomRole.None;
        connectedAddress = string.Empty;
        connectedPort = 0;

        if (hostPlayerSlot != null)
        {
            hostPlayerSlot.ShowEmpty(RoomRole.Host);
        }

        if (clientPlayerSlot != null)
        {
            clientPlayerSlot.ShowEmpty(RoomRole.Client);
        }

        if (normalDifficultyCard != null)
        {
            normalDifficultyCard.SetAvailable(true);
            normalDifficultyCard.SetInteractable(false);
            normalDifficultyCard.SetSelected(false);
        }

        if (hardDifficultyCard != null)
        {
            hardDifficultyCard.SetAvailable(false);
            hardDifficultyCard.SetInteractable(false);
        }

        if (hellDifficultyCard != null)
        {
            hellDifficultyCard.SetAvailable(false);
            hellDifficultyCard.SetInteractable(false);
        }

        if (readyButton != null)
        {
            readyButton.interactable = false;
        }

        if (readyButtonText != null)
        {
            readyButtonText.text = "准备";
        }

        if (startGameButton != null)
        {
            startGameButton.interactable = false;
        }

        if (lobbyTitleText != null)
        {
            lobbyTitleText.text = "联机房间";
        }

        if (addressText != null)
        {
            addressText.text = string.Empty;
        }

        ShowMessage(string.Empty);
    }

    private void ShowMessage(string message)
    {
        if (roomMessageText != null)
        {
            roomMessageText.text = message;
        }
    }
}