# HIDSharp

HIDSharp is a multiplatform C# library for USB HID devices.

Version 2.0 allows you to read and parse reports from any USB HID input device. You can read USB gamepads, scales, anything you need. It does this by providing full report descriptor and report parsing capability. HIDSharp is, to my knowledge, the first driverless cross-platform library which can do this! Of course, raw reading and writing is still fully supported.

Version 1.5 and below are COM enabled to allow use by VB6 and MS Access programs (the .NET Framework will of course need to be installed). If there is demand I may add COM support to HIDSharp 2.0.

HIDSharp has received eight years of continual use with a Dymo Scale in MS Access, and seven years in commercial software with a wide variety of USB HID devices I've developed, so I know it to be reliable.

HIDSharp supports Windows, MacOS, and Linux (hidraw).

HIDSharp uses the Apache open-source license.

Originally distributed by James F. Bellinger through [his webpage][Origin]. Available as a [Nuget package][OnNuget].

[Origin]: https://www.zer7.com/software/hidsharp
[OnNuget]: https://www.nuget.org/packages/HidSharp/
