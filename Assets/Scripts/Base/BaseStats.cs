using UnityEngine;
using UnityEngine.Events;

// 기본 Stats 클래스 (추상 클래스)
public abstract class BaseStats : MonoBehaviour
{
    [SerializeField] protected int coins;
    public int Coins => coins;
    [SerializeField] protected int stars;
    public int Stars => stars;
    public int coinsBeforeChange;

    [HideInInspector] public UnityEvent OnInitialize;
    [HideInInspector] public UnityEvent<int> OnAnimation;

    protected virtual void Start()
    {
        OnInitialize.Invoke();
        coinsBeforeChange = coins;
    }

    public virtual void AddCoins(int amount)
    {
        coinsBeforeChange = coins;
        coins += amount;
        coins = Mathf.Clamp(coins, 0, 999);
    }

    public virtual void AddStars(int amount)
    {
        stars += amount;
        stars = Mathf.Clamp(stars, 0, 999);
    }

    public virtual void CoinAnimation(int value)
    {
        coinsBeforeChange += value;
        OnAnimation.Invoke(coinsBeforeChange);
    }

    public virtual void UpdateStats()
    {
        OnInitialize.Invoke();
        coinsBeforeChange = coins;
    }
}