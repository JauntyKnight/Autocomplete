Simple text editor with autocomplete support for the English language.

It uses a dictionary of most frequently encountered words from the English language extracted from wikipedia, which can be found in this repository:

https://github.com/IlyaSemenov/wikipedia-word-frequency/tree/master/results

In order to add support for a different language, the user shall provide another `dict_freq.txt` file, which would obey the following rules:
1. The first line of the file is an integer representing the total number of distinct words present in the dictionary.
2. The second line is an integer representing the total number of occurrences of all the words present in th dictionary.
3. All the following lines have the form `word n`, separated by space, where `n` is the number of occurrences of the respective word in the sample data.
4. The words shall have only characters in the range `[A-Z]` (the case doesn't matter) or the characters `-` and `'`.

