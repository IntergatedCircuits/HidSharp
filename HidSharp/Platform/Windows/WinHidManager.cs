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
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    class WinHidManager : HidManager
    {
        protected override object[] Refresh()
        {
            var paths = new List<string>();

            Guid hidGuid; WinApi.HidD_GetHidGuid(out hidGuid);
            WinApi.HDEVINFO devInfo = WinApi.SetupDiGetClassDevs(hidGuid, null, IntPtr.Zero, 
                WinApi.DIGCF.AllClasses | WinApi.DIGCF.DeviceInterface | WinApi.DIGCF.Present);

            if (devInfo.IsValid)
            {
                try   
                {
                    WinApi.SP_DEVICE_INTERFACE_DATA did = new WinApi.SP_DEVICE_INTERFACE_DATA();
                    did.Size = Marshal.SizeOf(did);

                    for (int i = 0; WinApi.SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, hidGuid, i, ref did); i ++)
                    {
                        WinApi.SP_DEVICE_INTERFACE_DETAIL_DATA didetail = new WinApi.SP_DEVICE_INTERFACE_DETAIL_DATA();
                        didetail.Size = IntPtr.Size == 8 ? 8 : (4 + Marshal.SystemDefaultCharSize);
                        if (WinApi.SetupDiGetDeviceInterfaceDetail(devInfo, ref did, ref didetail,
                            Marshal.SizeOf(didetail) - (int)Marshal.OffsetOf(didetail.GetType(), "DevicePath"),
                            IntPtr.Zero, IntPtr.Zero))
                        {
                            paths.Add(didetail.DevicePath);
                        }
                    }
                }
                finally
                {
                    WinApi.SetupDiDestroyDeviceInfoList(devInfo);
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override bool TryCreateDevice(object key, out HidDevice device)
        {
            string path = (string)key; var hidDevice = new WinHidDevice(path);
            IntPtr handle = WinApi.CreateFileFromDevice(path, WinApi.EFileAccess.None, WinApi.EFileShare.All);

            try
            {
                if (handle == (IntPtr)(-1) || !hidDevice.GetInfo(handle)) { device = null; return false; }
            }
            finally
            {
                if (handle != (IntPtr)(-1)) { WinApi.CloseHandle(handle); }
            }

            device = hidDevice; return true;
        }

        public override bool IsSupported
        {
            get
            {
                var version = new WinApi.OSVERSIONINFO();
                version.OSVersionInfoSize = Marshal.SizeOf(typeof(WinApi.OSVERSIONINFO));

                try
                {
                    if (WinApi.GetVersionEx(ref version))
                    {
                        return true;
                    }
                }
                catch
                {

                }

                return false;
            }
        }
    }
}
