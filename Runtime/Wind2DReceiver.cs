using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cloth2D
{
    public class Wind2DReceiver
    {
        public List<Wind2D> Winds { get; private set; }

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
            Winds = new List<Wind2D>();
        }

        public void RegisterWind(Wind2D wind)
        {
            Winds.Add(wind);
        }

        public void UnRegisterWind(Wind2D wind)
        {
            Winds.Remove(wind);
        }

        public void ClearWinds()
        {
            Winds.Clear();
        }
    }
    
}
