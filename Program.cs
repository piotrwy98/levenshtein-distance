using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

using Catalyst;
using Mosaik.Core;

namespace LevenshteinDistance
{
    internal class Program
    {
        const string INPUT_FILE_DEFAULT = "przyklad.txt";
        const string OUTPUT_FILE_DICTIONARY = "slownik.txt";
        const string OUTPUT_FILE_MODIFIED = "przyklad_z_bledami.txt";
        const string OUTPUT_FILE_FIXED = "przyklad_poprawiony.txt";

        static void Main(string[] args)
        {
            var words = ReadFile(args.Length > 0 ? args[0] : INPUT_FILE_DEFAULT);

            if (words == null)
            {
                return;
            }

            var wordsOriginal = words.ToList();

            File.WriteAllLines(OUTPUT_FILE_DICTIONARY, words);

            MakeMistakes(words);

            File.WriteAllLines(OUTPUT_FILE_MODIFIED, words);

            CorrectMistakes(words, wordsOriginal);

            File.WriteAllLines(OUTPUT_FILE_FIXED, words);

            FindMistakes(words, wordsOriginal);
        }

        static List<string> ReadFile(string path)
        {
            if (path == null)
            {
                Console.WriteLine("No file path provided");
                return null;
            }

            if (!File.Exists(path))
            {
                Console.WriteLine($"File '{path}' doesn't exist");
                return null;
            }

            var content = string.Empty;

            try
            {
                content = File.ReadAllText(path);
            }
            catch
            {
                Console.WriteLine($"Cannot read file '{path}'");
                return null;
            }

            Catalyst.Models.English.Register();
            Storage.Current = new DiskStorage("catalyst-models");

            var words = new List<string>();
            var document = new Document(content, Language.English);
            var nlp = Pipeline.For(Language.English);

            nlp.ProcessSingle(document);

            foreach (var sentence in document)
            {
                foreach (var word in sentence)
                {
                    if (word.POS != PartOfSpeech.PUNCT)
                    {
                        words.Add(word.Value);
                    }
                }
            }

            return words;
        }

        static void MakeMistakes(List<string> words)
        {
            var random = new Random();

            for (int i = 0; i < words.Count; i++)
            {
                // losowanie 20% słów
                if (random.Next(5) == 0)
                {
                    // losowanie liczby modyfikacji (od 1 do 3)
                    var numberOfModifications = random.Next(1, 4);
                    var currentModificationIndex = 0;

                    while (currentModificationIndex < numberOfModifications)
                    {
                        // losowanie typu modyfikacji
                        var typeofModification = random.Next(3);

                        // losowanie indexu litery, której będzie dotyczyła modyfikacja
                        var letterIndex = random.Next(words[i].Length);

                        // losowanie litery do zamiany/dodania
                        var newLetterIndex = random.Next(26);
                        var newLetter = (char)('a' + newLetterIndex);

                        switch (typeofModification)
                        {
                            // zamiana litery
                            case 0:
                                var stringBuilder = new StringBuilder(words[i]);
                                stringBuilder[letterIndex] = newLetter;
                                words[i] = stringBuilder.ToString();
                                break;

                            // usunięcie litery
                            case 1:
                                // nie pozwalaj na usunięcie litery, jeśli została tylko jedna
                                if (words[i].Length == 1)
                                {
                                    // wylosuj inną modyfikację
                                    currentModificationIndex--;
                                }
                                else
                                {
                                    words[i] = words[i].Remove(letterIndex, 1);
                                }
                                break;

                            // dodanie litery
                            case 2:
                                // losowanie czy litera ma być dodana przed czy po indexie
                                var afterIndex = random.Next(2);
                                letterIndex += afterIndex;

                                words[i] = words[i].Substring(0, letterIndex)
                                    + newLetter + words[i].Substring(letterIndex);
                                break;
                        }

                        currentModificationIndex++;
                    }
                }
            }
        }

        static void CorrectMistakes(List<string> words, List<string> wordsOriginal)
        {
            for (int i = 0; i < words.Count; i++)
            {
                if (words[i] != wordsOriginal[i])
                {
                    var minDistance = int.MaxValue;
                    var newWord = string.Empty;

                    for (int j = 0; j < wordsOriginal.Count; j++)
                    {
                        var distance = LevenshteinDistance(words[i], wordsOriginal[j]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            newWord = wordsOriginal[i];
                        }

                        if (distance == 0)
                        {
                            break;
                        }
                    }

                    words[i] = newWord;
                }
            }
        }

        static int LevenshteinDistance(string source, string target)
        {
            if (string.IsNullOrEmpty(source))
            {
                if (string.IsNullOrEmpty(target))
                {
                    return 0;
                }

                return target.Length;
            }

            if (string.IsNullOrEmpty(target))
            {
                return source.Length;
            }

            if (source.Length > target.Length)
            {
                var temp = target;
                target = source;
                source = temp;
            }

            var m = target.Length;
            var n = source.Length;
            var distance = new int[2, m + 1];

            for (var j = 1; j <= m; j++)
            {
                distance[0, j] = j;
            }

            var currentRow = 0;

            for (var i = 1; i <= n; ++i)
            {
                currentRow = i & 1;
                distance[currentRow, 0] = i;
                var previousRow = currentRow ^ 1;

                for (var j = 1; j <= m; j++)
                {
                    var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                    distance[currentRow, j] = Math.Min(Math.Min(
                                distance[previousRow, j] + 1,
                                distance[currentRow, j - 1] + 1),
                                distance[previousRow, j - 1] + cost);
                }
            }

            return distance[currentRow, m];
        }

        static void FindMistakes(List<string> words, List<string> wordsOriginal)
        {
            var mistakes = new List<Tuple<string, string>>();

            for (int i = 0; i < words.Count; i++)
            {
                if (words[i] != wordsOriginal[i])
                {
                    mistakes.Add(new Tuple<string, string>(words[i], wordsOriginal[i]));
                }
            }

            Console.WriteLine($"Znalezionych błędów: {mistakes.Count}");

            foreach (var mistake in mistakes)
            {
                Console.WriteLine($"{mistake.Item1} [ZNALEZIONY] <> {mistake.Item2} [ORYGINAŁ]");
            }
        }
    }
}
