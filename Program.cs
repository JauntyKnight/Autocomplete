using static System.Console;
using SignatureHashing;
using static System.Math;
using Gdk;
using GLib;
using Gtk;
using GtkSource;
using Application = Gtk.Application;
using Key = Gdk.Key;


class Provider : GLib.Object, GtkSource.ICompletionProviderImplementor
{
    private SignatDictionary signatDict;
    private Dictionary<string, double> freqDict;
    private const int MaxSuggestions = 5;
    private const int MaxDist = 3;

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
            // using some almost random constants to compute the score
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

    string ApplyMask(string s, string t)
    {
        // returns a copy a copy of t, but with uppercase chars at indexes where
        // s is uppercase
        string r = "";
        for (int i = 0; i < Min(s.Length, t.Length); ++i)
            r += s[i].ToString() == s[i].ToString().ToUpper()
                ? t[i].ToString().ToUpper()
                : t[i];

        return s.Length < t.Length
            ? r + t.Substring(s.Length, t.Length - s.Length)
            : r;
    }
    
    public void Populate(CompletionContext context) {
        // Find the text that needs to be autocompleted.
        TextIter end = context.Iter;
        TextIter start = end;
        start.BackwardVisibleWordStart();
        GLib.List list = new GLib.List(typeof(CompletionItem));
        string word_to_complete = start.Buffer.GetText(start, end, false);
        string word_to_complete_lower = word_to_complete.ToLower();
    
        // dont suggest anything if the string contains non-recognizable chars
        if (!SignatString.IsValidString(word_to_complete_lower))
        {
            context.AddProposals(new CompletionProviderAdapter(this), list, true);
            return;
        }
        
        if (word_to_complete.Length >= 1)
        {
            var suggestions =
                signatDict.Lookup(new SignatString(word_to_complete_lower), MaxDist);

            SortSuggestions(word_to_complete_lower, suggestions);
            suggestions = suggestions.GetRange(0, Min(MaxSuggestions, suggestions.Count));

            foreach (var word in suggestions)
            {
                CompletionItem item = new CompletionItem();
                item.Label = item.Text = ApplyMask(word_to_complete, word.Item1);
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

    private void UpdateWindowTitle()
    {
        Title = $"Autocomplete: {filename}";
    }
    
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
            UpdateWindowTitle();
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
            UpdateWindowTitle();
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

    private bool CheckKeyPressMask(KeyPressEventArgs args, ModifierType mask)
        => (args.Event.State & mask) != 0;
    
    private void OnKeyPressEvent(object? o, KeyPressEventArgs args)
    {
        // Shortcut handler
        if (CheckKeyPressMask(args, ModifierType.ControlMask))
        {
            if (CheckKeyPressMask(args, ModifierType.ShiftMask) && args.Event.Key == Key.s)
                OnSaveAsClicked(null, EventArgs.Empty);
            else if (args.Event.Key == Key.s)
                OnSaveClicked(null, EventArgs.Empty);
            else if (args.Event.Key == Key.o)
                OnOpenClicked(null, EventArgs.Empty);
        }
    }
    
    private void OnKeyReleaseEvent(object? o, KeyReleaseEventArgs args)
    {
        // Shortcut handler
        WriteLine("release");
        if (args.Event.State == ModifierType.ControlMask)
        {
            WriteLine("fafsad");
            if (args.Event.Key == Key.Alt_L && args.Event.Key == Key.s)
                OnSaveAsClicked(null, EventArgs.Empty);
            else if (args.Event.Key == Key.s)
                OnSaveClicked(null, EventArgs.Empty);
            else if (args.Event.Key == Key.o)
                OnOpenClicked(null, EventArgs.Empty);
        }
    }

    public MainWindow() : base("Autocomplete")
    {
        // holds the content of the window
        VBox vBox = new VBox();
        // toolbar with save, open and options buttons
        Toolbar toolbar = new Toolbar();
        vBox.Add(toolbar);
        
        // KeyPressEvent += OnKeyPressEvent;
        // KeyReleaseEvent += OnKeyReleaseEvent;

        var saveBtn = new ToolButton(new Image("img/Save.png"), "Save");
        saveBtn.TooltipText = "Save File";
        saveBtn.Clicked += OnSaveClicked;
        toolbar.Add(saveBtn);
        
        var saveAsBtn = new ToolButton(new Image("img/SaveAs.png"), "Save as");
        saveAsBtn.TooltipText = "Save File As";
        saveAsBtn.Clicked += OnSaveAsClicked;
        toolbar.Add(saveAsBtn);
        
        var openBtn = new ToolButton(new Image("img/Open.png"), "Open");
        openBtn.TooltipText = "Open File";
        openBtn.Clicked += OnOpenClicked;
        toolbar.Add(openBtn);

        ScrolledWindow scrolledWindow = new ScrolledWindow();
        scrolledWindow.SetSizeRequest(800, 600);
        view = new SourceView();
        view.KeyPressEvent += OnKeyPressEvent;
        view.SetSizeRequest(800, 600);

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