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

            Guid hidGuid; NativeMethods.HidD_GetHidGuid(out hidGuid);
            NativeMethods.HDEVINFO devInfo = NativeMethods.SetupDiGetClassDevs(hidGuid, null, IntPtr.Zero, 
                NativeMethods.DIGCF.AllClasses | NativeMethods.DIGCF.DeviceInterface | NativeMethods.DIGCF.Present);

            if (devInfo.IsValid)
            {
                try   
                {
                    NativeMethods.SP_DEVICE_INTERFACE_DATA did = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                    did.Size = Marshal.SizeOf(did);

                    for (int i = 0; NativeMethods.SetupDiEnumDeviceInterfaces(devInfo, IntPtr.Zero, hidGuid, i, ref did); i ++)
                    {
                        NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA didetail = new NativeMethods.SP_DEVICE_INTERFACE_DETAIL_DATA();
                        didetail.Size = IntPtr.Size == 8 ? 8 : (4 + Marshal.SystemDefaultCharSize);
                        if (NativeMethods.SetupDiGetDeviceInterfaceDetail(devInfo, ref did, ref didetail,
                            Marshal.SizeOf(didetail) - (int)Marshal.OffsetOf(didetail.GetType(), "DevicePath"),
                            IntPtr.Zero, IntPtr.Zero))
                        {
                            paths.Add(didetail.DevicePath);
                        }
                    }
                }
                finally
                {
                    NativeMethods.SetupDiDestroyDeviceInfoList(devInfo);
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override bool TryCreateDevice(object key, out HidDevice device)
        {
            string path = (string)key; var hidDevice = new WinHidDevice(path);
            IntPtr handle = NativeMethods.CreateFileFromDevice(path, NativeMethods.EFileAccess.None, NativeMethods.EFileShare.All);

            try
            {
                if (handle == (IntPtr)(-1) || !hidDevice.GetInfo(handle)) { device = null; return false; }
            }
            finally
            {
                if (handle != (IntPtr)(-1)) { NativeMethods.CloseHandle(handle); }
            }

            device = hidDevice; return true;
        }

        public override bool IsSupported
        {
            get
            {
                var version = new NativeMethods.OSVERSIONINFO();
                version.OSVersionInfoSize = Marshal.SizeOf(typeof(NativeMethods.OSVERSIONINFO));

                try
                {
                    if (NativeMethods.GetVersionEx(ref version))
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
