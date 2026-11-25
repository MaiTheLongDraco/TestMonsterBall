using System;
using UnityEngine;

namespace Aiara.Combat
{
    public class MonsterBallHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private int _maxHp = 200;
        [SerializeField] private int _currentHp = 200;

        public int CurrentHp => _currentHp;
        public int MaxHp => _maxHp;
        public bool IsDead => _currentHp <= 0;

        public event Action<float, float> OnHealthChanged;
        public event Action<int> OnDamagedEvent;
        public event Action OnDeath;
        public event Action OnDeathEvent;

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        public void TakeDamage(int damage)
        {
            if (IsDead) return;

            _currentHp = Mathf.Max(0, _currentHp - damage);
            Debug.Log($"[MonsterBallHealth] Took {damage} damage! Remaining HP: {_currentHp}/{_maxHp}");
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
            OnDamagedEvent?.Invoke(damage);

            if (_currentHp <= 0)
            {
                Die();
            }
        }

        public void ResetHealth()
        {
            _currentHp = _maxHp;
            OnHealthChanged?.Invoke(_currentHp, _maxHp);
        }

        private void Die()
        {
            Debug.Log("[MonsterBallHealth] MonsterBall defeated!");
            OnDeath?.Invoke();
            OnDeathEvent?.Invoke();
        }
    }
}
