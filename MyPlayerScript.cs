using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Minifantasy;
using TMPro;

public class MyPlayerScript : MonoBehaviour
{
    [Header("設定")]
    public int MaxLife = 5;
    public GameObject LifeContainer;   //  Horizontal Layout Group を持つ GameObject
    public GameObject HeartPrefab;     //  ハート（Image）のプレハブ
    public TextMeshProUGUI StageText;
    public GameObject StopCanvas;
    public GameObject DieCanvas;
    public GameObject Canvas;

    [Header("ボタン")]
    public Button StopButton;
    public Button ResumeButton;
    public Button QuitButton;
    public Button TitleButton;
    public Button RestartButton;

    [Header("サウンド")]
    public AudioClip ItemSound;
    public AudioClip DieSound;
    public AudioClip AttackSound;
    public AudioClip ButtonSound;
    public AudioClip bgmSound;
    public AudioClip WrongSound;

    [Header("Explosion Effect")]
    public GameObject explosionEffectPrefab;

    private int currentLife;
    private bool isPaused = false;
    private bool isDying = false;

    private AudioSource sfxSource;
    private AudioSource bgmSource;
    private AudioSource uiSource;

    private float dieTimer = 0f;
    private const float DieDelay = 1.0f;

    private float lastInputTime = -1f; 
    private const float inputCooldown = 0.3f; 

    private List<GameObject> heartIcons = new List<GameObject>();

    void Awake()
    {
        sfxSource = gameObject.AddComponent<AudioSource>();
        bgmSource = gameObject.AddComponent<AudioSource>();
        bgmSource.loop = true;
        uiSource = gameObject.AddComponent<AudioSource>();
        uiSource.playOnAwake = false;
        uiSource.ignoreListenerPause = true;
    }

    void Start()
    {
        //  ป้องกันค้าง Pause จาก Scene ก่อนหน้า
        Time.timeScale = 1f;
        isPaused = false;
        isDying = false;

        currentLife = PlayerPrefs.GetInt("PlayerLife", MaxLife);
        CreateHeartIcons();

        if (bgmSound)
        {
            bgmSource.clip = bgmSound;
            bgmSource.Play();
        }

        //  แสดง UI ให้ถูกต้องเมื่อเริ่มเกมใหม่
        StopCanvas?.SetActive(false);
        DieCanvas?.SetActive(false);
        Canvas?.SetActive(true);

        if (StageText != null)
            StageText.text = SceneManager.GetActiveScene().name;

        StopButton?.onClick.AddListener(OnPauseButton);
        ResumeButton?.onClick.AddListener(OnResumeButton);
        QuitButton?.onClick.AddListener(OnQuitButton);
        RestartButton?.onClick.AddListener(OnRestartButton);
        TitleButton?.onClick.AddListener(OnTitleButton);

#if UNITY_2023_1_OR_NEWER
    var joystick = FindFirstObjectByType<ArduinoJoystick>();
#else
        var joystick = FindObjectOfType<ArduinoJoystick>();
#endif

        if (joystick != null)
        {
            joystick.StopCanvas = StopCanvas;
            joystick.DieCanvas = DieCanvas;
        }
    }


    void Update()
    {
        // プレイヤーが死亡した場合、死亡シーケンスを開始
        if (!isDying && currentLife <= 0)
        {
            StartDeathSequence();
        }
        if (currentLife == 1)
        {
            NotifyEnemiesPlayerLowHealth();
        }
        else
        {
            NotifyEnemiesPlayerNormalHealth();
        }
        // 死亡時のディレイ処理
        if (isDying)
        {
            dieTimer += Time.unscaledDeltaTime;
            if (dieTimer >= DieDelay)
                HandleDeath();
            // Enter または KeypadEnter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                lastInputTime = Time.unscaledTime;

                if (StopCanvas.activeSelf)
                {
                    uiSource.PlayOneShot(ButtonSound);
                    OnResumeButton(); // 一時停止解除
                }
                else if (DieCanvas.activeSelf)
                {
                    uiSource.PlayOneShot(ButtonSound);
                    OnRestartButton(); // 再スタート
                }
            }
            // ESC または Backspace
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                lastInputTime = Time.unscaledTime;
                uiSource.PlayOneShot(ButtonSound);
                OnTitleButton(); // 終了
            }
            return; // 死亡中は他の入力を受け付けない
        }

        // 入力のクールダウン
        if (Time.unscaledTime - lastInputTime < inputCooldown)
            return;

        // ESC または Backspace
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            lastInputTime = Time.unscaledTime;

            if (!StopCanvas.activeSelf && !DieCanvas.activeSelf)
            {
                uiSource.PlayOneShot(ButtonSound);
                OnPauseButton(); // 一時停止メニューを開く
            }
            else if (StopCanvas.activeSelf)
            {
                uiSource.PlayOneShot(ButtonSound);
                OnQuitButton(); // 終了
            }
        }


        // ゲームが一時停止中なら他の処理を停止
        if (isPaused)
        {
            // Enter または KeypadEnter
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                lastInputTime = Time.unscaledTime;

                if (StopCanvas.activeSelf)
                {
                    uiSource.PlayOneShot(ButtonSound);
                    OnResumeButton(); // 一時停止解除
                }
                else if (DieCanvas.activeSelf)
                {
                    uiSource.PlayOneShot(ButtonSound);
                    OnRestartButton(); // 再スタート
                }
            }

            return;
        }
    }


    private void OnTriggerEnter2D(Collider2D target)
    {
        if (target.CompareTag("Item"))
        {
            Destroy(target.gameObject);
            if (currentLife < MaxLife)
            {
                currentLife++;
                UpdateHeartDisplay();
                sfxSource.PlayOneShot(ItemSound);
            }
            else
            {
                sfxSource.PlayOneShot(WrongSound);
            }
        }
        else if (target.CompareTag("Enemy"))
        {
            if (currentLife > 0)
            {
                currentLife--;
                UpdateHeartDisplay();
                sfxSource.PlayOneShot(AttackSound);
                Vector3 hitPosition = target.transform.position;
                EnemyGiantFollow giantFollow = target.GetComponent<EnemyGiantFollow>();
                EnemyChargedFollow giantFollow1 = target.GetComponent<EnemyChargedFollow>();
                if (giantFollow != null)
                {
                    giantFollow.TriggerDelayedDestroy();
                }
                else if (giantFollow1 != null)
                {
                    giantFollow1.TriggerDelayedDestroy1();
                }
                else
                {
                    Destroy(target.gameObject);
                }

                if (explosionEffectPrefab != null)
                {
                    GameObject explosion = Instantiate(explosionEffectPrefab, hitPosition, Quaternion.identity);
                    Destroy(explosion, 2f);
                }
            }
        }
        else if (target.CompareTag("Enemy1"))
        {
            currentLife--;
            UpdateHeartDisplay();
            sfxSource.PlayOneShot(AttackSound);
            Vector3 hitPosition = target.transform.position;
            Destroy(target.gameObject);
            if (explosionEffectPrefab != null)
            {
                GameObject explosion = Instantiate(explosionEffectPrefab, hitPosition, Quaternion.identity);
                Destroy(explosion, 2f);
            }
        }
    }


    private void StartDeathSequence()
    {
        if (isDying) return;

        isDying = true;
        dieTimer = 0f;
        sfxSource.PlayOneShot(DieSound);
        bgmSource.Pause();
        SaveLife();
    }

    private void HandleDeath()
    {
        Time.timeScale = 0f;
        DieCanvas?.SetActive(true);
        Canvas?.SetActive(false);
    }

    private void PauseGame() // 一時停止
    {
        isPaused = true;
        Time.timeScale = 0f;
        StopCanvas?.SetActive(true);
        Canvas?.SetActive(false);
        bgmSource.Pause();
    }

    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        StopCanvas?.SetActive(false);
        Canvas?.SetActive(true);
        bgmSource.UnPause();
    }

    private void ResetLife()
    {
        currentLife = MaxLife;
        UpdateHeartDisplay();
        SaveLife();
    }

    //  ゲーム開始時にすべてのハートを生成する
    private void CreateHeartIcons()
    {
        foreach (Transform child in LifeContainer.transform)
            Destroy(child.gameObject);

        heartIcons.Clear();

        for (int i = 0; i < MaxLife; i++)
        {
            GameObject heart = Instantiate(HeartPrefab, LifeContainer.transform);
            heartIcons.Add(heart);
        }

        UpdateHeartDisplay();
    }

    //  ハートの表示を更新する
    private void UpdateHeartDisplay() 
    {
        for (int i = 0; i < heartIcons.Count; i++)
        {
            heartIcons[i].SetActive(i < currentLife);
        }
    }

    private void SaveLife()
    {
        PlayerPrefs.SetInt("PlayerLife", currentLife);
        PlayerPrefs.Save();
    }

    private void OnPauseButton()
    {
        sfxSource.PlayOneShot(ButtonSound);
        PauseGame();
    }

    private void OnResumeButton()
    {
        sfxSource.PlayOneShot(ButtonSound);
        ResumeGame();
    }

    private void OnQuitButton()
    {
        sfxSource.PlayOneShot(ButtonSound);
        ResetLife();
        if (KillManager.Instance != null)
            KillManager.Instance.ResetKills();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }

    private void OnRestartButton()
    {
        Time.timeScale = 1f;
        sfxSource.PlayOneShot(ButtonSound);
        ResetLife();
        if (KillManager.Instance != null)
            KillManager.Instance.ResetKills();
        isPaused = false;
        isDying = false;
        Time.timeScale = 1f;
        if (bgmSource) bgmSource.Stop();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnTitleButton()
    {
        sfxSource.PlayOneShot(ButtonSound);
        ResetLife();
        if (KillManager.Instance != null)
            KillManager.Instance.ResetKills();
        Time.timeScale = 1f;
        SceneManager.LoadScene("Title");
    }

    private void OnDisable()
    {
        SaveLife();
    }
    private void NotifyEnemiesPlayerLowHealth()
    {
        EnemyGiantFollow[] enemies = FindObjectsByType<EnemyGiantFollow>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.OnPlayerLowHealth();
        }
    }
    private void NotifyEnemiesPlayerNormalHealth()
    {
        EnemyGiantFollow[] enemies = FindObjectsByType<EnemyGiantFollow>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            enemy.OnPlayerNormalHealth();
        }
    }
    public int GetCurrentLife()
    {
        return currentLife;
    }
}

