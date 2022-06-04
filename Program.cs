using static System.Console;
using SignatureHashing;
using static Utils.Utils;
using Gdk;
using GLib;
using Gtk;
using GtkSource;
using Application = Gtk.Application;

class Provider : GLib.Object, GtkSource.ICompletionProviderImplementor
{
    private SignatDictionary signatDict;
    private Dictionary<string, double> freqDict;
    private int MaxSuggestions = 5;
    private int MaxDist = 2;

    public Provider()
    {
        signatDict = new SignatDictionary();
        freqDict = signatDict.Fill("dict_freq.txt");
    }

    public string Name => "completions";
    public Pixbuf Icon => null!;
    public string IconName => null!;
    public IIcon Gicon => null!;
    public int InteractiveDelay => -1;
    public int Priority => 0;

    // Display completions interactively as the user types.
    public CompletionActivation Activation => CompletionActivation.Interactive;

    public bool Match(CompletionContext context) => true;
    public Widget GetInfoWidget(ICompletionProposal proposal) => null!;
    public void UpdateInfo(ICompletionProposal proposal, CompletionInfo info) { }
    public bool GetStartIter(CompletionContext context, ICompletionProposal proposal, TextIter iter)
        => false;
    public bool ActivateProposal(ICompletionProposal proposal, TextIter iter) => false;
    
    public void Populate(CompletionContext context) {
        // Find the text that needs to be autocompleted.
        TextIter end = context.Iter;
        TextIter start = end;
        start.BackwardVisibleWordStart();
        GLib.List list = new GLib.List(typeof(CompletionItem));
        string word_to_complete = start.Buffer.GetText(start, end, false).ToLower();
    
        // dont suggest anything if the string contains non-recognizable chars
        if (!SignatString.IsValidString(word_to_complete))
        {
            context.AddProposals(new CompletionProviderAdapter(this), list, true);
            return;
        }

        if (word_to_complete.Length >= 1)
        {
            var suggestions =
                signatDict.Lookup(new SignatString(word_to_complete), MaxDist);
            
            // sorting the suggestions
            
            foreach (var w in suggestions.GetRange(0, MaxSuggestions))
                if (w.Item1.StartsWith(word_to_complete))
                {
                    CompletionItem item = new CompletionItem();
                    item.Label = item.Text = w.Item1;
                    list.Append(item.Handle);
                }
        }

        context.AddProposals(new CompletionProviderAdapter(this), list, true);
    }
}


class MainWindow : Gtk.Window
{
    public MainWindow() : base("Autocomplete")
    {
        SourceView view = new SourceView();
        Add(view);
        view.Completion.AddProvider(new CompletionProviderAdapter(new Provider()));
        view.Completion.ShowHeaders = false;
    }

    protected override bool OnDeleteEvent(Event evnt)
    {
        Application.Quit();
        return base.OnDeleteEvent(evnt);
    }
}


static class Program
{
    static void Main()
    {
        Application.Init();
        var window = new MainWindow();
        window.ShowAll();
        Application.Run();
    }
}