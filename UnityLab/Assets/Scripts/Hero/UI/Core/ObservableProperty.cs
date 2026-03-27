using System;

namespace Hero.UI.Core
{
    /// <summary>
    /// 값이 변경될 때 등록된 액션을 호출하는 Generic Wrapper 클래스 (MVVM 데이터 바인딩용)
    /// </summary>
    /// <typeparam name="T">데이터 타입</typeparam>
    public class ObservableProperty<T>
    {
        private T _value;

        public T Value
        {
            get => _value;
            set
            {
                if (Equals(_value, value)) return;
                _value = value;
                OnValueChanged?.Invoke(_value);
            }
        }

        public event Action<T> OnValueChanged;

        public ObservableProperty(T defaultValue = default)
        {
            _value = defaultValue;
        }

        /// <summary>
        /// 초기값과 함께 액션을 즉시 실행하며 구독
        /// </summary>
        public void Subscribe(Action<T> action)
        {
            OnValueChanged += action;
            action.Invoke(_value);
        }

        public void Unsubscribe(Action<T> action)
        {
            OnValueChanged -= action;
        }
    }
}
