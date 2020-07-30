#region License
/* Copyright 2010, 2013 James F. Bellinger <http://www.zer7.com/software/hidsharp>

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
using System.Runtime.InteropServices;

namespace HidSharp.DeviceHelpers
{
    [ComVisible(true), Guid("A12824D8-F716-46a6-B74D-6F4E1C14AA87")]
    public class DymoScale
    {
        byte[] _buffer; int _offset;
        byte _reportID;
        Stream _stream;

        public DymoScale()
        {
            _buffer = new byte[1024]; ReportID = 3;
        }

        public DymoScale(Stream stream)
            : this()
        {
            Stream = stream;
        }

        [ComVisible(false)]
        public static string GetNameFromStatus(DymoScaleStatus status)
        {
            switch (status)
            {
                case DymoScaleStatus.Fault: return "Fault";
                case DymoScaleStatus.StableAtZero: return "Stable at Zero";
                case DymoScaleStatus.InMotion: return "In Motion";
                case DymoScaleStatus.Stable: return "Stable";
                case DymoScaleStatus.StableUnderZero: return "Stable under Zero";
                case DymoScaleStatus.OverWeight: return "Over Weight";
                case DymoScaleStatus.RequiresCalibration: return "Requires Calibration";
                case DymoScaleStatus.RequiresRezeroing: return "Requires Re-zeroing";
                default: return "Unknown Error";
            }
        }

        [ComVisible(false)]
        public static string GetNameFromUnit(DymoScaleUnit unit)
        {
            switch (unit)
            {
                case DymoScaleUnit.Milligram: return "mg";
                case DymoScaleUnit.Gram: return "g";
                case DymoScaleUnit.Kilogram: return "kg";
                case DymoScaleUnit.Ounce: return "oz";
                case DymoScaleUnit.Pound: return "lb";
                default: return "?";
            }
        }

        public void ReadSample(out int value, out int exponent, out DymoScaleUnit unit, out DymoScaleStatus status,
                               out bool buffered)
        {
            if (Stream == null) { throw new InvalidOperationException("Stream not set."); }
            buffered = true;

            while (_offset < ReportLength)
            {
                buffered = false;
                int count = Stream.Read(_buffer, _offset, _buffer.Length - _offset);
                _offset += count;
            }

            ParseSample(_buffer, 0, out value, out exponent, out unit, out status);
            Array.Copy(_buffer, ReportLength, _buffer, 0, _offset - ReportLength); _offset -= ReportLength;
        }

        // This exists for simple VBA COM clients.
        public void ReadSampleAndNames(out int value, out int exponent, out string unitName, out string statusName,
                                       out bool buffered)
        {
            DymoScaleUnit unit; DymoScaleStatus status;
            ReadSample(out value, out exponent, out unit, out status, out buffered);
            unitName = DymoScale.GetNameFromUnit(unit);
            statusName = DymoScale.GetNameFromStatus(status);
        }

        public void ParseSample(byte[] buffer, int offset,
                                out int value, out int exponent, out DymoScaleUnit unit, out DymoScaleStatus status)
        {
            value = 0; exponent = 0; unit = 0; status = 0;
            if (buffer == null) { throw new ArgumentNullException("buffer"); }
            if (offset < 0 || buffer.Length - offset < ReportLength) { throw new ArgumentException("Not enough bytes.", "offset"); }
            if (buffer[offset + 0] != ReportID) { throw new IOException("Unexpected report ID."); }

            value = (int)(buffer[offset + 4] | buffer[offset + 5] << 8);

            status = (DymoScaleStatus)buffer[offset + 1]; if (status == DymoScaleStatus.StableUnderZero) { value = -value; }
            unit = (DymoScaleUnit)buffer[offset + 2];
            exponent = (sbyte)buffer[offset + 3];
        }

        public byte ReportID
        {
            get { return _reportID; }
            set { _reportID = value; }
        }

        public static int ReportLength
        {
            get { return 6; }
        }

        public Stream Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }
    }
}
