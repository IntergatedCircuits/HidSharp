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

//#define SAMPLE_OPEN_AND_READ
#define SAMPLE_DYMO_SCALE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HidSharp;
using HidSharp.DeviceHelpers;
using HidSharp.ReportDescriptors;
using HidSharp.ReportDescriptors.Parser;
using HidSharp.ReportDescriptors.Units;

namespace HidSharp.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            HidDeviceLoader loader = new HidDeviceLoader();
			Console.WriteLine("Complete device list:");
            foreach (HidDevice dev in loader.GetDevices())
            {
                Console.WriteLine(dev);
            }
            Console.WriteLine();

			Console.WriteLine("Opening HID device...");

#if SAMPLE_OPEN_AND_READ
            var device = loader.GetDeviceOrDefault(vendorID: 0x268b, productID: 0x0101);
            if (device == null) { Console.WriteLine("Failed to open device."); Environment.Exit(1); }

            Console.Write(@"
Max Lengths:
  Input:   {0}
  Output:  {1}
  Feature: {2}

"
, device.MaxInputReportLength
, device.MaxOutputReportLength
, device.MaxFeatureReportLength
);

            HidStream stream;
            if (!device.TryOpen(out stream)) { Console.WriteLine("Failed to open device."); Environment.Exit(2); }

            using (stream)
            {
			    int n = 0;
                while (true)
			    {
                    byte[] bytes = new byte[device.MaxInputReportLength];
                    int count = stream.Read(bytes, 0, bytes.Length);
					
                    if (count > 0)
                    {
                        Console.Write("* {0} : ", count);

                        for (int i = 0; i < count && i < 62; i++)
                        {
                            Console.Write("{0:X} ", bytes[i]);
                        }

                        Console.WriteLine();
					    if (++n == 100) { break; }
                    }
                }
            }
#elif SAMPLE_DYMO_SCALE
            HidDevice scale = loader.GetDeviceOrDefault(24726, 344);
            if (scale == null) { Console.WriteLine("Failed to find scale device."); Environment.Exit(1); }

            HidStream stream;
            if (!scale.TryOpen(out stream)) { Console.WriteLine("Failed to open scale device."); Environment.Exit(2); }

            using (stream)
            {
                int n = 0; DymoScale scaleReader = new DeviceHelpers.DymoScale(stream);
                while (true)
                {
                    int value, exponent;
                    DymoScaleUnit unit; string unitName;
                    DymoScaleStatus status; string statusName;
                    bool buffered;

                    scaleReader.ReadSample(out value, out exponent, out unit, out status, out buffered);
                    unitName = DymoScale.GetNameFromUnit(unit);
                    statusName = DymoScale.GetNameFromStatus(status);

                    Console.WriteLine("{4}  {0}: {1}x10^{2} {3} ", statusName, value, exponent, unitName, buffered ? "b" : " ");
                    if (!buffered) { if (++n == 100) { break; } }
                }
            }
#else
#error "No sample selected."
#endif
			
			Console.WriteLine("Press a key to exit...");
            Console.ReadKey();
        }
    }
}
