﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleHost
{
    public class Toaster
    {
        public Toaster()
        {
            Console.WriteLine($"New {nameof(Toaster)} here");
        }

        public virtual void StartToasting()
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

    public class ToasterOven : Toaster
    {
        private int count_;
        private Timer t_;

        public ToasterOven()
        {
            Console.WriteLine($"New {nameof(ToasterOven)} here");
        }
        public override void StartToasting()
        {
            if (t_ != null)
                throw new Exception("Already toasting.");

            t_ = new Timer(ServiceTimer, null, 1000, 1000);
        }

        private bool servicing_ = false;
        private void ServiceTimer(object state)
        {
            if (servicing_)
                return;

            servicing_ = true;
            OnToastCompleted2();
            count_++;
            if (count_ % 10 == 0)
            {
                OnLoafToasted(new Loaf { PiecesCooked = 10 });
            }
            servicing_ = false;
        }

        public void StopToasting()
        {
            t_.Dispose();
            t_ = null;
        }

        public event EventHandler ToastCompleted2;
        protected virtual void OnToastCompleted2()
        {
            var tc = ToastCompleted2;
            if (tc != null)
            {
                tc(this, EventArgs.Empty);
            }
            OnToastCompleted(); // call the base class event
        }

        public event EventHandler<Loaf> LoafToasted;
        protected virtual void OnLoafToasted(Loaf loaf)
        {
            var eh = LoafToasted;
            if (eh != null)
                eh(this, loaf);
        }
    }

    public class Loaf
    {
        public Loaf()
        {
            Console.WriteLine($"New {nameof(Loaf)} here");
        }

        public int PiecesCooked { get; set; }
    }
}
