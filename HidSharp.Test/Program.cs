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
using System.IO;

namespace HidSharp.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var loader = new HidDeviceLoader();
            var scale = loader.GetDeviceOrDefault(24726, 344);

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
            }

            Console.ReadKey();
        }
    }
}
