namespace WinAppSdkCleaner.Utilites;

internal static class Utils
{
    public static void PlayExclamation()
    {
        bool succeeded = PInvoke.MessageBeep(MESSAGEBOX_STYLE.MB_ICONEXCLAMATION);
        Debug.Assert(succeeded);
    }

    public static Point GetOffsetFromXamlRoot(UIElement e)
    {
        GeneralTransform gt = e.TransformToVisual(e.XamlRoot.Content);
        return gt.TransformPoint(new Point(0f, 0f));
    }

    public static RectInt32 ScaledRect(in Point location, in Vector2 size, double scale)
    {
        Debug.Assert(location.X >= 0.0);
        Debug.Assert(location.Y >= 0.0);
        Debug.Assert(size.X >= 0f);
        Debug.Assert(size.Y >= 0f);

        return new RectInt32((int)Math.FusedMultiplyAdd(location.X, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(location.Y, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(size.X, scale, 0.5),
                             (int)Math.FusedMultiplyAdd(size.Y, scale, 0.5));
    }

    public static RectInt32 GetPassthroughRect(UIElement e)
    {
        return ScaledRect(GetOffsetFromXamlRoot(e), e.ActualSize, e.XamlRoot.RasterizationScale);
    }
}
