using System;
using System.Collections.Generic;

namespace CircleWar
{
    public interface IEasyEvent
    {
        IUnRegister Register(Action onEvent);
    }

    public interface IReadonlyBindableProperty
    {
        object BoxedValue { get; }
    }

    public interface IBindableProperty : IReadonlyBindableProperty
    {
        new object BoxedValue { get; set; }
    }

    public interface IReadonlyBindableProperty<T> : IReadonlyBindableProperty, IEasyEvent
    {
        T Value { get; }
        IUnRegister Register(Action<T> onValueChanged);
        IUnRegister RegisterWithInitValue(Action<T> onValueChanged);
        void UnRegister(Action<T> onValueChanged);
    }

    public interface IBindableProperty<T> : IReadonlyBindableProperty<T>, IBindableProperty
    {
        new T Value { get; set; }
        void SetValueWithoutEvent(T newValue);
    }

    public class BindableProperty<T> : IBindableProperty<T>
    {
        public static Func<T, T, bool> Comparer = EqualityComparer<T>.Default.Equals;

        protected T mValue;
        private Action<T> onValueChanged = delegate { };

        public BindableProperty(T defaultValue = default)
        {
            mValue = defaultValue;
        }

        public T Value
        {
            get => GetValue();
            set
            {
                T oldValue = GetValue();
                if (IsEqual(value, oldValue))
                {
                    return;
                }

                SetValue(value);
                onValueChanged.Invoke(value);
            }
        }

        object IReadonlyBindableProperty.BoxedValue => Value;

        object IBindableProperty.BoxedValue
        {
            get => Value;
            set => Value = value == null ? default : (T)value;
        }

        public BindableProperty<T> WithComparer(Func<T, T, bool> comparer)
        {
            Comparer = comparer ?? EqualityComparer<T>.Default.Equals;
            return this;
        }

        public IUnRegister Register(Action<T> onValueChanged)
        {
            if (onValueChanged == null)
            {
                throw new ArgumentNullException(nameof(onValueChanged));
            }

            this.onValueChanged += onValueChanged;
            return new CustomUnRegister(() => UnRegister(onValueChanged));
        }

        public IUnRegister RegisterWithInitValue(Action<T> onValueChanged)
        {
            if (onValueChanged == null)
            {
                throw new ArgumentNullException(nameof(onValueChanged));
            }

            onValueChanged.Invoke(Value);
            return Register(onValueChanged);
        }

        public void UnRegister(Action<T> onValueChanged)
        {
            if (onValueChanged == null)
            {
                return;
            }

            this.onValueChanged -= onValueChanged;
        }

        public void SetValueWithoutEvent(T newValue)
        {
            SetValue(newValue);
        }

        public override string ToString()
        {
            T value = Value;
            return value == null ? string.Empty : value.ToString();
        }

        protected virtual void SetValue(T newValue)
        {
            mValue = newValue;
        }

        protected virtual T GetValue()
        {
            return mValue;
        }

        IUnRegister IEasyEvent.Register(Action onEvent)
        {
            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            return Register(_ => onEvent.Invoke());
        }

        private static bool IsEqual(T left, T right)
        {
            Func<T, T, bool> comparer = Comparer;
            return comparer == null ? EqualityComparer<T>.Default.Equals(left, right) : comparer(left, right);
        }
    }
}
