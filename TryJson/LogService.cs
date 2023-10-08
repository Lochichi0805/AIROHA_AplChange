using System;

namespace TryJson
{
    internal class LogService
    {
        internal static void KeepException(Exception ex)
        {
            throw ex;
        }
    }
}