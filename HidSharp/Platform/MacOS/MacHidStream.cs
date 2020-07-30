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
using System.Runtime.InteropServices;
using System.Threading;

namespace HidSharp.Platform.MacOS
{
    class MacHidStream : HidStream
    {
        Queue<byte[]> _inputQueue;
        Queue<CommonOutputReport> _outputQueue;

        MacHidDevice _device;
        IntPtr _handle;
        IntPtr _readRunLoop;
        Thread _readThread, _writeThread;
        volatile bool _shutdown;

        internal MacHidStream()
        {
            _inputQueue = new Queue<byte[]>();
            _outputQueue = new Queue<CommonOutputReport>();
            _readThread = new Thread(ReadThread);
			_readThread.IsBackground = true;
            _writeThread = new Thread(WriteThread);
			_writeThread.IsBackground = true;
        }
		
		internal void Init(MacApi.io_string_t path, MacHidDevice device)
		{
            IntPtr handle;
            using (var service = MacApi.IORegistryEntryFromPath(0, ref path).ToIOObject())
            {
                handle = MacApi.IOHIDDeviceCreate(IntPtr.Zero, service);
                if (handle == IntPtr.Zero) { throw new IOException("HID device not found."); }

                if (MacApi.IOReturn.Success != MacApi.IOHIDDeviceOpen(handle)) { MacApi.CFRelease(handle); throw new IOException("Unable to open HID device."); }
            }
            _device = device;
            _handle = handle;
			HandleInitAndOpen();

            _readThread.Start();
            _writeThread.Start();
		}
		
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
			if (!HandleClose()) { return; }
			
            _shutdown = true;
            try { lock (_outputQueue) { Monitor.PulseAll(_outputQueue); } } catch { }

            MacApi.CFRunLoopStop(_readRunLoop);
            try { _readThread.Join(); } catch { }
            try { _writeThread.Join(); } catch { }
			
			HandleRelease();
        }

		internal override void HandleFree()
		{
			MacApi.CFRelease(_handle); _handle = IntPtr.Zero;
		}
		
        static void ReadThreadEnqueue(Queue<byte[]> queue, byte[] report)
        {
            lock (queue)
            {
                if (queue.Count < 100) { queue.Enqueue(report); Monitor.PulseAll(queue); }
            }
        }

        void ReadThreadCallback(IntPtr context, MacApi.IOReturn result, IntPtr sender,
                                	   MacApi.IOHIDReportType type,
		                               uint reportID, IntPtr report, IntPtr reportLength)
        {
            byte[] reportBytes = new byte[(int)reportLength];
            Marshal.Copy(report, reportBytes, 0, reportBytes.Length);

            if (result == MacApi.IOReturn.Success && reportLength != IntPtr.Zero)
            {
                if (type == MacApi.IOHIDReportType.Input)
                {
                    ReadThreadEnqueue(_inputQueue, reportBytes);
                }
            }
        }

        unsafe void ReadThread()
        {			
			if (!HandleAcquire()) { return; }
			_readRunLoop = MacApi.CFRunLoopGetCurrent();
			
            try
            {
				var callback = new MacApi.IOHIDReportCallback(ReadThreadCallback);

                byte[] inputReport = new byte[_device.MaxInputReportLength];
                fixed (byte* inputReportBytes = inputReport)
                {
                    MacApi.IOHIDDeviceRegisterInputReportCallback(_handle,
                                                                  (IntPtr)inputReportBytes, (IntPtr)inputReport.Length,
                                                                  callback, IntPtr.Zero);
                    MacApi.IOHIDDeviceScheduleWithRunLoop(_handle, _readRunLoop, MacApi.kCFRunLoopDefaultMode);
                    MacApi.CFRunLoopRun();
                    MacApi.IOHIDDeviceUnscheduleFromRunLoop(_handle, _readRunLoop, MacApi.kCFRunLoopDefaultMode);
                }
				
				GC.KeepAlive(this);
				GC.KeepAlive(callback);
                GC.KeepAlive(_inputQueue);
            }
            finally
            {
                HandleRelease();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return CommonRead(buffer, offset, count, _inputQueue);
        }

        public unsafe override void GetFeature(byte[] buffer, int offset, int count)
        {
            CheckItAll(buffer, offset, count);
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            fixed (byte* bufferBytes = buffer)
	            {
	                IntPtr reportLength = (IntPtr)count;
	                if (MacApi.IOReturn.Success != MacApi.IOHIDDeviceGetReport(_handle, MacApi.IOHIDReportType.Feature,
	                                                                           (IntPtr)buffer[offset],
	                                                                           (IntPtr)(bufferBytes + offset),
	                                                                           ref reportLength))
	
	                {
	                    throw new IOException("GetFeature failed.");
	                }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        unsafe void WriteThread()
        {
			if (!HandleAcquire()) { return; }
			
			try
	        {	
				lock (_outputQueue)
				{								
	                while (true)
	                {
	                    while (!_shutdown && _outputQueue.Count == 0) { Monitor.Wait(_outputQueue); }
						if (_shutdown) { break; }
	
						MacApi.IOReturn ret;
	                    CommonOutputReport outputReport = _outputQueue.Peek();
	                    try
	                    {
	                        fixed (byte* outputReportBytes = outputReport.Bytes)
	                        {
	                            Monitor.Exit(_outputQueue);
	
	                            try
	                            {
	                                ret = MacApi.IOHIDDeviceSetReport(_handle,
									                                  outputReport.Feature ? MacApi.IOHIDReportType.Feature : MacApi.IOHIDReportType.Output,
	                                                                  (IntPtr)outputReport.Bytes[0],
	                                                                  (IntPtr)outputReportBytes,
	                                                                  (IntPtr)outputReport.Bytes.Length);
	                                if (ret == MacApi.IOReturn.Success) { outputReport.DoneOK = true; }
	                            }
	                            finally
	                            {
	                                Monitor.Enter(_outputQueue);
	                            }
	                        }
	                    }
	                    finally
	                    {
							_outputQueue.Dequeue();
	                        outputReport.Done = true;
	                        Monitor.PulseAll(_outputQueue);
	                    }
	                }
	            }
			}
            finally
            {
                HandleRelease();
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, false, _device.MaxOutputReportLength);
        }

        public override void SetFeature(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, true, _device.MaxOutputReportLength);
        }
    }
}
