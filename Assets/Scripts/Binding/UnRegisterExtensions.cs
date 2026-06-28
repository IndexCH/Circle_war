using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircleWar
{
    public interface IUnRegister
    {
        void UnRegister();
    }

    public sealed class CustomUnRegister : IUnRegister
    {
        private Action onUnRegister;
        private bool isUnregistered;

        public CustomUnRegister(Action onUnRegister)
        {
            this.onUnRegister = onUnRegister ?? throw new ArgumentNullException(nameof(onUnRegister));
        }

        public void UnRegister()
        {
            if (isUnregistered)
            {
                return;
            }

            isUnregistered = true;
            onUnRegister.Invoke();
            onUnRegister = null;
        }
    }

    public sealed class CompositeUnRegister : IUnRegister
    {
        private readonly List<IUnRegister> unRegisters = new List<IUnRegister>();
        private bool isUnregistered;

        public CompositeUnRegister(params IUnRegister[] unRegisters)
        {
            if (unRegisters == null)
            {
                return;
            }

            for (int index = 0; index < unRegisters.Length; index++)
            {
                Add(unRegisters[index]);
            }
        }

        public CompositeUnRegister Add(IUnRegister unRegister)
        {
            if (unRegister == null)
            {
                return this;
            }

            if (isUnregistered)
            {
                unRegister.UnRegister();
                return this;
            }

            unRegisters.Add(unRegister);
            return this;
        }

        public void UnRegister()
        {
            if (isUnregistered)
            {
                return;
            }

            isUnregistered = true;
            for (int index = unRegisters.Count - 1; index >= 0; index--)
            {
                unRegisters[index].UnRegister();
            }

            unRegisters.Clear();
        }
    }

    public interface IUnRegisterList
    {
        List<IUnRegister> UnregisterList { get; }
    }

    public static class UnRegisterListExtensions
    {
        public static void AddToUnregisterList(this IUnRegister self, IUnRegisterList unRegisterList)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (unRegisterList == null)
            {
                throw new ArgumentNullException(nameof(unRegisterList));
            }

            unRegisterList.UnregisterList.Add(self);
        }

        public static void UnRegisterAll(this IUnRegisterList self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            for (int index = self.UnregisterList.Count - 1; index >= 0; index--)
            {
                self.UnregisterList[index].UnRegister();
            }

            self.UnregisterList.Clear();
        }
    }

    public abstract class UnRegisterTrigger : MonoBehaviour
    {
        private readonly HashSet<IUnRegister> unRegisters = new HashSet<IUnRegister>();

        public IUnRegister AddUnRegister(IUnRegister unRegister)
        {
            if (unRegister == null)
            {
                throw new ArgumentNullException(nameof(unRegister));
            }

            unRegisters.Add(unRegister);
            return unRegister;
        }

        public void RemoveUnRegister(IUnRegister unRegister)
        {
            if (unRegister == null)
            {
                return;
            }

            unRegisters.Remove(unRegister);
        }

        protected void UnRegister()
        {
            foreach (IUnRegister unRegister in unRegisters)
            {
                unRegister.UnRegister();
            }

            unRegisters.Clear();
        }
    }

    public sealed class UnRegisterOnDestroyTrigger : UnRegisterTrigger
    {
        private void OnDestroy()
        {
            UnRegister();
        }
    }

    public sealed class UnRegisterOnDisableTrigger : UnRegisterTrigger
    {
        private void OnDisable()
        {
            UnRegister();
        }
    }

    public sealed class UnRegisterCurrentSceneUnloadedTrigger : UnRegisterTrigger
    {
        private static UnRegisterCurrentSceneUnloadedTrigger instance;

        public static UnRegisterCurrentSceneUnloadedTrigger Get
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                GameObject triggerObject = new GameObject(nameof(UnRegisterCurrentSceneUnloadedTrigger));
                triggerObject.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(triggerObject);
                instance = triggerObject.AddComponent<UnRegisterCurrentSceneUnloadedTrigger>();
                return instance;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            UnRegister();
        }
    }

    public static class UnRegisterExtensions
    {
        public static IUnRegister UnRegisterWhenGameObjectDestroyed(this IUnRegister self, Component component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            return self.UnRegisterWhenGameObjectDestroyed(component.gameObject);
        }

        public static IUnRegister UnRegisterWhenGameObjectDestroyed(this IUnRegister self, GameObject gameObject)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            return GetOrAddComponent<UnRegisterOnDestroyTrigger>(gameObject).AddUnRegister(self);
        }

        public static IUnRegister UnRegisterWhenDisabled(this IUnRegister self, Component component)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            return self.UnRegisterWhenDisabled(component.gameObject);
        }

        public static IUnRegister UnRegisterWhenDisabled(this IUnRegister self, GameObject gameObject)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            if (gameObject == null)
            {
                throw new ArgumentNullException(nameof(gameObject));
            }

            return GetOrAddComponent<UnRegisterOnDisableTrigger>(gameObject).AddUnRegister(self);
        }

        public static IUnRegister UnRegisterWhenCurrentSceneUnloaded(this IUnRegister self)
        {
            if (self == null)
            {
                throw new ArgumentNullException(nameof(self));
            }

            return UnRegisterCurrentSceneUnloadedTrigger.Get.AddUnRegister(self);
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component == null ? gameObject.AddComponent<T>() : component;
        }
    }
}
