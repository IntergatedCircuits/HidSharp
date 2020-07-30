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

namespace HidSharp.Platform.MacOS
{
    class MacHidDevice : HidDevice
    {
        string _manufacturer;
        string _productName;
        string _serialNumber;
        int _vid, _pid, _version;
        int _maxInput, _maxOutput, _maxFeature;
        MacApi.io_string_t _path;

        internal MacHidDevice(MacApi.io_string_t path)
        {
            _path = path;
        }
		
        public override HidStream Open()
        {
            var stream = new MacHidStream();
            try { stream.Init(_path, this); return stream; }
            catch { stream.Close(); throw; }
        }

        internal bool GetInfo(int handle)
        {
            int? vid = MacApi.IORegistryGetCFProperty_Int(handle, MacApi.kIOHIDVendorIDKey);
            int? pid = MacApi.IORegistryGetCFProperty_Int(handle, MacApi.kIOHIDProductIDKey);
            int? version = MacApi.IORegistryGetCFProperty_Int(handle, MacApi.kIOHIDVersionNumberKey);
            if (vid == null || pid == null || version == null) { return false; }

            _vid = (int)vid;
            _pid = (int)pid;
            _version = (int)version;
            _maxInput = MacApi.IORegistryGetCFProperty_Int(handle, MacApi.kIOHIDMaxInputReportSizeKey) ?? 0;
            _maxOutput = MacApi.IORegistryGetCFProperty_Int(handle, MacApi.kIOHIDMaxOutputReportSizeKey) ?? 0;
            _maxFeature = MacApi.IORegistryGetCFProperty_Int(handle, MacApi.kIOHIDMaxFeatureReportSizeKey) ?? 0;
            _manufacturer = MacApi.IORegistryGetCFProperty_String(handle, MacApi.kIOHIDManufacturerKey) ?? "";
            _productName = MacApi.IORegistryGetCFProperty_String(handle, MacApi.kIOHIDProductKey) ?? "";
            _serialNumber = MacApi.IORegistryGetCFProperty_String(handle, MacApi.kIOHIDSerialNumberKey) ?? "";
            return true;
        }

        public override int MaxInputReportLength
        {
            get { return _maxInput; }
        }

        public override int MaxOutputReportLength
        {
            get { return _maxOutput; }
        }

        public override int MaxFeatureReportLength
        {
            get { return _maxFeature; }
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
