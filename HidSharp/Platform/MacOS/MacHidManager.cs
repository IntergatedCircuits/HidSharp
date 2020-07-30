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

namespace HidSharp.Platform.MacOS
{
    class MacHidManager : HidManager
    {
        protected override object[] Refresh()
        {
            var paths = new List<MacApi.io_string_t>();

            var matching = MacApi.IOServiceMatching("IOHIDDevice").ToCFType(); // Consumed by IOServiceGetMatchingServices, so DON'T Dispose().
            if (matching.IsSet)
            {
                int iteratorObj;
                if (MacApi.IOReturn.Success == MacApi.IOServiceGetMatchingServices(0, matching, out iteratorObj))
                {
                    using (var iterator = iteratorObj.ToIOObject())
                    {
                        while (true)
                        {
                            using (var handle = MacApi.IOIteratorNext(iterator).ToIOObject())
                            {
                                if (!handle.IsSet) { break; }

                                MacApi.io_string_t path;
                                if (MacApi.IOReturn.Success == MacApi.IORegistryEntryGetPath(handle, "IOService", out path))
                                {
                                    paths.Add(path);
                                }
                            }
                        }
                    }
                }
            }

            return paths.Cast<object>().ToArray();
        }

        protected override bool TryCreateDevice(object key, out HidDevice device)
        {
            var path = (MacApi.io_string_t)key; var hidDevice = new MacHidDevice(path);
            using (var handle = MacApi.IORegistryEntryFromPath(0, ref path).ToIOObject())
            {
                if (!handle.IsSet || !hidDevice.GetInfo(handle)) { device = null; return false; }
                device = hidDevice; return true;
            }
        }
        public override bool IsSupported
        {
            get
            {
                try
                {
                    IntPtr major; MacApi.OSErr majorErr = MacApi.Gestalt(MacApi.OSType.gestaltSystemVersionMajor, out major);
                    IntPtr minor; MacApi.OSErr minorErr = MacApi.Gestalt(MacApi.OSType.gestaltSystemVersionMinor, out minor);
                    if (majorErr == MacApi.OSErr.noErr && minorErr == MacApi.OSErr.noErr)
                    {
                        return (long)major >= 10 || ((long)major == 10 && (long)minor >= 5);
                    }
                }
                catch
                {

                }

                return false;
            }
        }
    }
}
