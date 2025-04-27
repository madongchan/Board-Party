using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// // MinigameManager.cs (신규)
public class MinigameManager : MonoBehaviour
{
    //     public static MinigameManager Instance { get; private set; }

    //     [SerializeField] private List<MinigameData> availableMinigames;
    //     private MinigameData currentMinigame;
    //     private Dictionary<BaseController, int> minigameScores = new Dictionary<BaseController, int>();

    //     // 미니게임 시작
    //     public void StartMinigame(MinigameData minigame)
    //     {
    //         currentMinigame = minigame;

    //         // 현재 게임 상태 저장
    //         SaveManager.Instance.SaveGameState();

    //         // 미니게임 씬으로 전환
    //         GameEvents.OnMinigameStart.Invoke();
    //         SceneManager.LoadScene(minigame.sceneName);
    //     }

    //     // 미니게임 결과 처리
    //     public void EndMinigame(Dictionary<BaseController, int> scores)
    //     {
    //         minigameScores = scores;

    //         // 점수에 따라 플레이어 순위 결정
    //         List<BaseController> rankings = DetermineRankings(scores);

    //         // 코인 보상 지급
    //         DistributeRewards(rankings);

    //         // 메인 게임으로 돌아가기
    //         ReturnToMainGame();
    //     }

    //     // 순위 결정
    //     private List<BaseController> DetermineRankings(Dictionary<BaseController, int> scores)
    //     {
    //         // 점수에 따라 플레이어 정렬
    //         return scores.OrderByDescending(pair => pair.Value)
    //                     .Select(pair => pair.Key)
    //                     .ToList();
    //     }

    //     // 보상 지급
    //     private void DistributeRewards(List<BaseController> rankings)
    //     {
    //         for (int i = 0; i < rankings.Count; i++)
    //         {
    //             BaseController player = rankings[i];
    //             BaseStats stats = player.GetComponent<BaseStats>();

    //             // 순위에 따른 코인 보상
    //             int reward = currentMinigame.coinRewards[i];
    //             stats.AddCoins(reward);

    //             // 이벤트 발생
    //             GameEvents.OnCoinsChanged.Invoke(player, reward);
    //         }
    //     }

    //     // 메인 게임으로 돌아가기
    //     private void ReturnToMainGame()
    //     {
    //         // 메인 게임 씬으로 전환
    //         SceneManager.LoadScene("MainGame");

    //         // 저장된 게임 상태 불러오기
    //         //SaveManager.Instance.LoadGameState();

    //         // 미니게임 결과 이벤트 발생
    //         int[] rankings = minigameScores.Keys.Select(player => 
    //             GameManager.Instance.players.IndexOf(player)).ToArray();
    //         GameEvents.OnMinigameEnd.Invoke(rankings);
    //     }
}
