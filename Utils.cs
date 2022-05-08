using static System.Math;
using static FrequencyVectorHashing;
using System.Collections.Generic;


public static class Utils
{
    public static int ord(char c)
    {
        if (c == '\'')
            return 26;
        if (c == '-')
            return 27;
        return c - 'a';
    }

    // Damerau-Levenshtein distance
    public static int Distance(string p, string s)
    {
        int n = p.Length + 1, m = s.Length + 1;
        int[,] d = new int[n, m];
        int[] cp = new int[FrequencyVector.MaxChars];
        int x, y, cs;
    
        for (int i = 1; i < n; ++i)
            d[i, 0] = i;
        for (int i = 1; i < m; ++i)
            d[0, i] = i;
    
        for (int i = 1; i < n; ++i)
        {
            cs = 0;
            for (int j = 1; j < m; ++j)
            {
                // simple Levenshtein distance
                int k = Min(d[i - 1, j], d[i, j - 1]) + 1;
                d[i, j] = Min(k, d[i - 1, j - 1] + (p[i - 1] == s[j - 1] ? 0 : 1));
    
                // cp[c] stores the largest index x < i such that p[x] = c
                // cs stores the largest index y < j such that s[y] = p[i]
    
                x = cp[ord(s[j - 1])];
                y = cs;
                if (x > 0 && y > 0)
                    d[i - 1, j - 1] = Min(
                        d[i - 1, j - 1],
                        d[x - 1, y - 1] + (i - x) + (j - y) - 1
                    );
                if (p[i - 1] == s[j - 1])
                    cs = j;
            }
    
            cp[ord(p[i - 1])] = i;
        }
    
        return d[n - 1, m - 1];
    }
    
    private static int FCHelper(FrequencyVector x, FrequencyVector y, bool plus)
    {
        int r = 0;
        if (plus)
        {
            for (int i = 0; i < FrequencyVector.MaxChars; ++i)
                if (x[i] > y[i])
                    r += x[i] - y[i];
        }
        else
        {
            for (int i = 0; i < FrequencyVector.MaxChars; ++i)
                if (x[i] < y[i])
                    r += y[i] - x[i];
        }

        return r;
    }

    public static int FC(FrequencyVector x, FrequencyVector y)
        => Max(FCHelper(x, y, true), FCHelper(x, y, false));

    private static int FCHelper(int x, int y)
    {
        // counting the number of bits = 1 in x & ~y
        int r = 0;
        int a = x & ~y;
        for (; a > 0; a >>= 1)
            if ((a & 1) == 1)
                r++;
        return r;
    }
    
    public static int FC(int x, int y)
        => Max(FCHelper(x, y), FCHelper(y, x));

    
    // public static int Distance(string p, string s)
    // {
    //     int n = p.Length + 1, m = s.Length + 1;
    //     int[,] d = new int[n, m];
    //
    //     for (int i = 1; i < n; ++i)
    //         d[i, 0] = i;
    //     for (int i = 1; i < m; ++i)
    //         d[0, i] = i;
    //     
    //     for (int i = 1; i < n; ++i)
    //     for (int j = 1; j < m; ++j)
    //     {
    //         // simple Levenshtein distance
    //         int k = Min(d[i - 1, j], d[i, j - 1]) + 1;
    //         d[i, j] = Min(k, d[i - 1, j - 1] + (p[i - 1] == s[j - 1] ? 0 : 1));
    //             
    //         // considering transpositions
    //         for (int x = 1; x < i; ++x)
    //         for (int y = 1; y < j; ++y)
    //             if (p[i - 1] == s[y - 1] && p[x - 1] == s[j - 1])
    //                 d[i, j] = Min(
    //                     d[i, j],
    //                     d[x - 1, y - 1] + (i - x) + (j - y) - 1
    //                 );
    //     }
    //
    //     return d[n - 1, m - 1];
    // }

    
    public static void FillDictionaries(
        FreqVecDictionary wordsDict,
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
                FrequencyVectorHashing.FrequencyVector vec = 
                    new FrequencyVectorHashing.FrequencyVector(words[0]);
                
                if (wordsDict.ContainsKey(vec))
                    wordsDict[vec].Add(words[0]);
                else
                {
                    wordsDict[vec] = new List<string>();
                    wordsDict[vec].Add(words[0]);
                }
                
                // filling freqDict
                freqDict[words[0]] = double.Parse(words[1]) / freqSum;
            }
    }
    
    public static void FillDictionaries(
        Dictionary<int, List<string>> wordsDict,
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
                int signat = get_signat(words[0]);
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

    public static int get_signat(string s)
    {
        int r = 0;
        foreach (char c in s)
            r |= 1 << ord(c);
        return r;
    }
}