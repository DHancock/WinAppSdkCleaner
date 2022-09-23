namespace WinAppSdkCleaner.Common;

public static class IntegrityLevel
{

    // this function isn't available via CsWin32 see:
    // https://github.com/microsoft/win32metadata/issues/436
    private static HANDLE GetCurrentThreadEffectiveToken()
    {
        return (HANDLE)new IntPtr(-6);
    }

    internal static uint GetIntegrityLevel(HANDLE handle = default)
    {
        if (handle == default)
            handle = GetCurrentThreadEffectiveToken();

        SafeFileHandle tokenHandle = new SafeFileHandle(handle, ownsHandle: false); // safe for pseudo handles

        unsafe
        {
            PInvoke.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, IntPtr.Zero.ToPointer(), 0, out uint size);

            if (Marshal.GetLastPInvokeError() != (int)WIN32_ERROR.ERROR_INSUFFICIENT_BUFFER)
                throw new Win32Exception(Marshal.GetLastPInvokeError());

            IntPtr param = Marshal.AllocHGlobal((int)size);

            try
            {
                if (!PInvoke.GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenIntegrityLevel, param.ToPointer(), size, out size))
                    throw new Win32Exception(Marshal.GetLastPInvokeError());

                TOKEN_MANDATORY_LABEL tokenLabel = Marshal.PtrToStructure<TOKEN_MANDATORY_LABEL>(param);

                Byte count = *PInvoke.GetSidSubAuthorityCount(tokenLabel.Label.Sid);
                uint integrity = *PInvoke.GetSidSubAuthority(tokenLabel.Label.Sid, (uint)count - 1);

                return integrity;
            }
            finally
            {
                Marshal.FreeHGlobal(param);
            }
        }
    }


    // this is a port of the functions used by the WinAppSdk installer to see if packages 
    // should be provisioned on install. If it's good enough for MS...
    // https://github.com/microsoft/WindowsAppSDK/blob/main/dev/Common/Security.IntegrityLevel.h

    // SECURITY_MANDATORY_HIGH_RID is used by administrative applications launched through
    // elevation when UAC is enabled, or normal applications if UAC is disabled and the user
    // is an administrator.
    // https://microsoft.github.io/AttackSurfaceAnalyzer/api/Microsoft.CST.AttackSurfaceAnalyzer.Utils.Elevation.html#Microsoft_CST_AttackSurfaceAnalyzer_Utils_Elevation_GetProcessIntegrityLevel

    public static bool IsElevated()
    {
        uint integrityLevel = GetIntegrityLevel();
        return integrityLevel >= PInvoke.SECURITY_MANDATORY_HIGH_RID;
    }
}