using UnityEngine.Events;
using System.Collections;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using System;

// TurnUI 클래스 개선
public class TurnUI : MonoBehaviour
{
    [SerializeField] private PlayerController currentPlayer;
    [SerializeField] private CanvasGroup actionsCanvasGroup;
    [SerializeField] private CanvasGroup rollCanvasGroup;
    [SerializeField] private CanvasGroup starPurchasCanvasGroup;

    [Header("Player Turn Indicator")]
    [SerializeField] private TextMeshProUGUI currentPlayerNameText;
    [SerializeField] private Image currentPlayerIcon;
    [SerializeField] private Sprite[] playerIcons; // 플레이어와 NPC 아이콘

    [Header("Coin and Star References")]
    [SerializeField] private TextMeshProUGUI startCountLabel;
    [SerializeField] private TextMeshProUGUI coinCountLabel;

    [Header("States")]
    private bool isShowingBoard;

    [Header("Turn UI References")]
    [SerializeField] private Button diceButton;
    [SerializeField] private Button itemButton;
    [SerializeField] private Button boardButton;

    [Header("Star Purchase UI References")]
    [SerializeField] private Button starConfirmButton;
    public Button StarButton => starConfirmButton;
    [SerializeField] private Button starCancelButton;
    public Button CancelStarButton => starCancelButton;

    [Header("Overlay Camera Settings")]
    [SerializeField] private CinemachineCameraOffset overlayCameraOffset;
    private Vector3 originalCameraOffset;
    [SerializeField] private float disableCameraOffset;

    private GameObject lastSelectedButton;
    private BaseStats currentPlayerStats;

    void Awake()
    {
        actionsCanvasGroup.alpha = 0;
        diceButton.onClick.AddListener(OnDiceButtonSelect);
        boardButton.onClick.AddListener(OnBoardButtonSelect);
        originalCameraOffset = overlayCameraOffset.Offset;
        lastSelectedButton = diceButton.gameObject;

        EventSystem.current.GetComponent<InputSystemUIInputModule>().cancel.action.performed += CancelPerformed;
    }

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
        lastSelectedButton = diceButton.gameObject;
        if (currentPlayer != null)
        {
            currentPlayer.PrepareToRoll();
        }
        ShowUI(false);
    }

    private void OnBoardButtonSelect()
    {
        lastSelectedButton = boardButton.gameObject;
        SetBoardView(true);
    }

    void SetBoardView(bool view)
    {
        // FindObjectOfType 대신 GameManager를 통해 참조 획득
        CameraHandler cameraHandler = null;
        if (GameManager.Instance != null)
        {
            cameraHandler = GameManager.Instance.GetComponent<CameraHandler>();
        }

        if (cameraHandler != null)
        {
            cameraHandler.ShowBoard(view);
            ShowUI(!view);
            isShowingBoard = view;
        }
    }

    void ShowUI(bool show)
    {
        // 플레이어 턴일 때만 UI 표시
        bool isPlayerTurn = currentPlayer != null;

        if (isPlayerTurn)
        {
            actionsCanvasGroup.DOFade(show ? 1 : 0, .3f);

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
            actionsCanvasGroup.DOFade(0, .3f);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void CancelPerformed(InputAction.CallbackContext context)
    {
        if (isShowingBoard)
        {
            SetBoardView(false);
        }
    }

    void CameraOffset(float x)
    {
        overlayCameraOffset.Offset = originalCameraOffset + new Vector3(x, 0, 0);
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
