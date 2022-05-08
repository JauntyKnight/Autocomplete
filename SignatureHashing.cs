using System.Collections;
using System.Collections.Generic;
using static System.Console;
using static System.Math;

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
        Parallel.ForEach(dict.Keys, key =>
        {
            if (Signature.FC(key, signat) <= k)
                foreach (var c in dict[key])
                {
                    WriteLine(c);
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
        SignatString s = new SignatString("typewrit");
        s.get_suggestions_fc(dict, 2);
        elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine($"Processed in {elapsed:f1} ms");
    }
}
