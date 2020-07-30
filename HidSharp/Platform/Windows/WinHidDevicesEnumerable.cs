#region License
/* Copyright 2010 James F. Bellinger <http://www.zer7.com>

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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Windows
{
    sealed class WinHidDevicesEnumerator : IEnumerator<HidDevice>
    {
        HidDevice _current; int _i;
        WinApi.HDEVINFO _devInfoSet; Guid _hidGuid;

        public WinHidDevicesEnumerator()
        {
            Reset();

            WinApi.HidD_GetHidGuid(out _hidGuid);
            _devInfoSet = WinApi.SetupDiGetClassDevs(_hidGuid, null, IntPtr.Zero, WinApi.DIGCF.AllClasses | WinApi.DIGCF.DeviceInterface | WinApi.DIGCF.Present);
        }

        public bool MoveNext()
        {
            if (_devInfoSet.IsValid)
            {
                var did = new WinApi.SP_DEVICE_INTERFACE_DATA(); did.Size = Marshal.SizeOf(did);

                while (WinApi.SetupDiEnumDeviceInterfaces(_devInfoSet, IntPtr.Zero, _hidGuid, _i, ref did))
                {
                    _i ++;

                    var didetail = new WinApi.SP_DEVICE_INTERFACE_DETAIL_DATA();
                    didetail.Size = IntPtr.Size == 8 ? 8 : (4 + Marshal.SystemDefaultCharSize);
                    if (WinApi.SetupDiGetDeviceInterfaceDetail(_devInfoSet, ref did, ref didetail,
                        Marshal.SizeOf(didetail) - (int)Marshal.OffsetOf(didetail.GetType(), "DevicePath"),
                        IntPtr.Zero, IntPtr.Zero))
                    {
                        string path = didetail.DevicePath; var hidDevice = new WinHidDevice(path);
                        IntPtr handle = WinApi.CreateFileFromDevice(path, WinApi.EFileAccess.None, WinApi.EFileShare.All);
                        if (handle != (IntPtr)(-1))
                        {
                            try { hidDevice.GetInfo(handle); } catch { continue; }
                            finally { WinApi.CloseHandle(handle); }

                            _current = hidDevice; return true;
                        }
                    }
                }
            }

            return false;
        }

        ~WinHidDevicesEnumerator()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (_devInfoSet.IsValid)
            {
                WinApi.SetupDiDestroyDeviceInfoList(_devInfoSet);
                _devInfoSet.Invalidate();
            }
        }

        public void Reset()
        {
            _current = null; _devInfoSet.Invalidate(); _i = 0;
        }

        public HidDevice Current
        {
            get { return _current; }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }
    }

    sealed class WinHidDevicesEnumerable : IEnumerable<HidDevice>
    {
        public IEnumerator<HidDevice> GetEnumerator()
        {
            return new WinHidDevicesEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
