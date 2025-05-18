using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 이동 시 플레이어와 NPC의 데이터를 PlayerPrefs를 통해 유지하기 위한 싱글톤 매니저 클래스
/// </summary>
public class GameDataManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameDataManager Instance { get; private set; }

    // PlayerPrefs 키 접두사
    private const string COINS_KEY_PREFIX = "Coins_";
    private const string STARS_KEY_PREFIX = "Stars_";
    
    // 씬 로드 이벤트 구독 플래그
    private bool isSubscribed = false;

    private void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬 전환 시에도 파괴되지 않도록 설정
            
            // 씬 로드 이벤트 구독
            if (!isSubscribed)
            {
                SceneManager.sceneLoaded += OnSceneLoaded;
                isSubscribed = true;
            }
        }
        else
        {
            Destroy(gameObject); // 이미 인스턴스가 존재하면 현재 오브젝트 파괴
        }
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (isSubscribed && Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            isSubscribed = false;
        }
    }

    /// <summary>
    /// 씬 로드 시 호출되는 이벤트 핸들러
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 로드 후 데이터 복원
        RestoreGameData();
    }

    /// <summary>
    /// 현재 게임 상태의 데이터를 PlayerPrefs에 저장
    /// </summary>
    public void SaveGameData()
    {
        Debug.Log("게임 데이터를 PlayerPrefs에 저장 중...");

        // 플레이어 데이터 저장
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            string playerName = playerStats.gameObject.name;
            PlayerPrefs.SetInt(COINS_KEY_PREFIX + playerName, playerStats.Coins);
            PlayerPrefs.SetInt(STARS_KEY_PREFIX + playerName, playerStats.Stars);
            Debug.Log($"플레이어 데이터 저장: {playerName}, 코인: {playerStats.Coins}, 별: {playerStats.Stars}");
        }

        // NPC 데이터 저장
        NPCStats[] npcStatsArray = FindObjectsByType<NPCStats>(FindObjectsSortMode.None);

        foreach (NPCStats npcStats in npcStatsArray)
        {
            string npcName = npcStats.gameObject.name;
            PlayerPrefs.SetInt(COINS_KEY_PREFIX + npcName, npcStats.Coins);
            PlayerPrefs.SetInt(STARS_KEY_PREFIX + npcName, npcStats.Stars);
            Debug.Log($"NPC 데이터 저장: {npcName}, 코인: {npcStats.Coins}, 별: {npcStats.Stars}");
        }

        // 변경사항 즉시 저장
        PlayerPrefs.Save();
    }

    /// <summary>
    /// PlayerPrefs에서 저장된 게임 데이터를 현재 씬에 복원
    /// </summary>
    public void RestoreGameData()
    {
        Debug.Log("PlayerPrefs에서 게임 데이터 복원 중...");

        // 플레이어 데이터 복원
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            string playerName = playerStats.gameObject.name;
            if (PlayerPrefs.HasKey(COINS_KEY_PREFIX + playerName))
            {
                int coins = PlayerPrefs.GetInt(COINS_KEY_PREFIX + playerName);
                int stars = PlayerPrefs.GetInt(STARS_KEY_PREFIX + playerName, 0);
                
                playerStats.SetCoins(coins);
                playerStats.SetStars(stars);
                Debug.Log($"플레이어 데이터 복원: {playerName}, 코인: {coins}, 별: {stars}");
            }
        }

        // NPC 데이터 복원
        NPCStats[] npcStatsArray = FindObjectsByType<NPCStats>(FindObjectsSortMode.None);
        
        foreach (NPCStats npcStats in npcStatsArray)
        {
            string npcName = npcStats.gameObject.name;
            if (PlayerPrefs.HasKey(COINS_KEY_PREFIX + npcName))
            {
                int coins = PlayerPrefs.GetInt(COINS_KEY_PREFIX + npcName);
                int stars = PlayerPrefs.GetInt(STARS_KEY_PREFIX + npcName, 0);
                
                npcStats.SetCoins(coins);
                npcStats.SetStars(stars);
                Debug.Log($"NPC 데이터 복원: {npcName}, 코인: {coins}, 별: {stars}");
            }
        }
    }

    /// <summary>
    /// 씬 전환 시 호출하는 메서드
    /// </summary>
    public void LoadScene(string sceneName)
    {
        // 현재 게임 데이터 저장
        SaveGameData();
        
        // 씬 로드
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 미니게임 결과를 반영하는 메서드
    /// </summary>
    public void ApplyMinigameResults(KeyValuePair<string, int> playerResults, List<KeyValuePair<string, int>> npcResults)
    {
        // 플레이어 결과 반영
        string playerName = playerResults.Key;
        int playerCoinsEarned = playerResults.Value;
        if (PlayerPrefs.HasKey(COINS_KEY_PREFIX + playerName))
        {
            int currentCoins = PlayerPrefs.GetInt(COINS_KEY_PREFIX + playerName);
            PlayerPrefs.SetInt(COINS_KEY_PREFIX + playerName, currentCoins + playerCoinsEarned);
        }

        // NPC 결과 반영
        foreach (var npcResult in npcResults)
        {
            string npcName = npcResult.Key;
            int npcCoinsEarned = npcResult.Value;

            if (PlayerPrefs.HasKey(COINS_KEY_PREFIX + npcName))
            {
                int currentCoins = PlayerPrefs.GetInt(COINS_KEY_PREFIX + npcName);
                PlayerPrefs.SetInt(COINS_KEY_PREFIX + npcName, currentCoins + npcCoinsEarned);
            }
        }
        
        // 변경사항 즉시 저장
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 모든 저장된 데이터 초기화 (테스트용)
    /// </summary>
    public void ClearAllData()
    {
        Debug.Log("모든 PlayerPrefs 데이터 초기화 중...");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
