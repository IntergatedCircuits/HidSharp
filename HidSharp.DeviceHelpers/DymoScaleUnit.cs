using System.Runtime.InteropServices;

namespace HidSharp.DeviceHelpers
{
    [ComVisible(true), Guid("9574310D-702A-4047-871B-9398E9ECA2AE")]
    public enum DymoScaleUnit
    {
        Milligram = 1,
        Gram = 2,
        Kilogram = 3,
        Ounce = 11,
        Pound = 12
    }
}
