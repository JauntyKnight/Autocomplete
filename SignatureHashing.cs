using System.Collections;
using System.Collections.Generic;
using static System.Console;
using static System.Math;
using static DamerauLevenshtein;


static class DamerauLevenshtein
{
    public static int Distance(string p, string s)
    {
        int n = p.Length + 1, m = s.Length + 1;
        int[,] d = new int[n, m];

        for (int i = 1; i < n; ++i)
            d[i, 0] = i;
        for (int i = 1; i < m; ++i)
            d[0, i] = i;
        
        for (int i = 1; i < n; ++i)
            for (int j = 1; j < m; ++j)
            {
                // simple Levenshtein distance
                int k = Min(d[i - 1, j], d[i, j - 1]) + 1;
                d[i, j] = Min(k, d[i - 1, j - 1] + (p[i - 1] == s[j - 1] ? 0 : 1));
                
                // considering transpositions
                for (int x = 1; x < i; ++x)
                    for (int y = 1; y < j; ++y)
                        if (p[i - 1] == s[y - 1] && p[x - 1] == s[j - 1])
                            d[i, j] = Min(
                                d[i, j],
                                d[x - 1, y - 1] + (i - x) + (j - y) - 1
                            );
            }

        return d[n - 1, m - 1];
    }
}

class Signature
{
    private const int MaxChars = 28;
    private readonly int[] signat;
    

    public Signature(string s)
    {
        signat = new int[MaxChars];
        foreach (char c in s)
            if (c == '\'')
                signat[MaxChars - 2]++;
            else if (c == '-')
                signat[MaxChars - 1]++;
            else
                signat[c - 'a']++;
    }

    public int this[int index]
    {
        get => signat[index];
    }
    
    private static int FCHelper(Signature x, Signature y, bool plus)
    {
        int r = 0;
        if (plus)
        {
            for (int i = 0; i < MaxChars; ++i)
                if (x[i] > y[i])
                    r += x[i] - y[i];
        }
        else
        {
            for (int i = 0; i < MaxChars; ++i)
                            if (x[i] < y[i])
                                r += y[i] - x[i];
        }

        return r;
    }
    
    public static int FC(Signature x, Signature y)
        => Max(FCHelper(x, y, true), FCHelper(x, y, false));
    
    public override int GetHashCode()
    {
        int hash = 17;
        for (int i = 0; i < MaxChars; ++i)
            hash = hash * 19 + signat[i];
        return hash;
    }
}

class SignatString
{
    private string s;
    private Signature signat;

    public SignatString(string t)
    {
        s = t;
        signat = new Signature(s);
    }
    
    public Signature Signat => signat;

    public string text => s;
    
    public void get_suggestions_fc(SignatDictionary dict, int k)
    {
        // List<string>
        Parallel.ForEach(dict.Keys, key =>
        {
            if (Signature.FC(key, signat) <= k)
                foreach (var s in dict[key])
                {
                    if (Distance(text, s) <= k)
                            WriteLine(s);
                }
        });
    }
}


class SignatDictionary : Dictionary<Signature, List<string>>
{
    private int size = 436450;

    public SignatDictionary()
    {
        EnsureCapacity(size);
    }

    public void Fill(string filename)
    {
        using (StreamReader sr = new StreamReader(filename))
            while (sr.ReadLine() is string line)
            {
                Signature signat = new Signature(line);
                if (ContainsKey(signat))
                    this[signat].Add(line);
                else
                {
                    this[signat] = new List<string>();
                    this[signat].Add(line);
                }
            }
    }
}

class Program
{
    static void Main()
    {
        DateTime start = DateTime.Now;
        SignatDictionary dict = new SignatDictionary();
        dict.Fill("dict.txt");
        double elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine(dict.Count);
        WriteLine($"Filled in {elapsed:f1} ms");
        start = DateTime.Now;
        SignatString s = new SignatString("hel");
        s.get_suggestions_fc(dict, 1);
        elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine($"Processed in {elapsed:f1} ms");
    }
}
