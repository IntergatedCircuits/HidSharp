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
using System.Text;

namespace HidSharp.Platform.Linux
{
    class LinuxHidDevice : HidDevice
    {
        string _manufacturer;
        string _productName;
        string _serialNumber;
        int _vid, _pid, _version;
        string _path;

        public LinuxHidDevice(string path)
        {
            _path = path;
        }

        public override HidStream Open()
        {
            var stream = new LinuxHidStream();
            try { stream.Init(_path, this); return stream; }
            catch { stream.Close(); throw; }
        }

        internal unsafe bool GetInfo()
        {
            IntPtr udev = LinuxApi.udev_new();
            if (IntPtr.Zero != udev)
            {
                try
                {
                    IntPtr device = LinuxApi.udev_device_new_from_syspath(udev, _path);
                    if (device != IntPtr.Zero)
                    {
                        try
                        {
                            IntPtr parent = LinuxApi.udev_device_get_parent_with_subsystem_devtype(device, "usb", "usb_device");
                            if (IntPtr.Zero != parent)
                            {
                                string manufacturer = LinuxApi.udev_device_get_sysattr_value(parent, "manufacturer") ?? "";
                                string productName = LinuxApi.udev_device_get_sysattr_value(parent, "product") ?? "";
                                string serialNumber = LinuxApi.udev_device_get_sysattr_value(parent, "serial") ?? "";
                                string idVendor = LinuxApi.udev_device_get_sysattr_value(parent, "idVendor");
                                string idProduct = LinuxApi.udev_device_get_sysattr_value(parent, "idProduct");
                                string version = LinuxApi.udev_device_get_sysattr_value(parent, "version");

                                int vid, pid, verMajor, verMinor;
                                if (LinuxApi.TryParseHex(idVendor, out vid) &&
                                    LinuxApi.TryParseHex(idProduct, out pid) &&
                                    LinuxApi.TryParseVersion(version, out verMajor, out verMinor))
                                {
                                    _vid = vid;
                                    _pid = pid;
                                    _version = verMajor << 8 | verMinor;
                                    _manufacturer = manufacturer;
                                    _productName = productName;
                                    _serialNumber = serialNumber;
                                    return true;
                                }
                            }
                        }
                        finally
                        {
                            LinuxApi.udev_device_unref(device);
                        }
                    }
                }
                finally
                {
                    LinuxApi.udev_unref(udev);
                }
            }

            return false;
        }

        public override int MaxInputReportLength
        {
            get { throw new NotSupportedException(); }
        }

        public override int MaxOutputReportLength
        {
            get { throw new NotSupportedException(); }
        }

        public override int MaxFeatureReportLength
        {
            get { throw new NotSupportedException(); }
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
