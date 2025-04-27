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
    [HideInInspector] public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();
    [HideInInspector] public UnityEvent<int> OnStarsChanged = new UnityEvent<int>();

    protected virtual void Start()
    {
        OnInitialize.Invoke();
        coinsBeforeChange = coins;
    }

    public virtual void AddCoins(int amount)
    {
        coins += amount;
        OnCoinsChanged.Invoke(amount); // 이벤트 발생
        UpdateStats();
    }

    public virtual void AddStars(int amount)
    {
        stars += amount;
        OnStarsChanged.Invoke(amount); // 이벤트 발생
        UpdateStats();
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