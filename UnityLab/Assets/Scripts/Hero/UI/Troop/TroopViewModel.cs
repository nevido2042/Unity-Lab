using Hero.Combat.Manager;
using Hero.UI.Core;
using UnityEngine;

namespace Hero.UI.Troop
{
    /// <summary>
    /// 병력 수 데이터를 BattleManager에서 가져와 UI용 데이터로 변환하는 ViewModel
    /// </summary>
    public class TroopViewModel
    {
        public ObservableProperty<int> PlayerTroopCount { get; } = new ObservableProperty<int>();
        public ObservableProperty<int> EnemyTroopCount { get; } = new ObservableProperty<int>();

        public void Initialize()
        {
            if (BattleManager.Instance != null)
            {
                // 초기값 설정
                PlayerTroopCount.Value = BattleManager.Instance.PlayerTroopCount;
                EnemyTroopCount.Value = BattleManager.Instance.EnemyTroopCount;

                // 데이터 변경 이벤트 구독
                BattleManager.Instance.OnTroopCountChanged += UpdateTroopCount;
            }
        }

        public void Dispose()
        {
            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnTroopCountChanged -= UpdateTroopCount;
            }
        }

        private void UpdateTroopCount(int teamId, int newCount)
        {
            if (teamId == 0)
            {
                PlayerTroopCount.Value = newCount;
            }
            else if (teamId == 1)
            {
                EnemyTroopCount.Value = newCount;
            }
        }
    }
}
