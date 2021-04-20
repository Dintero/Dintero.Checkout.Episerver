using System.Collections.Generic;
using System.Threading;

namespace Dintero.Checkout.Episerver.Helpers
{
    internal static class LockHelper
    {
        private static readonly object lockObj = new object();

        private static readonly Dictionary<string, AutoResetEvent> lockDict = new Dictionary<string, AutoResetEvent>();

        public static void Lock(string orderNumber)
        {
            AutoResetEvent eventToWait;
            do
            {
                lock (lockObj)
                {
                    if (!lockDict.TryGetValue(orderNumber, out eventToWait))
                    {
                        lockDict.Add(orderNumber, new AutoResetEvent(false));
                    }
                }
            } while (eventToWait != null && eventToWait.WaitOne());
        }

        public static void Release(string orderNumber)
        {
            lock (lockObj)
            {
                AutoResetEvent evt;
                if (lockDict.TryGetValue(orderNumber, out evt))
                {
                    lockDict.Remove(orderNumber);
                    evt.Set();
                }
            }
        }
    }
}
