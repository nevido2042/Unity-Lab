using UnityEngine;
using TMPro;
using Hero.UI.Troop;

namespace Hero.UI.Troop
{
    /// <summary>
    /// 병력 수 UI를 담당하는 View 컴포넌트 (MonoBehaviour)
    /// </summary>
    public class TroopView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _playerTroopText;
        [SerializeField] private TextMeshProUGUI _enemyTroopText;

        private TroopViewModel _viewModel;

        private void Awake()
        {
            _viewModel = new TroopViewModel();
        }

        private void Start()
        {
            _viewModel.Initialize();

            // ViewModel의 ObservableProperty 구독
            _viewModel.PlayerTroopCount.Subscribe(UpdatePlayerTroopUI);
            _viewModel.EnemyTroopCount.Subscribe(UpdateEnemyTroopUI);
        }

        private void OnDestroy()
        {
            if (_viewModel != null)
            {
                _viewModel.PlayerTroopCount.Unsubscribe(UpdatePlayerTroopUI);
                _viewModel.EnemyTroopCount.Unsubscribe(UpdateEnemyTroopUI);
                _viewModel.Dispose();
            }
        }

        private void UpdatePlayerTroopUI(int count)
        {
            if (_playerTroopText != null)
            {
                _playerTroopText.text = $"Player: {count}";
            }
        }

        private void UpdateEnemyTroopUI(int count)
        {
            if (_enemyTroopText != null)
            {
                _enemyTroopText.text = $"Enemy: {count}";
            }
        }
    }
}
