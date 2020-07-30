#region License
/* Copyright 2012-2018 James F. Bellinger <http://www.zer7.com/software/hidsharp>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

      http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing,
   software distributed under the License is distributed on an
   "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
   KIND, either express or implied.  See the License for the
   specific language governing permissions and limitations
   under the License. */
#endregion

//#define SINGLE_THREADED_POLLING_APPROACH
#define SINGLE_THREADED_WAITHANDLE_APPROACH
//#define THREAD_POOL_RECEIVED_EVENT_APPROACH
//#define RAW_APPROACH

#define SHOW_CHANGES_ONLY

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using HidSharp.Reports;
using HidSharp.Reports.Encodings;

namespace HidSharp.Test
{
    class Program
    {
        static void WriteDeviceItemInputParserResult(Reports.Input.DeviceItemInputParser parser)
        {
#if SHOW_CHANGES_ONLY
            while (parser.HasChanged)
            {
                int changedIndex = parser.GetNextChangedIndex();
                var previousDataValue = parser.GetPreviousValue(changedIndex);
                var dataValue = parser.GetValue(changedIndex);

                Console.WriteLine(string.Format("  {0}: {1} -> {2}",
                                  (Usage)dataValue.Usages.FirstOrDefault(), previousDataValue.GetPhysicalValue(), dataValue.GetPhysicalValue()));
            }
#else
            if (parser.HasChanged)
            {
                int valueCount = parser.ValueCount;

                for (int valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    var dataValue = parser.GetValue(valueIndex);
                    Console.Write(string.Format("  {0}: {1}",
                                      (Usage)dataValue.Usages.FirstOrDefault(), dataValue.GetPhysicalValue()));

                }

                Console.WriteLine();
            }
#endif
        }

        static void Main(string[] args)
        {
            var list = DeviceList.Local;
            list.Changed += (sender, e) => Console.WriteLine("Device list changed.");
            
            /*
            var serialDeviceList = list.GetSerialDevices().ToArray();
            Console.WriteLine("Serial device list:");
            foreach (SerialDevice dev in serialDeviceList)
            {
                Console.WriteLine(dev.DevicePath);
            }

            Console.WriteLine();
            */

            var stopwatch = Stopwatch.StartNew();
            var hidDeviceList = list.GetHidDevices().ToArray();

            Console.WriteLine("Complete device list (took {0} ms to get {1} devices):",
                              stopwatch.ElapsedMilliseconds, hidDeviceList.Length);
            foreach (HidDevice dev in hidDeviceList)
            {
                Console.WriteLine(dev.DevicePath);
                //Console.WriteLine(string.Join(",", dev.GetDevicePathHierarchy())); // TODO
                Console.WriteLine(dev);

                try
                {
                    Console.WriteLine(string.Format("Max Lengths: Input {0}, Output {1}, Feature {2}",
                        dev.GetMaxInputReportLength(),
                        dev.GetMaxOutputReportLength(),
                        dev.GetMaxFeatureReportLength()));
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine();
                    continue;
                }

                try
                {
                    Console.WriteLine("Serial Ports: {0}", string.Join(",", dev.GetSerialPorts()));
                }
                catch
                {
                    Console.WriteLine("Serial Ports: Unknown on this platform.");
                }

                try
                {
                    var rawReportDescriptor = dev.GetRawReportDescriptor();
                    Console.WriteLine("Report Descriptor:");
                    Console.WriteLine("  {0} ({1} bytes)", string.Join(" ", rawReportDescriptor.Select(d => d.ToString("X2"))), rawReportDescriptor.Length);

                    int indent = 0;
                    foreach (var element in EncodedItem.DecodeItems(rawReportDescriptor, 0, rawReportDescriptor.Length))
                    {
                        if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.EndCollection) { indent -= 2; }

                        Console.WriteLine("  {0}{1}", new string(' ', indent), element);

                        if (element.ItemType == ItemType.Main && element.TagForMain == MainItemTag.Collection) { indent += 2; }
                    }

                    var reportDescriptor = dev.GetReportDescriptor();

                    // Lengths should match.
                    Debug.Assert(dev.GetMaxInputReportLength() == reportDescriptor.MaxInputReportLength);
                    Debug.Assert(dev.GetMaxOutputReportLength() == reportDescriptor.MaxOutputReportLength);
                    Debug.Assert(dev.GetMaxFeatureReportLength() == reportDescriptor.MaxFeatureReportLength);

                    foreach (var deviceItem in reportDescriptor.DeviceItems)
                    {
                        foreach (var usage in deviceItem.Usages.GetAllValues())
                        {
                            Console.WriteLine(string.Format("Usage: {0:X4} {1}", usage, (Usage)usage));
                        }
                        foreach (var report in deviceItem.Reports)
                        {
                            Console.WriteLine(string.Format("{0}: ReportID={1}, Length={2}, Items={3}",
                                                report.ReportType, report.ReportID, report.Length, report.DataItems.Count));
                            foreach (var dataItem in report.DataItems)
                            {
                                Console.WriteLine(string.Format("  {0} Elements x {1} Bits, Units: {2}, Expected Usage Type: {3}, Flags: {4}, Usages: {5}",
                                    dataItem.ElementCount, dataItem.ElementBits, dataItem.Unit.System, dataItem.ExpectedUsageType, dataItem.Flags,
                                    string.Join(", ", dataItem.Usages.GetAllValues().Select(usage => usage.ToString("X4") + " " + ((Usage)usage).ToString()))));
                            }
                        }

                        {
                            Console.WriteLine("Opening device for 20 seconds...");

                            HidStream hidStream;
                            if (dev.TryOpen(out hidStream))
                            {
                                Console.WriteLine("Opened device.");
                                hidStream.ReadTimeout = Timeout.Infinite;

                                using (hidStream)
                                {
                                    var inputReportBuffer = new byte[dev.GetMaxInputReportLength()];
                                    var inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
                                    var inputParser = deviceItem.CreateDeviceItemInputParser();

#if SINGLE_THREADED_WAITHANDLE_APPROACH
                                    inputReceiver.Start(hidStream);

                                    int startTime = Environment.TickCount;
                                    while (true)
                                    {
                                        if (inputReceiver.WaitHandle.WaitOne(1000))
                                        {
                                            if (!inputReceiver.IsRunning) { break; } // Disconnected?

                                            Report report;
                                            while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                                            {
                                                // Parse the report if possible.
                                                // This will return false if (for example) the report applies to a different DeviceItem.
                                                if (inputParser.TryParseReport(inputReportBuffer, 0, report))
                                                {
                                                    WriteDeviceItemInputParserResult(inputParser);
                                                }
                                            }
                                        }

                                        uint elapsedTime = (uint)(Environment.TickCount - startTime);
                                        if (elapsedTime >= 20000) { break; } // Stay open for 20 seconds.
                                    }
#elif SINGLE_THREADED_POLLING_APPROACH
                                    inputReceiver.Start(hidStream);

                                    int startTime = Environment.TickCount;
                                    while (true)
                                    {
                                        if (!inputReceiver.IsRunning) { break; } // Disconnected?

                                        Report report; // Periodically check if the receiver has any reports.
                                        while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                                        {
                                            // Parse the report if possible.
                                            // This will return false if (for example) the report applies to a different DeviceItem.
                                            if (inputParser.TryParseReport(inputReportBuffer, 0, report))
                                            {
                                                WriteDeviceItemInputParserResult(inputParser);
                                            }
                                        }
                                    }

                                    uint elapsedTime = (uint)(Environment.TickCount - startTime);
                                    if (elapsedTime >= 20000) { break; } // Stay open for 20 seconds.
#elif THREAD_POOL_RECEIVED_EVENT_APPROACH
                                    inputReceiver.Received += (sender, e) =>
                                        {
                                            Report report;
                                            while (inputReceiver.TryRead(inputReportBuffer, 0, out report))
                                            {
                                                // Parse the report if possible.
                                                // This will return false if (for example) the report applies to a different DeviceItem.
                                                if (inputParser.TryParseReport(inputReportBuffer, 0, report))
                                                {
                                                    // If you are using Windows Forms, you could call BeginInvoke here to marshal the results
                                                    // to your main thread.
                                                    WriteDeviceItemInputParserResult(inputParser);
                                                }
                                            }
                                        };
                                    inputReceiver.Start(hidStream);

                                    Thread.Sleep(20000);
#elif RAW_APPROACH
                                    IAsyncResult ar = null;

                                    int startTime = Environment.TickCount;
                                    while (true)
                                    {
                                        if (ar == null)
                                        {
                                            ar = hidStream.BeginRead(inputReportBuffer, 0, inputReportBuffer.Length, null, null);
                                        }

                                        if (ar != null)
                                        {
                                            if (ar.IsCompleted)
                                            {
                                                int byteCount = hidStream.EndRead(ar);
                                                ar = null;

                                                if (byteCount > 0)
                                                {
                                                    string hexOfBytes = string.Join(" ", inputReportBuffer.Take(byteCount).Select(b => b.ToString("X2")));
                                                    Console.WriteLine("  {0}", hexOfBytes);
                                                }
                                            }
                                            else
                                            {
                                                ar.AsyncWaitHandle.WaitOne(1000);
                                            }
                                        }

                                        uint elapsedTime = (uint)(Environment.TickCount - startTime);
                                        if (elapsedTime >= 20000) { break; } // Stay open for 20 seconds.
                                    }
#else
#error "Choose an approach for the example."
#endif
                                }

                                Console.WriteLine("Closed device.");
                            }
                            else
                            {
                                Console.WriteLine("Failed to open device.");
                            }

                            Console.WriteLine();
                        }
                    }

                    Console.WriteLine();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            Console.WriteLine("Press a key to exit...");
            Console.ReadKey();
        }
    }
}
