#region License
/* Copyright 2012 James F. Bellinger <http://www.zer7.com>

   Permission to use, copy, modify, and/or distribute this software for any
   purpose with or without fee is hereby granted, provided that the above
   copyright notice and this permission notice appear in all copies.

   THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
   WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
   MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
   ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
   WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
   ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
   OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE. */
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace HidSharp.Platform
{
    abstract class HidManager
    {
        Dictionary<object, HidDevice> _deviceList;
        object _syncRoot;

        protected HidManager()
        {
            _deviceList = new Dictionary<object, HidDevice>();
            _syncRoot = new object();
        }

        public virtual void Init()
        {

        }

        public virtual void Run()
        {
            while (true) { Thread.Sleep(Timeout.Infinite); }
        }

        internal void RunImpl(object readyEvent)
        {
            Init();
            ((ManualResetEvent)readyEvent).Set();
            Run();
        }

        public IEnumerable<HidDevice> GetDevices()
        {
            lock (SyncRoot)
            {
                object[] devices = Refresh();
                object[] additions = devices.Except(_deviceList.Keys).ToArray();
                object[] removals = _deviceList.Keys.Except(devices).ToArray();

                foreach (object addition in additions)
                {
                    HidDevice device;
                    if (TryCreateDevice(addition, out device))
                    {
                        // By not adding on failure, we'll end up retrying every time.
                        _deviceList.Add(addition, device);
                    }
                }

                foreach (object removal in removals)
                {
                    _deviceList.Remove(removal);
                }

                return _deviceList.Values.ToArray();
            }
        }

        protected abstract object[] Refresh();

        protected abstract bool TryCreateDevice(object key, out HidDevice device);

        public abstract bool IsSupported
        {
            get;
        }

        protected object SyncRoot
        {
            get { return _syncRoot; }
        }
    }
}
