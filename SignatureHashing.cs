using System.Collections;
using System.Collections.Generic;
using static System.Console;
using static System.Math;
using static Utils;
using System.Collections.Concurrent;


static class Utils
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
    
    public static void FillDictionaries(
        Dictionary<Signature, List<string>> wordsDict,
        Dictionary<string, double> freqDict,
        string filename
        )
    {
        // precomputed constant to avoid reading the file twice
        double freqSum = 2293211905f;
        using (StreamReader sr = new StreamReader(filename))
            while (sr.ReadLine() is string line)
            {
                // filling wordsDict
                string[] words = line.Trim().Split(null);
                Signature signat = new Signature(words[0]);
                if (wordsDict.ContainsKey(signat))
                    wordsDict[signat].Add(words[0]);
                else
                {
                    wordsDict[signat] = new List<string>();
                    wordsDict[signat].Add(words[0]);
                }
                
                // filling freqDict
                freqDict[words[0]] = double.Parse(words[1]) / freqSum;
            }
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

    private int Comparator((string, double) p, (string, double) q)
        => q.Item2.CompareTo(p.Item2);
    
    public void get_suggestions_fc(
        SignatDictionary wordsDict, Dictionary<string, double> freqDict,
        int k, int topOptions
        )
    {
        // k = max admissible edit distance
        DateTime start = DateTime.Now;
        ConcurrentBag<(string, double)> mathcesConcurrent = new ConcurrentBag<(string, double)>();
        Parallel.ForEach(wordsDict.Keys, key =>
        {
            if (Signature.FC(key, signat) <= k)
                foreach (var s in wordsDict[key])
                {
                    int dist = Distance(text, s);
                    // some absolutely random constants yet
                    if (dist <= k)
                            mathcesConcurrent.Add((s, Pow(k - dist + 1, 20) * freqDict[s]));
                }
        });

        List<(string, double)> mathces = mathcesConcurrent.ToList();
        mathces.Sort(Comparator);
        
        double elapsed = (DateTime.Now - start).TotalMilliseconds;
        // Output topOptions
        WriteLine($"Suggestions for {s}");
        for (int i = 0; i < topOptions && i < mathces.Count; ++i)
            WriteLine($"{mathces[i].Item1}: {mathces[i].Item2:f3}");
        WriteLine($"Processed suggestions in {elapsed:f1} ms");
        WriteLine("================");
    }
}


class SignatDictionary : Dictionary<Signature, List<string>>
{
    private int size = 436450;

    public SignatDictionary()
    {
        EnsureCapacity(size);
    }
}

class Program
{
    static void Main()
    {
        DateTime start = DateTime.Now;
        SignatDictionary wordsDict = new SignatDictionary();
        Dictionary<string, double> freqDict = new Dictionary<string, double>();
        FillDictionaries(wordsDict, freqDict,"dict_freq.txt");
        double elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine($"Filled in {elapsed:f1} ms");
        
        // testing with data from test_input.txt
        using (StreamReader sr = new StreamReader("test_input.txt"))
            while (sr.ReadLine() is string line)
            {
                SignatString word = new SignatString(line.Trim());
                word.get_suggestions_fc(wordsDict, freqDict, 2, 10);
            }
    }
}
