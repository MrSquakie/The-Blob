﻿using UnityEngine.Events;
namespace GameCreator.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    [AddComponentMenu("")]
    public class EventDispatchManager : Singleton<EventDispatchManager>
    {
        [Serializable] public class Dispatcher : UnityEvent<GameObject> { }

        // PROPERTIES: ----------------------------------------------------------------------------

        private Dictionary<string, Dispatcher> events = new Dictionary<string, Dispatcher>();

        // INITIALIZERS: --------------------------------------------------------------------------

        protected override void OnCreate()
        {
            base.OnCreate();
            this.events = new Dictionary<string, Dispatcher>();
        }

        // PUBLIC METHODS: ------------------------------------------------------------------------

        public void Dispatch(string name, GameObject invoker)
        {
            this.RequireInit(ref name);
            this.events[name].Invoke(invoker);
        }

        public void Subscribe(string name, UnityAction<GameObject> callback)
        {
            this.RequireInit(ref name);
            this.events[name].AddListener(callback);
        }

        public void Unsubscribe(string name, UnityAction<GameObject> callback)
        {
            this.RequireInit(ref name);
            this.events[name].RemoveListener(callback);
        }

        // PRIVATE METHODS: -----------------------------------------------------------------------

        private void RequireInit(ref string eventName)
        {
            eventName = eventName.Trim().Replace(" ", "-").ToLower();

            if (this.events.ContainsKey(eventName)) return;
            this.events.Add(eventName, new Dispatcher());
        }
    }
}