namespace WinAppSdkCleaner.Views;

internal sealed partial class InfoDialog : ContentDialog
{
    private InfoDialog() : base()
    {
        this.InitializeComponent();
    }

    public InfoDialog(List<(string name, string value)> info, ImageSource? logo) : this()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        Loaded += (s, e) =>
        {
            LogoImage.Source = logo;
            HeadingText.Text = info[0].value;

            for (int index = 1; index < info.Count; index++)
            {
                Run nameRun = new Run();
                nameRun.FontWeight = FontWeights.SemiBold;
                nameRun.Text = info[index].name + ": ";

                Run valueRun = new Run();
                valueRun.Text = info[index].value;

                Paragraph paragraph = new Paragraph();
                paragraph.Inlines.Add(nameRun);
                paragraph.Inlines.Add(valueRun);

                InfoText.Blocks.Add(paragraph);
            }
        };
    }
}
