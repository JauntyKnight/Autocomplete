using static System.Math;
using static System.Console;
using System.Collections.Generic;
using System.Collections.Concurrent;
using static Utils;


public class FrequencyVectorHashing
{ 
    public class FrequencyVector
    {
        public const int MaxChars = 28;
        public readonly int[] vec;
        

        public FrequencyVector(string s)
        {
            vec = new int[MaxChars];
            foreach (char c in s)
                vec[ord(c)]++;
        }

        public int this[int index]
        {
            get => vec[index];
        }
        
        public override int GetHashCode()
        {
            int hash = 17;
            for (int i = 0; i < MaxChars; ++i)
                hash = hash * 19 + vec[i];
            return hash;
        }

        private bool EqualsHelper(FrequencyVector a)
        {
            for (int i = 0; i < MaxChars; ++i)
                if (vec[i] != a[i])
                    return false;
            return true;
        }
        
        public override bool Equals(object? obj)
            => obj is FrequencyVector a && EqualsHelper(a);
        
    }

    public class FreqString
    {
        private string text;
        private FrequencyVector vec;

        public FreqString(string t)
        {
            text = t;
            vec = new FrequencyVector(text);
        }

        public FrequencyVector Frequency => vec;

        public string Text => text;
        
        private static int Comparator((string, double) p, (string, double) q)
            => q.Item2.CompareTo(p.Item2);
        
        public void GetSuggestions(
            FreqVecDictionary freqVecDict, Dictionary<string, double> occurencesDict,
            int k, int topOptions
        )
        {
            // k = max admissible edit distance
            ConcurrentBag<int> mt = new ConcurrentBag<int>();
            ConcurrentBag<int> wt = new ConcurrentBag<int>();
            DateTime start = DateTime.Now;
            ConcurrentBag<(string, double)> mathcesConcurrent = new ConcurrentBag<(string, double)>();
            
            Parallel.ForEach(freqVecDict.Keys, key =>
            {
                if (FC(key, vec) <= k)
                {
                    mt.Add(1);
                    foreach (var s in freqVecDict[key])
                    {
                        wt.Add(1);
                        int dist = Distance(Text, s);
                        // some absolutely random constants yet
                        if (dist <= k)
                            mathcesConcurrent.Add((s, Pow(k - dist + 1, 20) * occurencesDict[s]));
                    }
                }
            });
            
            // sorting the mathces
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

    public class FreqVecDictionary : Dictionary<FrequencyVector, List<string>>
    {
        private int size = 436450;

        public FreqVecDictionary()
        {
            EnsureCapacity(size);
        }
    }
}