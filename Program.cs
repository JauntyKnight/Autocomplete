using System.Threading.Tasks.Sources;
using static System.Console;
using SignatureHashing;
using static System.Math;
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
    private int MaxDist = 3;

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
    
    private int SortDecreasing((string, double) a, (string, double) b)
        => b.Item2.CompareTo(a.Item2);

    private void SortSuggestions(string word_to_complete, List<(string, double)> suggestions)
    {
        for (int i = 0; i < suggestions.Count; ++i)
        {
            (string s, double distance) = suggestions[i];
            double score = 0;
            if (s != word_to_complete)
            {
                score = Pow(MaxDist - distance + 1, 5);
                score *= freqDict[suggestions[i].Item1];
                if (s.StartsWith(word_to_complete))
                    score *= 1000;
            }

            suggestions[i] = (s, score);
        }
        
        suggestions.Sort(SortDecreasing);
    }
    
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

            SortSuggestions(word_to_complete, suggestions);
            suggestions = suggestions.GetRange(0, Min(MaxSuggestions, suggestions.Count));

            foreach (var w in suggestions)
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
        ScrolledWindow scrolledWindow = new ScrolledWindow();
        scrolledWindow.SetSizeRequest(600, 400);
        SourceView view = new SourceView();
        view.SetSizeRequest(600, 400);
        scrolledWindow.Add(view);
        Add(scrolledWindow);
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