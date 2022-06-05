using Cairo;
using static System.Console;
using SignatureHashing;
using static System.Math;
using static Utils.Utils;
using Gdk;
using GLib;
using Gtk;
using GtkSource;
using Application = Gtk.Application;
using CancelArgs = Gtk.CancelArgs;
using CancelHandler = Gtk.CancelHandler;
using Menu = Gtk.Menu;

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
    private SourceView view;
    private string filename = "";  // the file where the data is saved
    
    private void OnOpenClicked(object? o, EventArgs args)
    {
        var fcd = new FileChooserDialog("Open File", null, FileChooserAction.Open);
        fcd.AddButton(Stock.Cancel, ResponseType.Cancel);
        fcd.AddButton(Stock.Open, ResponseType.Ok);
        fcd.DefaultResponse = ResponseType.Ok;
        fcd.SelectMultiple = false;
        if (fcd.Run() == (int) ResponseType.Ok)
        {
            filename = fcd.Filename;
            view.Buffer.Clear();
            using (StreamReader sr = new StreamReader(fcd.Filename))
                while (sr.ReadLine() is string s)
                    view.Buffer.Text += s + '\n';
        }
        fcd.Destroy();
    }

    private void SaveToFile()
    {
        using (StreamWriter sw = new StreamWriter(filename))
            foreach (var s in view.Buffer.Text.Split('\n'))
                sw.WriteLine(s);
    }

    private void OnSaveAsClicked(object? o, EventArgs args)
    {
        var fcd = new FileChooserDialog("Save File", null, FileChooserAction.Save);
        fcd.AddButton(Stock.Cancel, ResponseType.Cancel);
        fcd.AddButton(Stock.SaveAs, ResponseType.Ok);
        fcd.DefaultResponse = ResponseType.Ok;
        fcd.SelectMultiple = false;
        if (fcd.Run() == (int)ResponseType.Ok)
        {
            filename = fcd.Filename;
            SaveToFile();
        }
        fcd.Destroy();
    }
    
    private void OnSaveClicked(object? o, EventArgs args)
    {
        if (filename != "")
            SaveToFile();
        else
            OnSaveAsClicked(null, EventArgs.Empty);
    }

    public MainWindow() : base("Autocomplete")
    {
        // holds the content of the window
        VBox vBox = new VBox();
        // toolbar with save, open and options buttons
        Toolbar toolbar = new Toolbar();
        vBox.Add(toolbar);

        var saveBtn = new ToolButton(new Image("img/Save.png"), "Save");
        saveBtn.Clicked += OnSaveClicked;
        toolbar.Add(saveBtn);
        
        var saveAsBtn = new ToolButton(new Image("img/SaveAs.png"), "Save");
        saveAsBtn.Clicked += OnSaveAsClicked;
        toolbar.Add(saveAsBtn);
        
        var openBtn = new ToolButton(new Image("img/Open.png"), "Open");
        openBtn.Clicked += OnOpenClicked;
        toolbar.Add(openBtn);
        
        ScrolledWindow scrolledWindow = new ScrolledWindow();
        scrolledWindow.SetSizeRequest(600, 400);
        view = new SourceView();
        view.SetSizeRequest(600, 400);
        scrolledWindow.Add(view);
        vBox.Add(scrolledWindow);
        Add(vBox);
        view.Completion.AddProvider(new CompletionProviderAdapter(new Provider()));
        view.Completion.ShowHeaders = false;
        ShowAll();
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