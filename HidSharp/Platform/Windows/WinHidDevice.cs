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

#pragma warning disable 618

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HidSharp.Platform.Windows
{
    sealed class WinHidDevice : HidDevice
    {
        string _path;

        string _manufacturer;
        string _productName;
        string _serialNumber;
        int _pid, _vid, _version;

        public WinHidDevice(string path)
        {
            _path = path;
        }

        public override Stream Open()
        {
            IntPtr handle = WinApi.CreateFileFromDevice(_path, WinApi.EFileAccess.Read | WinApi.EFileAccess.Write, WinApi.EFileShare.All);
            if (handle == (IntPtr)(-1)) { throw new IOException("Unable to open HID device."); }

            try { var stream = new FileStream(handle, FileAccess.ReadWrite, true); return stream; }
            catch { WinApi.CloseHandle(handle); throw; }
        }

        internal void GetInfo(IntPtr handle)
        {
            char[] buffer = new char[128];
            _manufacturer = WinApi.HidD_GetManufacturerString(handle, buffer, 256) ? WinApi.NTString(buffer) : "";
            _productName = WinApi.HidD_GetProductString(handle, buffer, 256) ? WinApi.NTString(buffer) : "";
            _serialNumber = WinApi.HidD_GetSerialNumberString(handle, buffer, 256) ? WinApi.NTString(buffer) : "";

            var attributes = new WinApi.HIDD_ATTRIBUTES(); attributes.Size = Marshal.SizeOf(attributes);
            if (WinApi.HidD_GetAttributes(handle, ref attributes))
            {
                _pid = attributes.ProductID; _vid = attributes.VendorID; _version = attributes.VersionNumber;
            }
        }

        public override string Manufacturer
        {
            get { return _manufacturer; }
        }

        public override int ProductID
        {
            get { return _pid; }
        }

        public override string ProductName
        {
            get { return _productName; }
        }

        public override int ProductVersion
        {
            get { return _version; }
        }

        public override string SerialNumber
        {
            get { return _serialNumber; }
        }

        public override int VendorID
        {
            get { return _vid; }
        }
    }
}
