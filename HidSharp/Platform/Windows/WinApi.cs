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

#pragma warning disable 649

using System;
using System.Runtime.InteropServices;

namespace HidSharp
{
    static class WinApi
    {
        [Flags]
        public enum EFileAccess : uint
        {
            None = 0,
            Read = 0x80000000,
            Write = 0x40000000,
            Execute = 0x20000000,
            All = 0x10000000
        }

        [Flags]
        public enum EFileShare : uint
        {
            None = 0x00000000,
            Read = 0x00000001,
            Write = 0x00000002,
            Delete = 0x00000004,
            All = Read | Write | Delete
        }

        public enum ECreationDisposition : uint
        {
            New = 1,
            CreateAlways = 2,
            OpenExisting = 3,
            OpenAlways = 4,
            TruncateExisting = 5
        }

        [Flags]
        public enum EFileAttributes : uint
        {
            Readonly = 0x00000001,
            Hidden = 0x00000002,
            System = 0x00000004,
            Directory = 0x00000010,
            Archive = 0x00000020,
            Device = 0x00000040,
            Normal = 0x00000080,
            Temporary = 0x00000100,
            SparseFile = 0x00000200,
            ReparsePoint = 0x00000400,
            Compressed = 0x00000800,
            Offline = 0x00001000,
            NotContentIndexed = 0x00002000,
            Encrypted = 0x00004000,
            Writethrough = 0x80000000,
            Overlapped = 0x40000000,
            NoBuffering = 0x20000000,
            RandomAccess = 0x10000000,
            SequentialScan = 0x08000000,
            DeleteOnClose = 0x04000000,
            BackupSemantics = 0x02000000,
            PosixSemantics = 0x01000000,
            OpenReparsePoint = 0x00200000,
            OpenNoRecall = 0x00100000,
            FirstPipeInstance = 0x00080000
        }

        [Flags]
        public enum DIGCF
        {
            None = 0,
            Default = 1,
            Present = 2,
            AllClasses = 4,
            Profile = 8,
            DeviceInterface = 16
        }

        [Flags]
        public enum SPINT
        {
            None = 0,
            Active = 1,
            Default = 2,
            Removed = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HDEVINFO
        {
            IntPtr Value;

            public void Invalidate()
            {
                Value = (IntPtr)(-1);
            }

            public bool IsValid
            {
                get { return Value != (IntPtr)(-1); }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVINFO_DATA
        {
            public int Size;
            public Guid ClassGuid;
            public uint DevInst;
            IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int Size;
            public Guid InterfaceClassGuid;
            public SPINT Flags;
            IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public struct SP_DEVICE_INTERFACE_DETAIL_DATA
        {
            public int Size;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=1024)] public string DevicePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID, ProductID, VersionNumber;
        }

        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(out Guid hidGuid);

        [DllImport("hid.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool HidD_GetAttributes(IntPtr handle, ref HIDD_ATTRIBUTES attributes);

        [DllImport("hid.dll", CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool HidD_GetManufacturerString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool HidD_GetProductString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool HidD_GetSerialNumberString(IntPtr handle, char[] buffer, int bufferLengthInBytes);

        [DllImport("setupapi.dll", SetLastError=true)]
        public static extern HDEVINFO SetupDiGetClassDevs
            ([MarshalAs(UnmanagedType.LPStruct)] Guid classGuid, string enumerator, IntPtr hwndParent, DIGCF flags);

        [DllImport("setupapi.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiDestroyDeviceInfoList(HDEVINFO deviceInfoSet);

        [DllImport("setupapi.dll", SetLastError=true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiEnumDeviceInterfaces(HDEVINFO deviceInfoSet, IntPtr deviceInfoData,
            [MarshalAs(UnmanagedType.LPStruct)] Guid interfaceClassGuid, int memberIndex,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError=true, CharSet=CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(HDEVINFO deviceInfoSet,
            ref SP_DEVICE_INTERFACE_DATA deviceInterfaceData,
            ref SP_DEVICE_INTERFACE_DETAIL_DATA deviceInterfaceDetailData,
            int deviceInterfaceDetailDataSize, IntPtr requiredSize, IntPtr deviceInfoData);

        [DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)]
        public static extern IntPtr CreateFile(string filename, EFileAccess desiredAccess, EFileShare shareMode, IntPtr securityAttributes,
            ECreationDisposition creationDisposition, EFileAttributes attributes, IntPtr template);

        public static IntPtr CreateFileFromDevice(string filename, EFileAccess desiredAccess, EFileShare shareMode)
        {
            return CreateFile(filename, desiredAccess, shareMode, IntPtr.Zero,
                ECreationDisposition.OpenExisting, EFileAttributes.Device,
                IntPtr.Zero);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool ReadFile(IntPtr handle, byte* buffer, int bytesToRead, out int bytesRead, IntPtr overlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public unsafe static extern bool WriteFile(IntPtr handle, byte* buffer, int bytesToRead, out int bytesRead, IntPtr overlapped);

        public static string NTString(char[] buffer)
        {
            int index = Array.IndexOf(buffer, '\0');
            return new string(buffer, 0, index >= 0 ? index : buffer.Length);
        }
    }
}
