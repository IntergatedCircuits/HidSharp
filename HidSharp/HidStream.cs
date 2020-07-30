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
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable 420

namespace HidSharp
{
    [ComVisible(true), Guid("0C263D05-0D58-4c6c-AEA7-EB9E0C5338A2")]
    public abstract class HidStream : Stream
    {
		int _opened, _closed;
		volatile int _refCount;
		
        internal class CommonOutputReport
        {
            public byte[] Bytes;
			public bool DoneOK, Feature;
            public volatile bool Done;
        }
		
        internal HidStream()
        {
            ReadTimeout = 3000;
            WriteTimeout = 3000;
        }
		
		internal static void CheckNull(byte[] buffer)
		{
			if (buffer == null) { throw new ArgumentNullException("buffer"); }
		}
		
        internal static void CheckItAll(byte[] buffer, int offset, int count)
        {
            CheckNull(buffer);
            if (offset < 0 || offset > buffer.Length) { throw new ArgumentOutOfRangeException("offset"); }
            if (count < 0 || count > buffer.Length - offset) { throw new ArgumentOutOfRangeException("count"); }
        }
		
		internal static int GetTimeout(int startTime, int timeout)
		{
			return Math.Min(timeout, Math.Max(0, startTime + timeout - Environment.TickCount));
		}
		
        internal int CommonRead(byte[] buffer, int offset, int count, Queue<byte[]> queue)
        {
            CheckItAll(buffer, offset, count);
            if (count == 0) { return 0; }

            int readTimeout = ReadTimeout;
            int startTime = Environment.TickCount;
            int timeout;

			HandleAcquireIfOpenOrFail();
			try
			{
	            lock (queue)
	            {
	                while (true)
	                {
	                    if (queue.Count > 0)
	                    {
	                        byte[] packet = queue.Dequeue();
	                        count = Math.Min(count, packet.Length);
	                        Array.Copy(packet, 0, buffer, offset, count);
	                        return count;
	                    }
	
	                    timeout = GetTimeout(startTime, readTimeout);
	                    if (!Monitor.Wait(queue, timeout)) { throw new TimeoutException(); }
	                }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        internal void CommonWrite(byte[] buffer, int offset, int count,
		                          Queue<CommonOutputReport> queue,
		                          bool feature, int maxOutputReportLength)
        {
            CheckItAll(buffer, offset, count);
            count = Math.Min(count, maxOutputReportLength);
            if (count == 0) { return; }

            int writeTimeout = WriteTimeout;
            int startTime = Environment.TickCount;
            int timeout;
			
			HandleAcquireIfOpenOrFail();
			try
			{
	            lock (queue)
	            {
	                while (true)
	                {
	                    if (queue.Count == 0)
	                    {
	                        byte[] packet = new byte[count];
	                        Array.Copy(buffer, offset, packet, 0, count);
	                        var outputReport = new CommonOutputReport() { Bytes = packet, Feature = feature };
	                        queue.Enqueue(outputReport);
	                        Monitor.PulseAll(queue);
	
	                        while (true)
	                        {
	                            if (outputReport.Done)
	                            {
	                                if (!outputReport.DoneOK) { throw new IOException(); }
	                                return;
	                            }
	
	                            timeout = GetTimeout(startTime, writeTimeout);
	                            if (!Monitor.Wait(queue, timeout)) { throw new TimeoutException(); }
	                        }
	                    }
	
	                    timeout = GetTimeout(startTime, writeTimeout);
	                    if (!Monitor.Wait(queue, timeout)) { throw new TimeoutException(); }
	                }
	            }
			}
			finally
			{
				HandleRelease();
			}
        }

        public override void Flush()
        {
            
        }

        public void GetFeature(byte[] buffer)
        {
			CheckNull(buffer);
            GetFeature(buffer, 0, buffer.Length);
        }

        public abstract void GetFeature(byte[] buffer, int offset, int count);

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return AsyncResult<int>.BeginOperation(delegate()
            {
                return Read(buffer, offset, count);
            }, callback, state);
        }

        public int Read(byte[] buffer)
        {
			CheckNull(buffer);
            return Read(buffer, 0, buffer.Length);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return ((AsyncResult<int>)asyncResult).EndOperation();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public void SetFeature(byte[] buffer)
        {
			CheckNull(buffer);
            SetFeature(buffer, 0, buffer.Length);
        }

        public abstract void SetFeature(byte[] buffer, int offset, int count);

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return AsyncResult<int>.BeginOperation(delegate()
            {
                Write(buffer, offset, count); return 0;
            }, callback, state);
        }
		
		internal void HandleInitAndOpen()
		{
			_opened = 1; _refCount = 1;
		}
		
		internal bool HandleClose()
		{
			return 0 == Interlocked.CompareExchange(ref _closed, 1, 0) && _opened != 0;
		}
		
		internal bool HandleAcquire()
		{
			while (true)
			{
				int refCount = _refCount;
				if (refCount == 0) { return false; }
				
				if (refCount == Interlocked.CompareExchange
				    (ref _refCount, refCount + 1, refCount))
				{
					return true;
				}
			}
		}
		
		internal void HandleAcquireIfOpenOrFail()
		{
			if (_closed != 0 || !HandleAcquire()) { throw new IOException("Closed."); }
		}
		
		internal void HandleRelease()
		{
			if (0 == Interlocked.Decrement(ref _refCount))
			{
				if (_opened != 0) { HandleFree(); }
			}
		}
		
		internal abstract void HandleFree();
		
        public void Write(byte[] buffer)
        {
			CheckNull(buffer);
            Write(buffer, 0, buffer.Length);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            ((AsyncResult<int>)asyncResult).EndOperation();
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override bool CanTimeout
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public sealed override int ReadTimeout
        {
            get;
            set;
        }

        public sealed override int WriteTimeout
        {
            get;
            set;
        }
    }
}
