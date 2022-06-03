using static System.Console;
using SignatureHashing;
using static Utils.Utils;


static class Program
{
    static void Main()
    {
        // filling the Dictionaries
        DateTime start = DateTime.Now;
        var signatDict = new SignatDictionary();
        var freqDict = signatDict.Fill("dict_freq.txt");
        double elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine($"FreqVector dictionary filled in {elapsed:f1} ms");
        
        WriteLine($"Keys in the Signature dictionary: {signatDict.Keys.Count}");
        
        // testing with data from test_input.txt
        using (StreamReader sr = new StreamReader("test_input.txt"))
            while (sr.ReadLine() is string line)
            {
                string s = line.Trim();
                
                WriteLine($"Recommendations for {s}");
                SignatString word = new SignatString(s);
                foreach (var c in signatDict.Lookup(word, 2))
                    WriteLine(c.Item1);
                WriteLine("================");
            }
    }
}