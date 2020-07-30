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

namespace HidSharp
{
    [ClassInterface(ClassInterfaceType.AutoDual), Guid("CD7CBD7D-7204-473c-AA2A-2B9622CFC6CC")]
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
            return new Platform.Windows.WinHidDevicesEnumerable();
        }

        public HidDevice GetDeviceOrDefault(int vendorID, int productID)
        {
            foreach (var hid in GetDevices())
            {
                if (hid.VendorID == vendorID && hid.ProductID == productID) { return hid; }
            }

            return null;
        }
    }
}
