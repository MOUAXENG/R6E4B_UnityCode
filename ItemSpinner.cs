using UnityEngine;

public class ItemSpinner : MonoBehaviour
{
    [Header("回転設定")]
    public float rotationSpeed = 180f;   // 回転速度（1秒あたりの角度）
    public float delayAfterFullRotation = 0.5f; // 1回転（360度）後の待機時間

    private float currentRotation = 0f;  // 現在回転した角度を追跡
    private bool isDelaying = false;     // 待機中かどうかを確認

    void Update()
    {
        // 待機中の場合 → 回転しない
        if (isDelaying) return;

        // X軸を中心に回転
        float rotationThisFrame = rotationSpeed * Time.deltaTime;
        transform.Rotate(rotationThisFrame, 0f, 0f);

        currentRotation += Mathf.Abs(rotationThisFrame);

        // 360度回転したら → 待機処理を開始
        if (currentRotation >= 180f)
        {
            currentRotation = 0f;
            rotationSpeed = -rotationSpeed;
            StartCoroutine(RotationDelay());
        }
    }

    private System.Collections.IEnumerator RotationDelay()
    {
        isDelaying = true;
        yield return new WaitForSeconds(delayAfterFullRotation); // 指定時間待機
        isDelaying = false;
    }
}
