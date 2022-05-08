using static System.Console;
using static System.Math;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static Utils;


public static class SignatureHashing
{
    public class SignatString
    {
        private string text;
        private int signat;

        public SignatString(string t)
        {
            text = t;
            signat = get_signat(text);
        }

        public string Text => text;

        private static int Comparator((string, double) p, (string, double) q)
            => q.Item2.CompareTo(p.Item2);

        public void GetSuggestions(
            Dictionary<int, List<string>> signatDict, Dictionary<string, double> occurencesDict,
            int k, int topOptions
        )
        {
            // k = max admissible edit distance
            ConcurrentBag<int> mt = new ConcurrentBag<int>();
            ConcurrentBag<int> wt = new ConcurrentBag<int>();
            
            DateTime start = DateTime.Now;
            ConcurrentBag<(string, double)> mathcesConcurrent = new ConcurrentBag<(string, double)>();
            
            Parallel.ForEach(signatDict.Keys, key =>
            {
                if (FC(key, signat) <= k)
                {
                    mt.Add(1);
                    foreach (var s in signatDict[key])
                    {
                        wt.Add(1);
                        int dist = Distance(Text, s);
                        // some absolutely random constants yet
                        if (dist <= k)
                            mathcesConcurrent.Add((s, Pow(k - dist + 1, 20) * occurencesDict[s]));
                    }
                }
            });
    
            List<(string, double)> mathces = mathcesConcurrent.ToList();
            mathces.Sort(Comparator);
            
            double elapsed = (DateTime.Now - start).TotalMilliseconds;
            // Output topOptions
            WriteLine($"Matched keys: {mt.Count}");
            WriteLine($"Processed words: {wt.Count}");
            for (int i = 0; i < topOptions && i < mathces.Count; ++i)
                WriteLine($"{mathces[i].Item1}: {mathces[i].Item2:f3}");
            WriteLine($"Processed suggestions in {elapsed:f1} ms");
        }
    }


    public class SignatDictionary : Dictionary<int, List<string>>
    {
        private int size = 436450;

        public SignatDictionary()
        {
            EnsureCapacity(size);
        }
    }
}