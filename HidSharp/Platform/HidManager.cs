#region License
/* Copyright 2012-2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
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
        enum KeyType
        {
            Hid,
            Serial
        }
        struct TypedKey : IEquatable<TypedKey>
        {
            public override bool Equals(object obj)
            {
                return obj is TypedKey && Equals((TypedKey)obj);
            }

            public bool Equals(TypedKey other)
            {
                return Type == other.Type && object.Equals(Key, other.Key);
            }

            public override int GetHashCode()
            {
                return Key.GetHashCode();
            }

            public KeyType Type
            {
                get;
                set;
            }

            public object Key
            {
                get;
                set;
            }
        }
        Dictionary<TypedKey, Device> _deviceList;
        object _getDevicesLock;

        protected HidManager()
        {
            _deviceList = new Dictionary<TypedKey, Device>();
            _getDevicesLock = new object();
        }

        internal void InitializeEventManager()
        {
            EventManager = CreateEventManager();
            EventManager.Start();
        }

        protected virtual SystemEvents.EventManager CreateEventManager()
        {
            return new SystemEvents.DefaultEventManager();
        }

        protected virtual void Run(Action readyCallback)
        {
            readyCallback();
        }

        internal void RunImpl(object readyEvent)
        {
            Run(() => ((ManualResetEvent)readyEvent).Set());
        }

        protected static void RunAssert(bool condition, string error)
        {
            if (!condition) { throw new InvalidOperationException(error); }
        }

        public IEnumerable<Device> GetDevices()
        {
            Device[] deviceList;
            
            lock (_getDevicesLock)
            {
                TypedKey[] devices = GetAllDeviceKeys();
                TypedKey[] additions = devices.Except(_deviceList.Keys).ToArray();
                TypedKey[] removals = _deviceList.Keys.Except(devices).ToArray();

                if (additions.Length > 0)
                {
                    int completedAdditions = 0;

                    foreach (TypedKey addition in additions)
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(addition_ =>
                            {
                                var typedKey = (TypedKey)addition_;

                                Device device = null; bool created;

                                switch (typedKey.Type)
                                {
                                    case KeyType.Hid:
                                        created = TryCreateHidDevice(typedKey.Key, out device);
                                        break;

                                    case KeyType.Serial:
                                        created = TryCreateSerialDevice(typedKey.Key, out device);
                                        break;

                                    default:
                                        created = false; Debug.Assert(false);
                                        break;
                                }

                                if (created)
                                {
                                    // By not adding on failure, we'll end up retrying every time.
                                    lock (_deviceList)
                                    {
                                        _deviceList.Add(typedKey, device);
                                        Debug.Print("** HIDSharp detected a new device: {0}", typedKey.Key);
                                    }
                                }

                                lock (_deviceList)
                                {
                                    completedAdditions++; Monitor.Pulse(_deviceList);
                                }
                            }), addition);
                    }

                    lock (_deviceList)
                    {
                        while (completedAdditions != additions.Length) { Monitor.Wait(_deviceList); }
                    }
                }

                foreach (TypedKey removal in removals)
                {
                    _deviceList.Remove(removal);
                    Debug.Print("** HIDSharp detected a device removal: {0}", removal.Key);
                }
                deviceList = _deviceList.Values.ToArray();
            }

            return deviceList;
        }

        TypedKey[] GetAllDeviceKeys()
        {
            return GetHidDeviceKeys().Select(key => new TypedKey() { Key = key, Type = KeyType.Hid })
                .Concat(GetSerialDeviceKeys().Select(key => new TypedKey() { Key = key, Type = KeyType.Serial }))
                .ToArray();
        }

        protected abstract object[] GetHidDeviceKeys();

        protected abstract object[] GetSerialDeviceKeys();

        protected abstract bool TryCreateHidDevice(object key, out Device device);

        protected abstract bool TryCreateSerialDevice(object key, out Device device);

        public virtual bool AreDriversBeingInstalled
        {
            get { return false; }
        }

        public SystemEvents.EventManager EventManager
        {
            get;
            private set;
        }

        public abstract string FriendlyName
        {
            get;
        }

        public abstract bool IsSupported
        {
            get;
        }
    }
}
