using Microsoft.UI.Xaml.Documents;

namespace WinAppSdkCleaner.Views;

internal sealed partial class InfoDialog : ContentDialog
{
    public InfoDialog(List<(string name, string value)> info) : base()
    {
        PrimaryButtonText = "OK";
        DefaultButton = ContentDialogButton.Primary;

        ScrollView sv = new ScrollView() 
        { 
            ContentOrientation = ScrollingContentOrientation.Horizontal,
        };

        RichTextBlock rtb = new RichTextBlock()
        {
            Margin = new Thickness(16, 0, 0, 16),
            IsTextSelectionEnabled = true,
        };

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

        sv.Content = rtb;
        Content = sv;
    }
}

