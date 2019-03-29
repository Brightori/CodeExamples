using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

public delegate void EventHandler<T>(T e);

namespace GlobalEventAggregator
{
    public class EventContainer<T> : IDebugable
    {
        private event EventHandler<T> _eventKeeper;
        private readonly Dictionary<WeakReference, EventHandler<T>> _activeListenersOfThisType = new Dictionary<WeakReference, EventHandler<T>>();
        private const string Error = "null";

        public bool HasDuplicates(object listener)
        {
            return _activeListenersOfThisType.Keys.Any(k => k.Target == listener);
        }

        public void AddToEvent(object listener, EventHandler<T> action)
        {
            var newAction = new WeakReference(listener);
            _activeListenersOfThisType.Add(newAction, action);
            _eventKeeper += _activeListenersOfThisType[newAction];
        }

        public void RemoveFromEvent(object listener)
        {
            var currentEvent = _activeListenersOfThisType.Keys.FirstOrDefault(k => k.Target == listener);
            if (currentEvent != null)
            {
                _eventKeeper -= _activeListenersOfThisType[currentEvent];
                _activeListenersOfThisType.Remove(currentEvent);
            }
        }

        public EventContainer(object listener, EventHandler<T> action)
        {
            _eventKeeper += action;
            _activeListenersOfThisType.Add(new WeakReference(listener), action);
        }

        public void Invoke(T t)
        {
            if (_activeListenersOfThisType.Keys.Any(k => k.Target.ToString() == Error))
            {
                var failObjList = _activeListenersOfThisType.Keys.Where(k => k.Target.ToString() == Error).ToList();
                foreach (var fail in failObjList)
                {
                    _eventKeeper -= _activeListenersOfThisType[fail];
                    _activeListenersOfThisType.Remove(fail);
                }
            }

            if (_eventKeeper != null)
                _eventKeeper(t);
            return;
        }

        public string DebugInfo()
        {
            string info = string.Empty;
            foreach (var c in _activeListenersOfThisType.Keys)
            {
                info += c.Target.ToString() + "\n";
            }
            return info;
        }
    }

    public static class EventAggregator
    {
        private static Dictionary<Type, object> GlobalListeners = new Dictionary<Type, object>();

        static EventAggregator()
        {
            SceneManager.sceneUnloaded += ClearGlobalListeners;
        }

        private static void ClearGlobalListeners(Scene scene)
        {
            GlobalListeners.Clear();
        }

        public static void AddListener<T>(object listener, Action<T> action)
        {
            var key = typeof(T);
            EventHandler<T> handler = new EventHandler<T>(action);

            if (GlobalListeners.ContainsKey(key))
            {
                var lr = (EventContainer<T>)GlobalListeners[key];
                if (lr.HasDuplicates(listener))
                    return;
                lr.AddToEvent(listener, handler);
                return;
            }
            GlobalListeners.Add(key, new EventContainer<T>(listener, handler));
        }

        public static void Invoke<T>(T data)
        {
            var key = typeof(T);
            if (!GlobalListeners.ContainsKey(key))
                return;
            var eventContainer = (EventContainer<T>)GlobalListeners[key];
            eventContainer.Invoke(data);
        }

        public static void RemoveListener<T>(object listener)
        {
            var key = typeof(T);
            if (GlobalListeners.ContainsKey(key))
            {
                var eventContainer = (EventContainer<T>)GlobalListeners[key];
                eventContainer.RemoveFromEvent(listener);
            }
        }

        public static string DebugInfo()
        {
            string info = string.Empty;

            foreach (var listener in GlobalListeners)
            {
                info += "тип на который подписаны объекты " +  listener.Key.ToString() + "\n";
                var t = (IDebugable)listener.Value;
                info += t.DebugInfo() + "\n";
            }

            return info;
        }
    }

    public interface IDebugable
    {
        string DebugInfo();
    }
}
