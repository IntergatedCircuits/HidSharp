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
using System.IO;
using System.Linq;
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
            foreach (HidDevice dev in loader.GetDevices()) { Console.WriteLine(dev); }
			
			Console.WriteLine("Opening HID device...");
            HidStream stream;
            if (loader
                .GetDeviceOrDefault(vendorID: 0x268b, productID: 0x0001)
                .TryOpen(out stream))
            {				
				int n = 0;
                while (true)
				{
                byte[] test = new byte[62];
                test[0] = 2;
                test[1] = 60;
                stream.Write(test);

                    byte[] bytes = new byte[62];
                    int count = stream.Read(bytes, 0, bytes.Length);
					
                    if (count > 0)
                    {
                        Console.Write("* {0} : ", count);

                        for (int i = 0; i < count && i < 62; i++)
                        {
                            Console.Write("{0:X} ", bytes[i]);
                        }

                        Console.WriteLine();
						if (++n == 1000) { stream.Close(); break; }
                        /*if (bytes[0] == 2)
                        {
                            byte[] cc = new byte[258];
                            cc[0] = 1;
                            cc[1] = (byte)(++k & 7);
                            stream.Write(cc, 0, cc.Length);
                        }*/
                    }

                    /*if (!first)
                    {
                        stream.SetFeature(new byte[] { 1, 6 }, 0, 2);
                        first = true;
                    }*/
                }
            }

            /*HidDevice scale = loader.GetDeviceOrDefault(24726, 344);

            if (scale != null)
            {
                Stream stream;
                if (scale.TryOpen(out stream))
                {
                    DymoScale scaleReader = new DymoScale();
                    scaleReader.Stream = stream;

                    while (true)
                    {
                        int value, exponent; string unit, status; bool buffered;
                        scaleReader.ReadSample(out value, out exponent, out unit, out status, out buffered);
                        Console.WriteLine("{4}  {0}: {1}x10^{2} {3} ", status, value, exponent, unit, buffered ? "b" : " ");
                    }

                    stream.Close();
                }
            }*/
			
			Console.WriteLine("...");
            //Console.ReadKey();
        }
    }
}
