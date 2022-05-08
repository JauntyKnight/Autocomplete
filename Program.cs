using static System.Console;
using static SignatureHashing;
using static FrequencyVectorHashing;
using static Utils;


static class Program
{
    static void Main()
    {
        // filling the FreqVector Dictionary
        DateTime start = DateTime.Now;
        FreqVecDictionary freqVecDict = new FreqVecDictionary();
        Dictionary<string, double> occurencesDict = new Dictionary<string, double>();
        FillDictionaries(freqVecDict, occurencesDict,"dict_freq.txt");
        double elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine($"FreqVector dictionary filled in {elapsed:f1} ms");

        // filling the Signature Dictionary
        start = DateTime.Now;
        SignatDictionary signatDict = new SignatDictionary();
        occurencesDict = new Dictionary<string, double>();
        FillDictionaries(signatDict, occurencesDict,"dict_freq.txt");
        elapsed = (DateTime.Now - start).TotalMilliseconds;
        WriteLine($"SignatDictionary filled in {elapsed:f1} ms");
        
        WriteLine($"Keys in the FrequencyVector dictionary: {freqVecDict.Keys.Count}");
        WriteLine($"Keys in the Signature dictionary: {signatDict.Keys.Count}");
        
        // testing with data from test_input.txt
        using (StreamReader sr = new StreamReader("test_input.txt"))
            while (sr.ReadLine() is string line)
            {
                string s = line.Trim();
                
                FreqString freqWord = new FreqString(s);
                WriteLine($"Suggestions for {s}");
                WriteLine("Using frequency vector:");
                freqWord.GetSuggestions(freqVecDict, occurencesDict, 2, 10);
                
                WriteLine("Using signature:");
                SignatString signatWord = new SignatString(s);
                signatWord.GetSuggestions(signatDict, occurencesDict, 2, 10);
                WriteLine("================");
            }
    }
}