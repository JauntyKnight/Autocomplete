using static System.Console;
using static System.Math;
using System.Collections.Concurrent;
using System.Collections.Generic;
using static Utils.Utils;
using static System.Numerics.BitOperations;


namespace SignatureHashing
{
    public class SignatString
    {
        private string text;
        private int signat;
        
        public static int GetSignat(string s)
        {
            int r = 0;
            foreach (char c in s)
                r |= 1 << Ord(c);
            return r;
        }

        public static bool IsValidString(string s)
        {
            foreach (char c in s)
                if (Ord(c) >= MaxChars || Ord(c) < 0)
                    return false;
            return true;
        }
        
        public SignatString(string t)
        {
            text = t;
            signat = GetSignat(text);
        }

        public int Signature => signat;
        
        public string Text => text;
        
        private static int FCHelper(int x, int y)
        {
            // counting the number of bits = 1 in x & ~y
            return PopCount((uint) (x & ~y));
        }
    
        public static int FC(int x, int y)
            => Max(FCHelper(x, y), FCHelper(y, x));

        private static int Comparator((string, double) p, (string, double) q)
            => q.Item2.CompareTo(p.Item2);
    }


    public class SignatDictionary : Dictionary<int, List<string>>
    {
        private int size = 436450;

        public SignatDictionary()
        {
            EnsureCapacity(size);
        }

        public void Add(string s)
        {
            int signat = SignatString.GetSignat(s);
            if (ContainsKey(signat))
                this[signat].Add(s);
            else
            {
                this[signat] = new List<string>();
                this[signat].Add(s);
            }
        }

        // Fills this dictionary and returns the FrequencyDictionary,
        // to avoid reading the file twice
        public Dictionary<string, double> Fill(string filename)
        {
            // precomputed constant to avoid reading the file twice
            double freqSum = 2293211905f;
            var freqDict = new Dictionary<string, double>();
            using (StreamReader sr = new StreamReader(filename))
                while (sr.ReadLine() is string line)
                {
                    string[] words = line.Trim().Split(null);

                    Add(words[0]);

                    // filling freqDict
                    freqDict[words[0]] = double.Parse(words[1]) / freqSum;
                }

            return freqDict;
        }

        public List<(string, int)> Lookup(SignatString word, int maxDist)
        {
            var mathcesConcurrent = new ConcurrentBag<(string, int)>();

            Parallel.ForEach(Keys, key =>
            {
                if (SignatString.FC(key, word.Signature) <= maxDist)
                {
                    foreach (var s in this[key])
                    {
                        int dist = Distance(word.Text, s);
                        // some absolutely random constants yet
                        if (dist <= maxDist)
                            mathcesConcurrent.Add((s, dist));
                    }
                }
            });

            return mathcesConcurrent.ToList();
        }
    }
}