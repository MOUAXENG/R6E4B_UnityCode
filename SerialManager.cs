using System;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SerialManager : MonoBehaviour
{
    public static SerialManager Instance;
    [Header("COM 設定 / COM Settings")]
    public int espPortNumber = 3;      // ESP32 の COM
    public int maikonPortNumber = 10;  // マイコンの COM
    private SerialPort espSerial;
    private SerialPort maikonSerial;
    private Thread readThread;
    private volatile bool keepReading = true;
    public volatile bool attackTriggered = false;
    private const int ESP_BAUD = 115200;
    private const int MAIKON_BAUD = 9600;
    [Header("UI 設定 / UI Elements")]
    public Canvas mainCanvas;   // ゲームのメイン Canvas
    public Canvas comCanvas;    // COM 設定 Canvas
    public Button settingsButton;
    public TMP_Text espText;
    public TMP_Text maikonText;
    public TMP_Text espStatusText;
    public TMP_Text maikonStatusText;
    private int tempEspCom;
    private int tempMaikonCom;
    private string inputBuffer = "";
    private enum InputState { Normal, SettingEsp, SettingMaikon }
    private InputState currentState = InputState.Normal;
 //   private bool isInComSettings = false;
    private bool inputLocked = false;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadComSettings();
            InitializePorts();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (settingsButton == null)
            settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();

        if (espText == null)
            espText = GameObject.Find("ESPText")?.GetComponent<TMP_Text>();

        if (maikonText == null)
            maikonText = GameObject.Find("MaikonText")?.GetComponent<TMP_Text>();

        if (mainCanvas == null)
            mainCanvas = GameObject.Find("MainCanvas")?.GetComponent<Canvas>();

        if (comCanvas == null)
            comCanvas = GameObject.Find("ComCanvas")?.GetComponent<Canvas>();

        // 設定ボタンのみリスナー登録
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OpenComSettings);

        tempEspCom = espPortNumber;
        tempMaikonCom = maikonPortNumber;
        UpdateTexts();

        if (comCanvas != null)
            comCanvas.gameObject.SetActive(false);
    }


    void Update()
    {
        if (inputLocked) return;
        switch (currentState)
        {
            case InputState.Normal:
                if (Input.GetKeyDown(KeyCode.C))
                {
                    OpenComSettings();
                    Debug.Log("=== COM設定画面を開きました ===");
                    Debug.Log("Eを押してESP COM、Mを押してマイコン COMを変更");
                }

                if (comCanvas != null && comCanvas.gameObject.activeSelf)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        currentState = InputState.SettingEsp;
                        inputBuffer = "";
                        Debug.Log("ESP COM 設定: 番号を入力してEnterを押してください");
                    }
                    else if (Input.GetKeyDown(KeyCode.M))
                    {
                        currentState = InputState.SettingMaikon;
                        inputBuffer = "";
                        Debug.Log("マイコン COM 設定: 番号を入力してEnterを押してください");
                    }
                    else if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        SaveAndReturn();
                    }
                }
                break;

            case InputState.SettingEsp:
                ReadKeyboardInput("esp");
                break;

            case InputState.SettingMaikon:
                ReadKeyboardInput("maikon");
                break;
        }
    }

    // ==============================
    // 🔹 シリアル制御
    // ==============================
    public void InitializePorts()
    {
        CloseAll();

        try
        {
            string espPort = "COM" + espPortNumber;
            espSerial = new SerialPort(espPort, ESP_BAUD);
            espSerial.Encoding = System.Text.Encoding.UTF8;
            espSerial.ReadTimeout = 100;
            espSerial.Open();
            Debug.Log($"ESP に接続: {espPort}");
            if(espStatusText != null)
                espStatusText.text = "ESP Status: Connected";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ESPポート接続失敗: {e.Message}");
            if (espStatusText != null)
                espStatusText.text = "ESP Status: Not Connected";
        }

        try
        {
            string maikonPort = "COM" + maikonPortNumber;
            maikonSerial = new SerialPort(maikonPort, MAIKON_BAUD);
            maikonSerial.Open();
            Debug.Log($"マイコンに接続: {maikonPort}");
            if (maikonStatusText != null)
                maikonStatusText.text = "Microcontroller status: Connected";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"マイコンポート接続失敗: {e.Message}");
            if (maikonStatusText != null)
                maikonStatusText.text = "Microcontroller status: Not connected";
        }

        if (espSerial != null && espSerial.IsOpen)
        {
            keepReading = true;
            readThread = new Thread(ReadEspLoop);
            readThread.Start();
        }
    }

    void ReadEspLoop()
    {
        while (keepReading)
        {
            try
            {
                if (espSerial != null && espSerial.IsOpen)
                {
                    string message = espSerial.ReadLine().Trim();
                    if (message == "ATTACK")
                        attackTriggered = true;
                }
            }
            catch (TimeoutException) { }
            catch (Exception e)
            {
                Debug.LogError("ESP受信エラー: " + e.Message);
            }
        }
    }

    public void SendAttackToMaikon()
    {
        if (maikonSerial != null && maikonSerial.IsOpen)
        {
            try
            {
                maikonSerial.Write("1");
                Debug.Log("マイコンへ攻撃信号を送信 (1)");
            }
            catch
            {
                Debug.LogWarning("マイコン送信失敗");
            }
        }
    }

    public void ResetTrigger() => attackTriggered = false;

    void CloseAll()
    {
        try { if (espSerial != null && espSerial.IsOpen) espSerial.Close(); } catch { }
        try { if (maikonSerial != null && maikonSerial.IsOpen) maikonSerial.Close(); } catch { }
    }

    // ==============================
    // 🔹 UI 制御
    // ==============================
    void OpenComSettings()
    {
        if (mainCanvas != null) mainCanvas.gameObject.SetActive(false);
        if (comCanvas != null) comCanvas.gameObject.SetActive(true);
     //   isInComSettings = true;
        UpdateTexts();
    }

    void ReadKeyboardInput(string mode)
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsDigit(c))
            {
                inputBuffer += c;
                Debug.Log("入力: " + inputBuffer);
            }
            else if (c == '\b' && inputBuffer.Length > 0)
            {
                inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
            }
            else if (c == '\n' || c == '\r') // Enterキー
            {
                if (int.TryParse(inputBuffer, out int value) && value > 0 && value <= 100)
                {
                    if (mode == "esp")
                    {
                        tempEspCom = value;
                        Debug.Log($"✅ ESP COM 設定 = {value}");
                    }
                    else
                    {
                        tempMaikonCom = value;
                        Debug.Log($"✅ マイコン COM 設定 = {value}");
                    }

                    UpdateTexts();
                    SaveComSettings(tempEspCom, tempMaikonCom);

                    Debug.Log("✅ 保存完了 (ESCで戻る)");
                }
                else
                {
                    Debug.LogWarning("⚠️ 1–100 の数字を入力してください");
                }

                currentState = InputState.Normal;
                inputBuffer = "";
                break;
            }
        }
    }

    void SaveAndReturn()
    {
        SaveComSettings(tempEspCom, tempMaikonCom);

        if (comCanvas != null)
            comCanvas.gameObject.SetActive(false);

        if (mainCanvas != null)
            mainCanvas.gameObject.SetActive(true);

        currentState = InputState.Normal;
      //  isInComSettings = false;

        // 🧠 Enter重複防止のための遅延
        StartCoroutine(DelayResetInput());

        Debug.Log("ゲームに戻りました");
    }

    private System.Collections.IEnumerator DelayResetInput()
    {
        inputLocked = true;           // 入力ロック開始
        Input.ResetInputAxes();       // 押しっぱなしのボタンをリセット

        yield return new WaitForSeconds(0.3f); // 0.3秒待機

        Input.ResetInputAxes();       // 再度リセット
        inputLocked = false;          // 入力ロック解除
    }

    void UpdateTexts()
    {
        if (espText != null)
            espText.text = $"ESP COM: {tempEspCom}";
        if (maikonText != null)
            maikonText.text = $"Maikon COM: {tempMaikonCom}";
    }

    // ==============================
    // 🔹 設定の保存 / 読み込み
    // ==============================
    public void SaveComSettings(int espCom, int maikonCom)
    {
        espPortNumber = espCom;
        maikonPortNumber = maikonCom;
        PlayerPrefs.SetInt("ESP_COM", espCom);
        PlayerPrefs.SetInt("MAIKON_COM", maikonCom);
        PlayerPrefs.Save();
        InitializePorts();
    }

    public void LoadComSettings()
    {
        espPortNumber = PlayerPrefs.GetInt("ESP_COM", espPortNumber);
        maikonPortNumber = PlayerPrefs.GetInt("MAIKON_COM", maikonPortNumber);
    }

    void OnApplicationQuit()
    {
        keepReading = false;
        if (readThread != null && readThread.IsAlive) readThread.Join();
        CloseAll();
    }
}
