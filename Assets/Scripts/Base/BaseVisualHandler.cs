using UnityEngine;
using DG.Tweening;
using UnityEngine.Splines;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using Unity.Cinemachine;

public abstract class BaseVisualHandler : MonoBehaviour
{
    protected Animator animator;
    protected BaseController baseController;
    protected BaseStats baseStats;
    protected SplineKnotAnimate splineKnotAnimator;
    protected SplineKnotInstantiate splineKnotData;

    [Header("References")]
    [SerializeField] protected Transform characterModel;
    [SerializeField] protected Transform characterDice;
    [SerializeField] protected Transform junctionVisual;
    [SerializeField] protected Transform junctionArrowPrefab;
    protected List<GameObject> junctionList;

    [Header("JumpParameters")]
    [SerializeField] protected Color selectedJunctionColor;
    [SerializeField] protected Color defaultJunctionColor;

    [Header("Jump Parameters")]
    [SerializeField] protected int jumpPower = 1;
    [SerializeField] protected float jumpDuration = .2f;

    [Header("Dice Parameters")]
    public float rotationSpeed = 360f;
    public float tiltAmplitude = 15f;
    public float tiltFrequency = 2f;
    protected float tiltTime = 0f;
    [SerializeField] protected float numberAnimationSpeed = .15f;
    protected TextMeshPro[] numberLabels;

    [Header("States")]
    protected bool diceSpinning;

    [Header("Particles")]
    [SerializeField] protected ParticleSystem coinGainParticle;
    [SerializeField] protected ParticleSystem coinLossParticle;
    [SerializeField] protected ParticleSystem diceHitParticle;
    [SerializeField] protected ParticleSystem diceResultParticle;
    protected float particleRepeatInterval;

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        baseController = GetComponentInParent<BaseController>();
        baseStats = GetComponentInParent<BaseStats>();
        splineKnotAnimator = GetComponentInParent<SplineKnotAnimate>();
        numberLabels = characterDice.GetComponentsInChildren<TextMeshPro>();

        // GameManager를 통해 SplineKnotData 참조 획득
        if (GameManager.Instance != null && GameManager.Instance.SplineKnotData != null)
            splineKnotData = GameManager.Instance.SplineKnotData;
        
        if (coinGainParticle != null)
            particleRepeatInterval = coinGainParticle.emission.GetBurst(0).repeatInterval; // 파티클의 반복 간격을 가져옴

        // 이벤트 리스너 등록
        RegisterEventListeners();
    }

    protected virtual void RegisterEventListeners()
    {
        if (baseController != null)
        {
            baseController.OnRollStart.AddListener(OnRollStart);
            baseController.OnRollJump.AddListener(OnRollJump);
            baseController.OnRollCancel.AddListener(OnRollCancel);
            baseController.OnRollDisplay.AddListener(OnRollDisplay);
            baseController.OnRollEnd.AddListener(OnRollEnd);
            baseController.OnMovementStart.AddListener(OnMovementStart);
        }

        if (splineKnotAnimator != null)
        {
            splineKnotAnimator.OnEnterJunction.AddListener(OnEnterJunction);
            splineKnotAnimator.OnJunctionSelection.AddListener(OnJunctionSelection);
            splineKnotAnimator.OnKnotLand.AddListener(OnKnotLand);
        }
    }

    protected virtual void OnRollStart()
    {
        transform.DOLookAt(Camera.main.transform.position, .35f, AxisConstraint.Y);

        diceSpinning = true;

        StartCoroutine(RandomDiceNumberCoroutine());

        characterDice.gameObject.SetActive(true);
        characterDice.DOScale(0, .3f).From();
    }

    protected virtual void OnRollCancel()
    {
        diceSpinning = false;
        characterDice.DOComplete();
        characterDice.DOScale(0, .12f).OnComplete(() => { characterDice.gameObject.SetActive(false); characterDice.transform.localScale = Vector3.one; });
    }

    protected virtual void OnRollJump()
    {
        characterModel.DOComplete();
        characterModel.DOJump(transform.position, jumpPower, 1, jumpDuration);
        animator.SetTrigger("RollJump");
        transform.DOLocalMoveY(0.5f, 0.3f).SetLoops(2, LoopType.Yoyo);
    }

    protected virtual void OnRollDisplay(int roll)
    {
        if (diceHitParticle != null)
        {
            diceHitParticle.Play();
            if (diceHitParticle.GetComponent<CinemachineImpulseSource>() != null)
                diceHitParticle.GetComponent<CinemachineImpulseSource>().GenerateImpulse();
        }
        
        characterDice.DOComplete();
        diceSpinning = false;
        SetDiceNumber(roll);
        characterDice.transform.eulerAngles = Vector3.zero;
        Vector3 diceLocalPos = characterDice.localPosition;
        characterDice.DOLocalJump(diceLocalPos, .8f, 1, .25f);
        characterDice.DOPunchScale(Vector3.one / 4, .3f, 10, 1);
    }

    protected virtual void OnRollEnd()
    {
        characterDice.gameObject.SetActive(false);
        if (diceResultParticle != null)
            diceResultParticle.Play();
    }

    protected virtual void OnMovementStart(bool movement)
    {
        if (movement)
        {
            transform.DOLocalRotate(Vector3.zero, .3f);
        }
        else
        {
            transform.DOLookAt(Camera.main.transform.position, .35f, AxisConstraint.Y);
        }
    }

    protected virtual void OnKnotLand(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;
        
        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];

        if (coinGainParticle != null && coinLossParticle != null && baseStats != null)
        {
            short count = (short)(data.coinGain > 0 ? 1 : Mathf.Clamp(Mathf.Abs(data.coinGain), 0, baseStats.Coins));
            int cycle = data.coinGain > 0 ? Mathf.Abs(data.coinGain) : 1;
            ParticleSystem.Burst burst = new ParticleSystem.Burst(0, count, count, cycle, particleRepeatInterval / Mathf.Sqrt(count));
            coinGainParticle.emission.SetBurst(0, burst);
            coinLossParticle.emission.SetBurst(0, burst);
            int animationRepetition = coinGainParticle.emission.GetBurst(0).cycleCount;
            bool firstAnimation = true;

            if (data.coinGain > 0)
            {
                coinGainParticle.Play();
                StartCoroutine(StatUpdateCoroutine());
            }
            else if (data.coinGain < 0)
            {
                coinLossParticle.Play();
                StartCoroutine(DelayCoroutine());
            }

            animator.SetTrigger(data.coinGain > 0 ? "Happy" : "Sad");

            IEnumerator StatUpdateCoroutine()
            {
                animationRepetition--;
                yield return new WaitForSeconds(firstAnimation ? coinGainParticle.main.startLifetime.constant : coinGainParticle.emission.GetBurst(0).repeatInterval);
                firstAnimation = false;
                baseStats.CoinAnimation(data.coinGain > 0 ? 1 : -1);
                if (animationRepetition > 0)
                    StartCoroutine(StatUpdateCoroutine());
            }

            IEnumerator DelayCoroutine()
            {
                yield return new WaitForSeconds(.15f);
                baseStats.UpdateStats();
            }
        }
    }

    protected virtual void OnEnterJunction(bool junction)
    {
        animator.SetBool("InJunction", junction);

        if (!junction)
        {
            if (junctionList != null)
            {
                foreach (GameObject go in junctionList)
                {
                    go.transform.DOComplete();
                    go.transform.GetChild(0).DOComplete();
                    Destroy(go);
                }
            }
            return;
        }

        junctionList = new List<GameObject>();
        junctionVisual.DOComplete();
        junctionVisual.DOScale(0, .2f).From().SetEase(Ease.OutBack);
        
        for (int i = 0; i < splineKnotAnimator.walkableKnots.Count; i++)
        {
            GameObject junctionObject = Instantiate(junctionArrowPrefab.gameObject, junctionVisual);
            junctionList.Add(junctionObject);
            junctionObject.transform.LookAt(splineKnotAnimator.GetJunctionPathPosition(i), transform.up);
        }
    }

    protected virtual void OnJunctionSelection(int junctionIndex)
    {
        if (junctionList == null || junctionIndex >= junctionList.Count) return;
        
        for (int i = 0; i < junctionList.Count; i++)
        {
            if (i != junctionIndex)
            {
                junctionList[i].GetComponentInChildren<Renderer>().material.color = defaultJunctionColor;
                junctionList[i].transform.GetChild(0).DOComplete();
                junctionList[i].transform.GetChild(0).DOScale(.2f, .2f);
            }
        }

        junctionList[junctionIndex].GetComponentInChildren<Renderer>().material.color = selectedJunctionColor;
        junctionList[junctionIndex].transform.DOComplete();
        junctionList[junctionIndex].transform.DOPunchScale(Vector3.one / 4, .3f, 10, 1);
        junctionList[junctionIndex].transform.GetChild(0).DOComplete();
        junctionList[junctionIndex].transform.GetChild(0).DOScale(.8f, .3f).SetEase(Ease.OutBack);
    }

    protected virtual void Update()
    {
        if (animator != null && splineKnotAnimator != null)
        {
            float speed = splineKnotAnimator.isMoving ? 1 : 0;
            speed = splineKnotAnimator.Paused ? 0 : speed;
            float fadeSpeed = splineKnotAnimator.isMoving ? .1f : .05f;

            animator.SetFloat("Blend", speed, fadeSpeed, Time.deltaTime);
        }

        if (diceSpinning)
            SpinDice();
    }

    protected virtual void SpinDice()
    {
        if (characterDice == null) return;
        
        characterDice.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        tiltTime += Time.deltaTime * tiltFrequency;
        float tiltAngle = Mathf.Sin(tiltTime) * tiltAmplitude;

        characterDice.rotation = Quaternion.Euler(tiltAngle, characterDice.rotation.eulerAngles.y, 0);
    }

    protected virtual IEnumerator RandomDiceNumberCoroutine()
    {
        if (diceSpinning == false)
            yield break;

        // Random.Range는 정수형일 경우 min값은 포함하고 max값은 포함하지 않음
        int num = Random.Range(1, 7); // 1~6까지의 랜덤 숫자 생성
        SetDiceNumber(num);
        yield return new WaitForSeconds(numberAnimationSpeed);
        StartCoroutine(RandomDiceNumberCoroutine());
    }

    public virtual void SetDiceNumber(int value)
    {
        if (numberLabels == null) return;
        
        foreach (TextMeshPro p in numberLabels)
        {
            p.text = value.ToString();
        }
    }
    
    // 공통 애니메이션 메서드
    public virtual void PlayJumpAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Jump");
    }
    
    public virtual void SetMovingAnimation(bool isMoving)
    {
        if (animator != null)
            animator.SetBool("Move", isMoving);
    }
    
    public virtual void PlayCelebrateAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Happy");
    }
    
    public virtual void PlaySadAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Sad");
    }
}