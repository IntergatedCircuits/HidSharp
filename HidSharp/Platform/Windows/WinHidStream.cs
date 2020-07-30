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
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.Windows
{
    class WinHidStream : HidStream
    {
        object _readSync = new object(), _writeSync = new object();
        byte[] _readBuffer, _writeBuffer;
        IntPtr _handle, _closeEventHandle;
        WinHidDevice _device;

        internal WinHidStream()
        {
            _closeEventHandle = WinApi.CreateManualResetEventOrThrow();
        }

        ~WinHidStream()
        {
			Close();
            WinApi.CloseHandle(_closeEventHandle);
        }

        internal void Init(string path, WinHidDevice device)
        {
            IntPtr handle = WinApi.CreateFileFromDevice(path, WinApi.EFileAccess.Read | WinApi.EFileAccess.Write, WinApi.EFileShare.All);
            if (handle == (IntPtr)(-1)) { throw new IOException("Unable to open HID device."); }

            _device = device;
			_handle = handle;
			HandleInitAndOpen();
        }
		
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
			if (!HandleClose()) { return; }
			
			WinApi.SetEvent(_closeEventHandle);
			HandleRelease();
		}
		
		internal override void HandleFree()
		{
			WinApi.CloseHandle(ref _handle);
			WinApi.CloseHandle(ref _closeEventHandle);
		}

        public unsafe override void GetFeature(byte[] buffer, int offset, int count)
        {
            CheckItAll(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* ptr = buffer)
	            {
	                if (!WinApi.HidD_GetFeature(_handle, ptr + offset, count))
	                    { throw new IOException("GetFeature failed.", new Win32Exception()); }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        // Buffer needs to be big enough for the largest report, plus a byte
        // for the Report ID.
        public unsafe override int Read(byte[] buffer, int offset, int count)
        {
            CheckItAll(buffer, offset, count); uint bytesTransferred;
            IntPtr @event = WinApi.CreateManualResetEventOrThrow();
			
			HandleAcquireIfOpenOrFail();
            try
            {
				lock (_readSync)
				{
	                int maxIn = _device.MaxInputReportLength;
	                Array.Resize(ref _readBuffer, maxIn); if (count > maxIn) { count = maxIn; }
	
	                fixed (byte* ptr = _readBuffer)
	                {
	                    WinApi.OVERLAPPED overlapped = new WinApi.OVERLAPPED();
	                    overlapped.Event = @event;
	                    WinApi.OverlappedOperation(_handle, @event, ReadTimeout, _closeEventHandle,
	                        WinApi.ReadFile(_handle, ptr, maxIn, IntPtr.Zero, ref overlapped),
	                        ref overlapped, out bytesTransferred);
	                    if (count > (int)bytesTransferred) { count = (int)bytesTransferred; }
	                    Array.Copy(_readBuffer, 0, buffer, offset, count);
	                    return count;
	                }
				}
            }
            finally
            {
				HandleRelease();
                WinApi.CloseHandle(@event);
            }
        }

        public unsafe override void SetFeature(byte[] buffer, int offset, int count)
        {
            CheckItAll(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* ptr = buffer)
	            {
	                if (!WinApi.HidD_SetFeature(_handle, ptr + offset, count))
	                    { throw new IOException("SetFeature failed.", new Win32Exception()); }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        public unsafe override void Write(byte[] buffer, int offset, int count)
        {
            CheckItAll(buffer, offset, count); uint bytesTransferred;
            IntPtr @event = WinApi.CreateManualResetEventOrThrow();

			HandleAcquireIfOpenOrFail();
            try
            {
				lock (_writeSync)
				{
	                int maxOut = _device.MaxOutputReportLength;
	                Array.Resize(ref _writeBuffer, maxOut); if (count > maxOut) { count = maxOut; }
	                Array.Copy(buffer, offset, _writeBuffer, 0, count); count = maxOut;
	
	                fixed (byte* ptr = _writeBuffer)
	                {
	                    int offset0 = 0;
	                    while (count > 0)
	                    {
	                        WinApi.OVERLAPPED overlapped = new WinApi.OVERLAPPED();
	                        overlapped.Event = @event;
	                        WinApi.OverlappedOperation(_handle, @event, WriteTimeout, _closeEventHandle,
	                            WinApi.WriteFile(_handle, ptr + offset0, count, IntPtr.Zero, ref overlapped),
	                            ref overlapped, out bytesTransferred);
	                        count -= (int)bytesTransferred; offset0 += (int)bytesTransferred;
	                    }
	                }
				}
            }
            finally
            {
				HandleRelease();
                WinApi.CloseHandle(@event);
            }
        }
    }
}
