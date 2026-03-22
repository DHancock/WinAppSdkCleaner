using Microsoft.UI.Xaml.Documents;

namespace WinAppSdkCleaner.Views;

internal sealed partial class InfoDialog : ContentDialog
{
    public InfoDialog(List<(string name, string value)> info) : base()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        RichTextBlock rtb = new RichTextBlock();

        for (int index = 0; index < info.Count; index++) 
        {
            Run nameRun = new Run();
            nameRun.FontWeight = FontWeights.SemiBold;                   
            nameRun.Text = info[index].name + ": ";

            Run valueRun = new Run();
            valueRun.Text = info[index].value;

            Paragraph paragraph = new Paragraph();
            paragraph.Inlines.Add(nameRun);
            paragraph.Inlines.Add(valueRun);

            rtb.Blocks.Add(paragraph);
        }

        rtb.IsTextSelectionEnabled = true;
        rtb.Margin = new Thickness(left: 15, 0, 0, 0);

        Content = rtb;
    }
}

