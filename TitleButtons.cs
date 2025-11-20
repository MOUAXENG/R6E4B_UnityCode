using UnityEngine;
using UnityEngine.SceneManagement;

public class TieleButtons : MonoBehaviour
{
    [Header("設定")]
    public string stageName = "stage 1";           // 通常のスタート時にロードするシーン名
    public string secretStageName = "SecretStage"; // 「ricchy」入力時にロードするシーン名
    public float inputCooldown = 0.3f;             // 入力のクールダウン（連続押下防止）
    private float lastInputTime = -1f;

    // 「ricchy」判定用
    private string secretCode = "ricchy";
    private string currentInput = "";

    void Update()
    {
        // 連続入力を防止
        if (Time.unscaledTime - lastInputTime < inputCooldown)
            return;

        // ------------------------
        // シークレットコード判定
        // ------------------------
        foreach (char c in Input.inputString)
        {
            currentInput += c;

            // 長すぎたら古い部分を削除
            if (currentInput.Length > secretCode.Length)
                currentInput = currentInput.Substring(currentInput.Length - secretCode.Length);

            // 一致チェック
            if (currentInput.ToLower() == secretCode.ToLower())
            {
                Debug.Log("シークレットコード発動！SecretStageへ移動");
                SceneManager.LoadScene(secretStageName);
            }
        }

        // ------------------------
        // 通常操作
        // ------------------------
        if (Input.GetKeyDown(KeyCode.LeftShift) ||
     Input.GetKeyDown(KeyCode.RightShift) ||
     Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            lastInputTime = Time.unscaledTime;
            StartBtn();
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
        {
            lastInputTime = Time.unscaledTime;
            QuitBtn();
        }
    }

    // -----------------------
    // スタートボタン
    // -----------------------
    public void StartBtn()
    {
        SceneManager.LoadScene(stageName);
    }

    // -----------------------
    // 終了ボタン
    // -----------------------
    public void QuitBtn()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // エディタ上で停止
#else
        Application.Quit(); // 実際にゲームを終了
#endif
    }
}
