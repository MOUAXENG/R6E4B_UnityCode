using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Minifantasy
{
    public class KillManager : MonoBehaviour
    {
        public static KillManager Instance;

        public int TotalKills { get; private set; } = 0;

        private void Awake()
        {
            // ทำให้วัตถุนี้ไม่ถูกทำลายเมื่อเปลี่ยน Scene
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void AddKill()
        {
            TotalKills++;
        }

        public void ResetKills()
        {
            TotalKills = 0;
        }
    }
}
