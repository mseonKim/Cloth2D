using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cloth2D
{
    public class Wind2DReceiver
    {
        public Dictionary<int, Wind2D> Winds { get; private set; }

        private static readonly Lazy<Wind2DReceiver> instance = new Lazy<Wind2DReceiver>(() => new Wind2DReceiver());
        public static Wind2DReceiver GetInstance()
        {
            return instance.Value;
        }

        private Wind2DReceiver()
        {
            Winds = new Dictionary<int, Wind2D>();
        }

        public void RegisterWind(Wind2D wind)
        {
            int guid = wind.gameObject.GetInstanceID();
            if (!Winds.ContainsKey(guid))
            {
                Winds.Add(wind.gameObject.GetInstanceID(), wind);
            }
        }

        public void UnregisterWind(int guid)
        {
            Winds.Remove(guid);
        }

        public void ClearWinds()
        {
            Winds.Clear();
        }
    }
    
}
