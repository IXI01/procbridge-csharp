
using System;

namespace ProcBridge_CSharp
{
    public enum StatusCode
    {
        UNKNOWN = 99,
        REQUEST = 0,
        GOOD_RESPONSE = 1,
        BAD_RESPONSE = 2
    }

    public static class StatusCodeHelper {

    public static StatusCode FromRawValue(int rawValue)
    {
        return rawValue switch
        {
            0 => StatusCode.REQUEST,
            1 => StatusCode.GOOD_RESPONSE,
            2 => StatusCode.BAD_RESPONSE,
            _ => StatusCode.UNKNOWN
        };
    }

    public static byte ToRawValue(StatusCode statusCode)
    {
        return statusCode switch
        {
            StatusCode.REQUEST => 0,
            StatusCode.GOOD_RESPONSE => 1,
            StatusCode.BAD_RESPONSE => 2,
            StatusCode.UNKNOWN => 99,
            _ => throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, null)
        };
    }

}
}