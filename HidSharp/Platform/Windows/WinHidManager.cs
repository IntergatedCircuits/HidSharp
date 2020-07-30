#region License
/* Copyright 2012-2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Windows
{
    sealed class WinHidManager : HidManager
    {
        class DevicePathBase
        {
            public override bool Equals(object obj)
            {
                var path = obj as DevicePathBase;
                return path != null && DevicePath == path.DevicePath && DeviceID == path.DeviceID;
            }

            public override int GetHashCode()
            {
                return DevicePath.GetHashCode();
            }

            public override string ToString()
            {
                return DevicePath;
            }

            public string DevicePath;
            public string DeviceID;
        }

        sealed class HidDevicePath : DevicePathBase
        {

        }

        sealed class SerialDevicePath : DevicePathBase
        {
            public override bool Equals(object obj)
            {
                var path = obj as SerialDevicePath;
                return path != null && FileSystemName == path.FileSystemName && FriendlyName == path.FriendlyName;
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public string FileSystemName;
            public string FriendlyName;
        }

        protected override void Run(Action readyCallback)
        {
            const string className = "HidSharpDeviceMonitor";

            NativeMethods.WindowProc windowProc = DeviceMonitorWindowProc; GC.KeepAlive(windowProc);
            var wc = new NativeMethods.WNDCLASS() { ClassName = className, WindowProc = windowProc };
            RunAssert(0 != NativeMethods.RegisterClass(ref wc), "HidSharp RegisterClass failed.");

            var hwnd = NativeMethods.CreateWindowEx(0, className, className, 0,
                                                    NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT,
                                                    NativeMethods.HWND_MESSAGE,
                                                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            RunAssert(hwnd != IntPtr.Zero, "HidSharp CreateWindow failed.");

            var notifyFilter = new NativeMethods.DEV_BROADCAST_DEVICEINTERFACE()
            {
                Size = Marshal.SizeOf(typeof(NativeMethods.DEV_BROADCAST_DEVICEINTERFACE)),
                ClassGuid = NativeMethods.HidD_GetHidGuid(),
                DeviceType = NativeMethods.DBT_DEVTYP_DEVICEINTERFACE
            };
            var notifyHandle = NativeMethods.RegisterDeviceNotification(hwnd, ref notifyFilter, 0);
            RunAssert(notifyHandle != IntPtr.Zero, "HidSharp RegisterDeviceNotification failed.");

            readyCallback();

            NativeMethods.MSG msg;
            while (true)
            {
                int result = NativeMethods.GetMessage(out msg, hwnd, 0, 0);
                if (result == 0 || result == -1) { break; }

                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }

            RunAssert(NativeMethods.UnregisterDeviceNotification(notifyHandle), "HidSharp UnregisterDeviceNotification failed.");
            RunAssert(NativeMethods.DestroyWindow(hwnd), "HidSharp DestroyWindow failed.");
            RunAssert(NativeMethods.UnregisterClass(className, IntPtr.Zero), "HidSharp UnregisterClass failed.");
        }

        static IntPtr DeviceMonitorWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (message == NativeMethods.WM_DEVICECHANGE)
            {
                DeviceList.Local.RaiseChanged();
                return (IntPtr)1;
            }

            return NativeMethods.DefWindowProc(window, message, wParam, lParam);
        }

        delegate void EnumerateDevicesCallback(NativeMethods.HDEVINFO deviceInfoSet,
                                               NativeMethods.SP_DEVINFO_DATA deviceInfoData,
                                               NativeMethods.SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
                                               string deviceID, NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA didetail);
        static void EnumerateDevices(Guid guid, EnumerateDevicesCallback callback)
        {
            NativeMethods.HDEVINFO devInfo = NativeMethods.SetupDiGetClassDevs(
                guid, null, IntPtr.Zero,
                NativeMethods.DIGCF.DeviceInterface | NativeMethods.DIGCF.Present
                );

            if (devInfo.IsValid)
            {
                try
                {
                    NativeMethods.SP_DEVINFO_DATA dvi = new NativeMethods.SP_DEVINFO_DATA();
                    dvi.Size = Marshal.SizeOf(dvi);

                    for (int j = 0; NativeMethods.SetupDiEnumDeviceInfo(devInfo, j, ref dvi); j++)
                    {
                        string deviceID;
                        if (0 != NativeMethods.CM_Get_Device_ID(dvi.DevInst, out deviceID)) { continue; }

                        NativeMethods.SP_DEVICE_INTERFACE_DATA did = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                        did.Size = Marshal.SizeOf(did);

                        for (int i = 0; NativeMethods.SetupDiEnumDeviceInterfaces(devInfo, ref dvi, guid, i, ref did); i++)
                        {
                            NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA didetail;
                            if (NativeMethods.SetupDiGetDeviceInterfaceDetail(devInfo, ref did, out didetail))
                            {
                                callback(devInfo, dvi, did, deviceID, didetail);
                            }
                        }
                    }
                }
                finally
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(devInfo);
                }
            }
        }

        protected override object[] GetHidDeviceKeys()
        {
            var paths = new List<object>();

            var hidGuid = NativeMethods.HidD_GetHidGuid();
            EnumerateDevices(hidGuid, (_, __, ___, deviceID, didetail) =>
                {
                    paths.Add(new HidDevicePath()
                    {
                        DeviceID = deviceID,
                        DevicePath = didetail.DevicePath
                    });
                });

            return paths.ToArray();
        }

        protected override object[] GetSerialDeviceKeys()
        {
            var paths = new List<object>();

            EnumerateDevices(NativeMethods.GuidForComPort, (deviceInfoSet, deviceInfoData, deviceInterfaceData, deviceID, didetail) =>
                {
                    string friendlyName;
                    if (NativeMethods.TryGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_FRIENDLYNAME, out friendlyName))
                    {

                    }
                    else if (NativeMethods.TryGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, NativeMethods.SPDRP_DEVICEDESC, out friendlyName))
                    {

                    }
                    else
                    {
                        friendlyName = null;
                    }

                    if (!string.IsNullOrEmpty(friendlyName))
                    {
                        string portName;
                        if (NativeMethods.TryGetSerialPortName(deviceInfoSet, ref deviceInfoData, out portName))
                        {
                            paths.Add(new SerialDevicePath()
                            {
                                DeviceID = deviceID,
                                DevicePath = didetail.DevicePath,
                                FileSystemName = portName,
                                FriendlyName = friendlyName
                            });
                        }
                    }
                });

            return paths.ToArray();
        }

        protected override bool TryCreateHidDevice(object key, out Device device)
        {
            var path = (HidDevicePath)key;
            device = WinHidDevice.TryCreate(path.DevicePath, path.DeviceID);
            return device != null;
        }

        protected override bool TryCreateSerialDevice(object key, out Device device)
        {
            var path = (SerialDevicePath)key;
            device = WinSerialDevice.TryCreate(path.DevicePath, path.FileSystemName, path.FriendlyName); return true;
        }

        public override bool AreDriversBeingInstalled
        {
            get
            {
                try
                {
                    return NativeMethods.WAIT_TIMEOUT == NativeMethods.CMP_WaitNoPendingInstallEvents(0);
                }
                catch
                {
                    return false;
                }
            }
        }

        public override string FriendlyName
        {
            get { return "Windows HID"; }
        }

        public override bool IsSupported
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    var version = new NativeMethods.OSVERSIONINFO();
                    version.OSVersionInfoSize = Marshal.SizeOf(typeof(NativeMethods.OSVERSIONINFO));

                    try
                    {
                        if (NativeMethods.GetVersionEx(ref version) && version.PlatformID == 2)
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Apparently we have no P/Invoke access.
                    }
                }

                return false;
            }
        }
    }
}
