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

namespace HidSharp.Platform.Linux
{
    class LinuxHidStream : HidStream
    {
		Queue<byte[]> _inputQueue;
		Queue<CommonOutputReport> _outputQueue;
		
		LinuxHidDevice _device;
		int _handle;
		Thread _readThread, _writeThread;
		volatile bool _shutdown;
		
        internal LinuxHidStream()
        {
			_inputQueue = new Queue<byte[]>();
			_outputQueue = new Queue<CommonOutputReport>();
			_handle = -1;
			_readThread = new Thread(ReadThread);
			_readThread.IsBackground = true;
			_writeThread = new Thread(WriteThread);
			_writeThread.IsBackground = true;
        }
		
		int DeviceHandleFromPath(string path)
		{
			IntPtr udev = LinuxApi.udev_new();
			if (IntPtr.Zero != udev)
			{
				try
				{
					IntPtr device = LinuxApi.udev_device_new_from_syspath(udev, path);
					if (IntPtr.Zero != device)
					{
						try
						{
							string devnode = LinuxApi.udev_device_get_devnode(device);
							if (devnode != null)
							{
								int handle = LinuxApi.retry(() => LinuxApi.open
								                            (devnode, LinuxApi.oflag.RDWR | LinuxApi.oflag.NONBLOCK));
								if (handle < 0)
								{
									var error = (LinuxApi.error)Marshal.GetLastWin32Error();
									if (error == LinuxApi.error.EACCES)
									{
										throw new UnauthorizedAccessException("Not permitted to open HID device at " + devnode + ".");
									}
									else
									{
										throw new IOException("Unable to open HID device (" + error.ToString() + ").");
									}
								}
								return handle;
							}
						}
						finally
						{
							LinuxApi.udev_device_unref(device);
						}
					}
				}
				finally
				{
					LinuxApi.udev_unref(udev);
				}
			}
			
			throw new IndexOutOfRangeException("HID device not found.");
		}
		
        internal void Init(string path, LinuxHidDevice device)
        {
			int handle;
			handle = DeviceHandleFromPath(path);
			
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
            try { lock (_inputQueue) { Monitor.PulseAll(_inputQueue); } } catch { }
			try { lock (_outputQueue) { Monitor.PulseAll(_outputQueue); } } catch { }

            try { _readThread.Join(); } catch { }
            try { _writeThread.Join(); } catch { }

			HandleRelease();
		}
		
		internal override void HandleFree()
		{
			LinuxApi.retry(() => LinuxApi.close(_handle)); _handle = -1;
		}
		
		unsafe void ReadThread()
		{
			if (!HandleAcquire()) { return; }
			
			try
			{
				lock (_inputQueue)
				{
					while (true)
					{
						var fds = new LinuxApi.pollfd[1];
						fds[0].fd = _handle;
						fds[0].events = LinuxApi.pollev.IN;
						
						while (!_shutdown)
						{
						tryReadAgain:
							int ret;
							Monitor.Exit(_inputQueue);
							try { ret = LinuxApi.retry(() => LinuxApi.poll(fds, (IntPtr)1, 250)); }
							finally { Monitor.Enter(_inputQueue); }
							if (ret != 1) { continue; }
							
							if (0 != (fds[0].revents & (LinuxApi.pollev.ERR | LinuxApi.pollev.HUP))) { break; }
							if (0 != (fds[0].revents & LinuxApi.pollev.IN))
							{
								byte[] inputReport = new byte[4096]; // TODO: Use the input report length.
								fixed (byte* inputBytes = inputReport)
								{
                                    var inputBytesPtr = (IntPtr)inputBytes;
									IntPtr length = LinuxApi.retry(() => LinuxApi.read
									                               (_handle, inputBytesPtr, (IntPtr)inputReport.Length));
									if ((long)length < 0)
									{
										if (Marshal.GetLastWin32Error() != (int)LinuxApi.error.EAGAIN) { break; }
										goto tryReadAgain;
									}
									
									Array.Resize(ref inputReport, (int)length);
									_inputQueue.Enqueue(inputReport);
								}
							}
						}
						while (!_shutdown && _inputQueue.Count == 0) { Monitor.Wait(_inputQueue); }
						if (_shutdown) { break; }
						
						_inputQueue.Dequeue();
					}
				}
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

        public override void GetFeature(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
						
						CommonOutputReport outputReport = _outputQueue.Peek();
						try
						{
							fixed (byte* outputBytes = outputReport.Bytes)
							{
								// hidraw is apparently blocking for output, even when O_NONBLOCK is used.
								// See for yourself at drivers/hid/hidraw.c...
                                IntPtr length;
                                Monitor.Exit(_outputQueue);
                                try
                                {
                                    var outputBytesPtr = (IntPtr)outputBytes;
                                    length = LinuxApi.retry(() => LinuxApi.write
                                                            (_handle, outputBytesPtr, (IntPtr)outputReport.Bytes.Length));
                                    if ((long)length == outputReport.Bytes.Length) { outputReport.DoneOK = true; }
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
            CommonWrite(buffer, offset, count, _outputQueue, false, 4096);
        }

        public override void SetFeature(byte[] buffer, int offset, int count)
        {
            CommonWrite(buffer, offset, count, _outputQueue, true, 4096);
        }
    }
}
