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
using System.Runtime.InteropServices;

namespace HidSharp
{
    [ClassInterface(ClassInterfaceType.AutoDual), Guid("A12824D8-F716-46a6-B74D-6F4E1C14AA87")]
    public class DymoScale
    {
        Stream _stream;
        byte[] _buffer; int _offset;

        public DymoScale()
        {
            _buffer = new byte[1024];
        }

        public void ReadSample(out int value, out int exponent, out string unit, out string status, out bool buffered)
        {
            status = "Unknown Error"; unit = "?"; buffered = true;

            while (_offset < 6)
            {
                buffered = false;
                int count = _stream.Read(_buffer, _offset, _buffer.Length - _offset);
                _offset += count;
            }

            switch (_buffer[0])
            {
                case 3:
                    value = (int)(_buffer[4] | _buffer[5] << 8);
                    switch (_buffer[1])
                    {
                        case 1: status = "Fault"; break;
                        case 2: status = "Stable at Zero"; break;
                        case 3: status = "In Motion"; break;
                        case 4: status = "Stable"; break;
                        case 5: status = "Stable under Zero"; value = -value; break;
                        case 6: status = "Over Weight"; break;
                        case 7: status = "Requires Calibration"; break;
                        case 8: status = "Requires Re-zeroing"; break;
                    }

                    switch (_buffer[2])
                    {
                        case 1: unit = "mg"; break;
                        case 2: unit = "g"; break;
                        case 3: unit = "kg"; break;
                        case 11: unit = "oz"; break;
                        case 12: unit = "lb"; break;
                    }

                    exponent = (sbyte)_buffer[3];
                    Array.Copy(_buffer, 6, _buffer, 0, _offset - 6); _offset -= 6;
                    break;

                default:
                    throw new IOException("Unknown buffer prefix.");
            }
        }

        public Stream Stream
        {
            get { return _stream; }
            set { _stream = value; }
        }
    }
}
