using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TopDownRoguelike.Infrastructure;
using TopDownRoguelike.Menu.UI;
using TopDownRoguelike.Networking.Room;

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

    private const int HostPlayerId = 1;
    private const int ClientPlayerId = 2;

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

    public void CreateLocalHostRoom(string hostNickname)
    {
        roomState = new RoomState();

        localPlayerId = HostPlayerId;
        localRole = RoomRole.Host;
        connectedAddress = string.Empty;
        connectedPort = 0;

        bool hostAdded = roomState.TryAddPlayer(
            HostPlayerId,
            hostNickname,
            RoomRole.Host);

        if (!hostAdded)
        {
            ShowMessage("创建房间失败");
            return;
        }

        roomState.TrySelectDifficulty(
            HostPlayerId,
            DifficultyId.Normal);

        bool clientAdded = roomState.TryAddPlayer(
            ClientPlayerId,
            "模拟加入者",
            RoomRole.Client);

        if (!clientAdded)
        {
            ShowMessage("模拟加入者进入失败");
            return;
        }

        roomState.TrySelectCharacter(
            ClientPlayerId,
            CharacterId.Ranged);

        roomState.TrySetReady(
            ClientPlayerId,
            true);

        RefreshView();

        if (readyButton != null)
        {
            readyButton.interactable = true;
        }

        ShowMessage("模拟加入者已准备，请房主确认准备");
    }

    public void CreateLocalClientRoom(
    string clientNickname,
    string address,
    int port)
    {
        roomState = new RoomState();

        localPlayerId = ClientPlayerId;
        localRole = RoomRole.Client;
        connectedAddress = address;
        connectedPort = port;

        bool hostAdded = roomState.TryAddPlayer(
            HostPlayerId,
            "模拟房主",
            RoomRole.Host);

        if (!hostAdded)
        {
            ShowMessage("模拟房主创建失败");
            return;
        }

        roomState.TrySelectCharacter(
            HostPlayerId,
            CharacterId.Ranged);

        roomState.TrySelectDifficulty(
            HostPlayerId,
            DifficultyId.Normal);

        roomState.TrySetReady(
            HostPlayerId,
            true);

        bool clientAdded = roomState.TryAddPlayer(
            ClientPlayerId,
            clientNickname,
            RoomRole.Client);

        if (!clientAdded)
        {
            ShowMessage("加入房间失败");
            return;
        }

        RefreshView();

        if (readyButton != null)
        {
            readyButton.interactable = true;
        }

        ShowMessage("已进入模拟房间，请确认准备");
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
            roomState.GetPlayer(HostPlayerId);

        RoomPlayerState clientPlayer =
            roomState.GetPlayer(ClientPlayerId);

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
            localPlayerId == HostPlayerId &&
            hostPlayer != null &&
            !hostPlayer.IsReady;

        bool canEditClient =
            localPlayerId == ClientPlayerId &&
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