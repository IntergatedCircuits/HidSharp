#region License
/* Copyright 2010-2012 James F. Bellinger <http://www.zer7.com>

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
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace HidSharp
{
    [ComVisible(true), Guid("4D8A9A1A-D5CC-414e-8356-5A025EDA098D")]
    public abstract class HidDevice
    {
        public abstract HidStream Open();

        public virtual byte[] GetReportDescriptor()
        {
            throw new NotSupportedException(); // Windows without libusb can't... Linux can. TODO: Offer it where it's available.
        }

        public bool TryOpen(out HidStream stream)
        {
            try
			{
				stream = Open();
				return true;
			}
            catch (Exception e)
			{
#if DEBUG
				Console.WriteLine(e);
#endif
				stream = null; return false;
			}
		}

        public abstract int MaxInputReportLength { get; }

        public abstract int MaxOutputReportLength { get; }

        public abstract int MaxFeatureReportLength { get; }

        public abstract string Manufacturer
        {
            get;
        }

        public abstract int ProductID
        {
            get;
        }

        public abstract string ProductName
        {
            get;
        }

        public abstract int ProductVersion
        {
            get;
        }

        public abstract string SerialNumber
        {
            get;
        }

        public abstract int VendorID
        {
            get;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} ({1}VID {2}, PID {3}, version {4})",
                Manufacturer.Length > 0 || ProductName.Length > 0 ? Manufacturer.Trim() + " " + ProductName.Trim() : "(unnamed)",
                SerialNumber.Length > 0 ? "serial " + SerialNumber.Trim() + ", " : "", VendorID, ProductID, ProductVersion);
        }
    }
}
