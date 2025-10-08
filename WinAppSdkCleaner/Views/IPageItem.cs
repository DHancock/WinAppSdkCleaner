namespace WinAppSdkCleaner.Views;

internal interface IPageItem
{
    int PassthroughCount { get; }
    void AddPassthroughContent(in RectInt32[] rects);
}
