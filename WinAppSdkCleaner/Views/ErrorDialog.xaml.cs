using WinAppSdkCleaner.Utilities;

namespace WinAppSdkCleaner.Views;

internal sealed partial class ErrorDialog : ContentDialog
{
    private ErrorDialog() : base()
    {
        this.InitializeComponent();
    }

    public ErrorDialog(string message, string details) : this()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        Paragraph messageParagraph = new Paragraph();
        messageParagraph.Inlines.Add(new Run() { Text = message, FontSize = 16, FontWeight = FontWeights.SemiBold });

        Paragraph detailsParagraph = new Paragraph();
        detailsParagraph.Inlines.Add(new Run() { Text = details });

        ErrorTextBlock.Blocks.Add(messageParagraph);
        ErrorTextBlock.Blocks.Add(detailsParagraph);

        Utils.PlayExclamation();
    }
}
