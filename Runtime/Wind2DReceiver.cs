using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cloth2D
{
    public class Wind2DReceiver
    {
        public Dictionary<int, Wind2D> Winds { get; private set; }

        private static class InnerWind2DReceiverInstance
        {
            public static Wind2DReceiver instance = new Wind2DReceiver();
        }

        public static Wind2DReceiver GetInstance()
        {
            return InnerWind2DReceiverInstance.instance;
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

        public void UnRegisterWind(int guid)
        {
            Winds.Remove(guid);
        }

        public void ClearWinds()
        {
            Winds.Clear();
        }
    }
    
}
