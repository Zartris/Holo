using System;

namespace MediaFrameQrProcessing.Wrappers
{
    public class Logger
    {
        public Logger()
        {
        }

        public void log(String m)
        {
            UnityEngine.Debug.Log(m);            
        }
    }
}