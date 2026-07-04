using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CircleWar
{
    public static class BindableUIExtensions
    {
        public static IUnRegister BindText<T>(this Text target, IReadonlyBindableProperty<T> property, Func<T, string> formatter = null)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            return property
                .RegisterWithInitValue(value => target.text = FormatText(value, formatter))
                .UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindToText<T>(this IReadonlyBindableProperty<T> property, Text target, Func<T, string> formatter = null)
        {
            return target.BindText(property, formatter);
        }

        public static IUnRegister BindText(this InputField target, IBindableProperty<string> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            CompositeUnRegister unRegister = new CompositeUnRegister();
            unRegister.Add(property.RegisterWithInitValue(value => target.SetTextWithoutNotify(value ?? string.Empty)));

            UnityAction<string> onValueChanged = value => property.Value = value;
            target.onValueChanged.AddListener(onValueChanged);
            unRegister.Add(new CustomUnRegister(() => target.onValueChanged.RemoveListener(onValueChanged)));

            return unRegister.UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindValue(this Slider target, IBindableProperty<float> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            CompositeUnRegister unRegister = new CompositeUnRegister();
            unRegister.Add(property.RegisterWithInitValue(target.SetValueWithoutNotify));

            UnityAction<float> onValueChanged = value => property.Value = value;
            target.onValueChanged.AddListener(onValueChanged);
            unRegister.Add(new CustomUnRegister(() => target.onValueChanged.RemoveListener(onValueChanged)));

            return unRegister.UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindValue(this Slider target, IBindableProperty<int> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            target.wholeNumbers = true;
            CompositeUnRegister unRegister = new CompositeUnRegister();
            unRegister.Add(property.RegisterWithInitValue(value => target.SetValueWithoutNotify(value)));

            UnityAction<float> onValueChanged = value => property.Value = Mathf.RoundToInt(value);
            target.onValueChanged.AddListener(onValueChanged);
            unRegister.Add(new CustomUnRegister(() => target.onValueChanged.RemoveListener(onValueChanged)));

            return unRegister.UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindValue(this Scrollbar target, IBindableProperty<float> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            CompositeUnRegister unRegister = new CompositeUnRegister();
            unRegister.Add(property.RegisterWithInitValue(target.SetValueWithoutNotify));

            UnityAction<float> onValueChanged = value => property.Value = value;
            target.onValueChanged.AddListener(onValueChanged);
            unRegister.Add(new CustomUnRegister(() => target.onValueChanged.RemoveListener(onValueChanged)));

            return unRegister.UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindIsOn(this Toggle target, IBindableProperty<bool> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            CompositeUnRegister unRegister = new CompositeUnRegister();
            unRegister.Add(property.RegisterWithInitValue(target.SetIsOnWithoutNotify));

            UnityAction<bool> onValueChanged = value => property.Value = value;
            target.onValueChanged.AddListener(onValueChanged);
            unRegister.Add(new CustomUnRegister(() => target.onValueChanged.RemoveListener(onValueChanged)));

            return unRegister.UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindInteractable(this Selectable target, IReadonlyBindableProperty<bool> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            return property
                .RegisterWithInitValue(value => target.interactable = value)
                .UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindFillAmount(this Image target, IReadonlyBindableProperty<float> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            return property
                .RegisterWithInitValue(value => target.fillAmount = Mathf.Clamp01(value))
                .UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindFillAmount(this Image target, IReadonlyBindableProperty<int> property, int maxValue = 100)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            int safeMaxValue = Mathf.Max(1, maxValue);
            return property
                .RegisterWithInitValue(value => target.fillAmount = Mathf.Clamp01((float)value / safeMaxValue))
                .UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindActive(this GameObject target, IReadonlyBindableProperty<bool> property)
        {
            Require(target, nameof(target));
            Require(property, nameof(property));

            return property
                .RegisterWithInitValue(target.SetActive)
                .UnRegisterWhenGameObjectDestroyed(target);
        }

        public static IUnRegister BindActive(this Component target, IReadonlyBindableProperty<bool> property)
        {
            Require(target, nameof(target));
            return target.gameObject.BindActive(property);
        }

        private static string FormatText<T>(T value, Func<T, string> formatter)
        {
            if (formatter != null)
            {
                return formatter.Invoke(value) ?? string.Empty;
            }

            return value == null ? string.Empty : value.ToString();
        }

        private static void Require(object value, string parameterName)
        {
            if (value == null)
            {
                throw new ArgumentNullException(parameterName);
            }
        }
    }
}
