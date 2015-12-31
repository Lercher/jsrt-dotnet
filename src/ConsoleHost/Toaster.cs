using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleHost
{
    public class Toaster
    {
        public void StartToasting()
        {
            Timer t = null;
            t = new Timer((s) =>
            {
                OnToastCompleted();
                t.Dispose();
            }, null, 1500, 1500);
        }

        public event EventHandler ToastCompleted;
        protected virtual void OnToastCompleted()
        {
            var tc = ToastCompleted;
            if (tc != null)
            {
                tc(this, EventArgs.Empty);
            }
        }
    }
}
