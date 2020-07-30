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
using System.Linq;
using System.Runtime.InteropServices;

namespace HidSharp
{
    [ComVisible(true), Guid("CD7CBD7D-7204-473c-AA2A-2B9622CFC6CC")]
    public class HidDeviceLoader
    {
        public HidDeviceLoader()
        {

        }

        public IEnumerable GetDevicesVB()
        {
            return GetDevices();
        }

        public IEnumerable<HidDevice> GetDevices()
        {
            return Platform.HidSelector.Instance.GetDevices();
        }

        public IEnumerable<HidDevice> GetDevices
            (int? vendorID = null, int? productID = null, int? productVersion = null, string serialNumber = null)
        {
            int vid = vendorID ?? -1, pid = productID ?? -1, ver = productVersion ?? -1;
            foreach (HidDevice hid in GetDevices())
            {
                if ((hid.VendorID == vendorID || vid < 0) &&
                    (hid.ProductID == productID || pid < 0) &&
                    (hid.ProductVersion == productVersion || ver < 0) &&
                    (hid.SerialNumber == serialNumber || string.IsNullOrEmpty(serialNumber)))
                {
                    yield return hid;
                }
            }
        }

        public HidDevice GetDeviceOrDefault
            (int? vendorID = null, int? productID = null, int? productVersion = null, string serialNumber = null)
        {
            return GetDevices(vendorID, productID, productVersion, serialNumber).FirstOrDefault();
        }
    }
}
