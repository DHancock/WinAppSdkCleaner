namespace CsWin32Lib;

public static class Monitors
{

    public static Rect GetWorkingAreaOfClosestMonitor(Rect windowBounds)
    {
        HMONITOR hMonitor = PInvoke.MonitorFromRect(ConvertToRECT(windowBounds), MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);

        if (Marshal.GetLastPInvokeError() != (int)WIN32_ERROR.ERROR_SUCCESS)
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        MONITORINFO monitorInfo = new MONITORINFO();
        monitorInfo.cbSize = (uint)Unsafe.SizeOf<MONITORINFO>();

        if (!PInvoke.GetMonitorInfo(hMonitor, ref monitorInfo))
            throw new Win32Exception(Marshal.GetLastPInvokeError());

        return new Rect(monitorInfo.rcWork.X, monitorInfo.rcWork.Y, monitorInfo.rcWork.Width, monitorInfo.rcWork.Height); ;
    }

    private static RECT ConvertToRECT(Rect input)
    {
        RECT outRECT = new RECT();

        // avoids accumulating rounding errors
        outRECT.top = Convert.ToInt32(input.Y);
        outRECT.left = Convert.ToInt32(input.X);
        outRECT.bottom = outRECT.top + Convert.ToInt32(input.Height);
        outRECT.right = outRECT.left + Convert.ToInt32(input.Width);

        return outRECT;
    }
}

