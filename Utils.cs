using static System.Math;
using System.Collections.Generic;

namespace Utils;

public static class Utils
{
    public const int MaxChars = 28;
    private const int SlashIndex = 26;
    private const int MinusIndex = 27;
    
    public static int ord(char c)
    {
        if (c == '\'')
            return SlashIndex;
        if (c == '-')
            return MinusIndex;
        return c - 'a';
    }

    // Damerau-Levenshtein distance
    public static int Distance(string p, string s)
    {
        int n = p.Length + 1, m = s.Length + 1;
        int[,] d = new int[n, m];
        int[] cp = new int[MaxChars];
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
}