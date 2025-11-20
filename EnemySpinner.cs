using UnityEngine;

public class EnemySpinner : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // ความเร็วในการหมุน (องศาต่อวินาที)

    void Update()
    {
        // หมุนรอบแกน Z
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
