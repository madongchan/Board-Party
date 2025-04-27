using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// TurnUI 클래스 개선
public class TurnUI : MonoBehaviour
{
    [SerializeField] private BaseController currentPlayer;
    [SerializeField] private CanvasGroup rollCanvasGroup;
    [SerializeField] private CanvasGroup starPurchasCanvasGroup;

    [Header("Player Turn Indicator")]
    [SerializeField] private TextMeshProUGUI currentPlayerNameText;
    [SerializeField] private Image currentPlayerIcon;
    [SerializeField] private Sprite[] playerIcons; // 플레이어와 NPC 아이콘

    [Header("Coin and Star References")]
    [SerializeField] private TextMeshProUGUI startCountLabel;
    [SerializeField] private TextMeshProUGUI coinCountLabel;

    [Header("Star Purchase UI References")]
    [SerializeField] private Button starConfirmButton;
    public Button StarButton => starConfirmButton;
    [SerializeField] private Button starCancelButton;
    public Button CancelStarButton => starCancelButton;

    private GameObject lastSelectedButton;
    private BaseStats currentPlayerStats;

    private void StatAnimation(int coinCount)
    {
        coinCountLabel.text = coinCount.ToString();
    }

    private void UpdatePlayerStats()
    {
        if (currentPlayerStats != null)
        {
            startCountLabel.text = currentPlayerStats.Stars.ToString();
            coinCountLabel.text = currentPlayerStats.Coins.ToString();
        }
    }

    public void StartPlayerTurn(PlayerController player)
    {
        currentPlayer = player;
        currentPlayerStats = player.GetComponent<BaseStats>();

        // 플레이어 이름과 아이콘 업데이트
        if (currentPlayerNameText != null)
        {
            currentPlayerNameText.text = "플레이어";
        }
        if (currentPlayerIcon != null)
        {
            currentPlayerIcon.sprite = playerIcons[0]; // 플레이어 아이콘
        }

        // 이벤트 리스너 설정
        if (currentPlayerStats != null)
        {
            currentPlayerStats.OnInitialize.RemoveAllListeners();
            currentPlayerStats.OnAnimation.RemoveAllListeners();
            currentPlayerStats.OnInitialize.AddListener(UpdatePlayerStats);
            currentPlayerStats.OnAnimation.AddListener(StatAnimation);
            UpdatePlayerStats();
        }

        ShowUI(false);
        OnDiceButtonSelect();
    }

    // NPC 턴 표시 업데이트
    public void UpdateTurnDisplay(NPCController npcController)
    {
        // 현재 플레이어 참조 업데이트 (임시 방편)
        currentPlayer = null;
        currentPlayerStats = npcController.GetComponent<BaseStats>();
        if (currentPlayerNameText != null)
        {
            // NPC 이름과 아이콘 업데이트
            currentPlayerNameText.text = npcController.name;
        }
        // NPC 아이콘 설정
        int npcIconIndex = 1; // 기본 NPC 아이콘
        if (currentPlayerIcon != null)
        {
            currentPlayerIcon.sprite = playerIcons[npcIconIndex];
        }
        // 이벤트 리스너 설정
        if (currentPlayerStats != null)
        {
            currentPlayerStats.OnInitialize.RemoveAllListeners();
            currentPlayerStats.OnAnimation.RemoveAllListeners();
            currentPlayerStats.OnInitialize.AddListener(UpdatePlayerStats);
            currentPlayerStats.OnAnimation.AddListener(StatAnimation);
            UpdatePlayerStats();
        }

        // UI 숨기기 (NPC 턴에는 플레이어 액션 UI 비활성화)
        ShowUI(false);
    }

    private void OnDiceButtonSelect()
    {
        if (currentPlayer != null)
        {
            currentPlayer.PrepareToRoll();
        }
        ShowUI(false);
    }

    void ShowUI(bool show)
    {
        // 플레이어 턴일 때만 UI 표시
        bool isPlayerTurn = currentPlayer != null;

        if (isPlayerTurn)
        {
            StartCoroutine(EventSystemSelectionDelay());

            IEnumerator EventSystemSelectionDelay()
            {
                yield return new WaitForSeconds(show ? .3f : 0);
                EventSystem.current.SetSelectedGameObject(show ? lastSelectedButton : null);
            }
        }
        else
        {
            // NPC 턴일 때는 UI 숨기기
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void FadeRollText(bool fadeText)
    {
        rollCanvasGroup.DOFade(fadeText ? 0 : 1, .3f);
    }

    public void ShowStarPurchaseUI(bool show)
    {
        starPurchasCanvasGroup.DOFade(show ? 1 : 0, .2f);
        if (show)
            EventSystem.current.SetSelectedGameObject(starConfirmButton.gameObject);
        else
            EventSystem.current.SetSelectedGameObject(null);
    }
}