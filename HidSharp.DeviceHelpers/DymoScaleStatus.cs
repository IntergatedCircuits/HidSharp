using System.Runtime.InteropServices;

namespace HidSharp.DeviceHelpers
{
    [ComVisible(true), Guid("57A04A78-F1D3-4FEA-B641-2E4DCF86DB9C")]
    public enum DymoScaleStatus
    {
        Fault = 1,
        StableAtZero = 2,
        InMotion = 3,
        Stable = 4,
        StableUnderZero = 5,
        OverWeight = 6,
        RequiresCalibration = 7,
        RequiresRezeroing = 8
    }
}
