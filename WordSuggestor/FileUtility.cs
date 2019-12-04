using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Management;

namespace WordSuggestor {
	class ModelStat {
		public Dictionary<string, int> monoGramStat;
		public Dictionary<string, int> biGramStat;
		public Dictionary<string, int> triGramStat;

		public int VocabularySize {
			get {
				if (this.monoGramStat == null) throw new Exception("ModelStat is not initialized yet");
				int output = 0;
				foreach (KeyValuePair<string, int> pair in monoGramStat) {
					if (pair.Key != "*" && pair.Key != "STOP") output += pair.Value;
				}
				return output;
			}
			set { }
		}

		#region CONSTRUCTOR
		public ModelStat(in Dictionary<string, int> monoGramStat, in Dictionary<string, int> biGramStat, in Dictionary<string, int> triGramStat) {
			this.monoGramStat = monoGramStat;
			this.biGramStat = biGramStat;
			this.triGramStat = triGramStat;
		}
		public ModelStat() {
			this.monoGramStat = new Dictionary<string, int>();
			this.biGramStat = new Dictionary<string, int>();
			this.triGramStat = new Dictionary<string, int>();
		}
		#endregion

		public static ModelStat LoadFromFolder(in string path) {
			ModelStat output = new ModelStat();
			if (!Directory.Exists(path)) throw new ArgumentException("ModelStat.LoadFromFolder: specified path does not exist");
			//monoGram
			output.monoGramStat = new Dictionary<string, int>();
			string monoGramText = File.ReadAllText(path + "/monoGram.txt");
			string[] monoGramsWithCount = monoGramText.Split(new char[] { '\n' });
			foreach (string monoGramWithCount in monoGramsWithCount) {
				string[] splitted = monoGramWithCount.Split(new char[] { ':' });
				if (splitted.Length != 2) continue;
				string monoGram = splitted[0];
				int count = int.Parse(splitted[1]);
				output.monoGramStat.Add(monoGram, count);
			}
			//biGram
			output.biGramStat = new Dictionary<string, int>();
			string biGramText = File.ReadAllText(path + "/biGram.txt");
			string[] biGramsWithCount = biGramText.Split(new char[] { '\n' });
			foreach (string biGramWithCount in biGramsWithCount) {
				string[] splitted = biGramWithCount.Split(new char[] { ':' });
				if (splitted.Length != 2) continue;
				string biGram = splitted[0];
				int count = int.Parse(splitted[1]);
				output.biGramStat.Add(biGram, count);
			}
			//triGram
			output.triGramStat = new Dictionary<string, int>();
			string triGramText = File.ReadAllText(path + "/triGram.txt");
			string[] triGramsWithCount = triGramText.Split(new char[] { '\n' });
			foreach (string triGramWithCount in triGramsWithCount) {
				string[] splitted = triGramWithCount.Split(new char[] { ':' });
				if (splitted.Length != 2) continue;
				string triGram = splitted[0];
				int count = int.Parse(splitted[1]);
				output.triGramStat.Add(triGram, count);
			}
			return output;
		}
		public void WriteInFolder(string path) {
			if (!Directory.Exists(path)) throw new ArgumentException();
			//Write monogram
			using (StreamWriter writer = new StreamWriter(path + "/monogram.txt")) {
				foreach (KeyValuePair<string, int> pair in this.monoGramStat)
					writer.WriteLine(pair.Key + ":" + pair.Value);
			}
			//Write bigram
			using (StreamWriter writer = new StreamWriter(path + "/bigram.txt")) {
				foreach (KeyValuePair<string, int> pair in this.biGramStat)
					writer.WriteLine(pair.Key + ":" + pair.Value);
			}
			//Write trigram
			using (StreamWriter writer = new StreamWriter(path + "/trigram.txt")) {
				foreach (KeyValuePair<string, int> pair in this.triGramStat)
					writer.WriteLine(pair.Key + ":" + pair.Value);
			}
		}
		public void DEBUG_WriteOutput() {
			List<string> monoGramKeys = this.monoGramStat.Keys.ToList<string>();
			monoGramKeys.Sort();
			Dictionary<string, int> tempMono = new Dictionary<string, int>();
			foreach (string key in monoGramKeys) tempMono.Add(key, this.monoGramStat[key]);
			this.monoGramStat = tempMono;

			List<string> biGramKeys = this.biGramStat.Keys.ToList<string>();
			biGramKeys.Sort();
			Dictionary<string, int> tempbi = new Dictionary<string, int>();
			foreach (string key in biGramKeys) tempbi.Add(key, this.biGramStat[key]);
			this.biGramStat = tempbi;

			List<string> triGramKeys = this.triGramStat.Keys.ToList<string>();
			triGramKeys.Sort();
			Dictionary<string, int> temptri = new Dictionary<string, int>();
			foreach (string key in triGramKeys) temptri.Add(key, this.triGramStat[key]);
			this.triGramStat = temptri;

			using (StreamWriter writer = new StreamWriter("../../output.txt")) {
				writer.WriteLine("===============MonoGram===================");
				foreach (KeyValuePair<string, int> pair in this.monoGramStat)
					writer.WriteLine(pair.Key + ":" + pair.Value);
				writer.WriteLine("================BiGram===================");
				foreach (KeyValuePair<string, int> pair in this.biGramStat)
					writer.WriteLine(pair.Key + ":" + pair.Value);
				writer.WriteLine("===============TriGram===================");
				foreach (KeyValuePair<string, int> pair in this.triGramStat)
					writer.WriteLine(pair.Key + ":" + pair.Value);
			}
		}

		public int Count(in string input) {
			int gramOrder = input.Split(new char[] { ',' }).Length;
			switch (gramOrder) {
				case 1:
				if (this.monoGramStat.ContainsKey(input)) return this.monoGramStat[input];
				else throw new Exception("ModelStat.Count: \'" + input + "\' was not included in the vocabulary");
				case 2:
				if (this.biGramStat.ContainsKey(input)) return this.biGramStat[input];
				else return 0;
				case 3:
				if (this.triGramStat.ContainsKey(input)) return this.triGramStat[input];
				else return 0;
				default:
				throw new Exception("We can count only up to and including triGrams");
			}
		}

		public double qML(in string input) {
			string[] splitted = input.Split(new char[] { ',' });
			int gramOrder = splitted.Length;
			switch (gramOrder) {
				case 1:
				if (this.monoGramStat.ContainsKey(input)) {
					double neu = this.monoGramStat[input];
					double den = this.monoGramStat.Values.Aggregate((x, y) => x + y);
					return neu / den;
				} else return 0;

				case 2:
				if (this.biGramStat.ContainsKey(input)) {
					double neu = this.biGramStat[input];
					double den = this.biGramStat.Values.Aggregate((x, y) => x + y);
					return neu / den;
				} else return 0;

				case 3:
				if (this.triGramStat.ContainsKey(input)) {
					double neu = this.triGramStat[input];
					double den = this.triGramStat.Values.Aggregate((x, y) => x + y);
					return neu / den;
				} else return 0;

				default:
				throw new Exception("LinearInterpolation/qML: only defined upto trigrams");
			}
		}

		public Dictionary<string, List<string>> CheckSpell(in string input) {
			Dictionary<string, List<string>> output = new Dictionary<string, List<string>>();
			string[] splitted = input.Split(new char[] { ' ', ',' });
			string[] sentence = new string[splitted.Length + 3];
			sentence[0] = "*"; sentence[1] = "*"; sentence[sentence.Length - 1] = "STOP";
			for (int i = 0; i < splitted.Length; i++) sentence[i + 2] = splitted[i];

			for (int i = 2; i < sentence.Length - 1; i++) {
				string context = sentence[i];
				List<string> closeWords = new List<string>();
				int currentMin = int.MaxValue;
				foreach (KeyValuePair<string, int> pair in this.monoGramStat) {
					if (pair.Value < 2) continue;
					int editDistance = EditDistance(context, pair.Key);
					if (editDistance < currentMin) {
						currentMin = editDistance;
						closeWords = new List<string>();
						closeWords.Add(pair.Key);
					} else if (editDistance == currentMin) closeWords.Add(pair.Key);
				}

				Dictionary<string, double> suggestion = new Dictionary<string, double>();
				foreach (string word in closeWords) {
					//Calculate monoGram probability
					double monoGramProb = this.qML(word);

					string biGram = sentence[i - 1] + "," + context;
					double biGramProb = this.qML(biGram);

					string triGram = sentence[i - 2] + sentence[i - 1] + "," + context;
					double triGramProb = this.qML(triGram);

					double probability = 0.3333 * monoGramProb + 0.3333 * biGramProb + 0.3333 * triGramProb;
					suggestion.Add(word, probability);
				}

				var orderedSuggestion = from pair in suggestion orderby pair.Value descending select pair;
				List<string> currentSuggestion = new List<string>();
				foreach (KeyValuePair<string, double> pair in orderedSuggestion)
					currentSuggestion.Add(pair.Key);
				output.Add(context, currentSuggestion);
			}
			return output;
		}

		private int EditDistance(in string s1, in string s2) {
			int[,] m = new int[200, 200];
			char[] cs1 = new char[s1.Length + 1];
			char[] cs2 = new char[s2.Length + 1];
			for (int i = 0; i < s1.Length; i++) cs1[i + 1] = s1.ElementAt(i);
			for (int i = 0; i < s2.Length; i++) cs2[i + 1] = s2.ElementAt(i);
			for (int i = 1; i <= s1.Length; i++) m[i, 0] = i;
			for (int j = 1; j <= s2.Length; j++) m[0, j] = j;
			for (int i = 1; i <= s1.Length; i++) for (int j = 1; j <= s2.Length; j++) {
					int v1, v2, v3, d;
					//if (s1.ElementAt(i) == s2.ElementAt(j)) d = 0;
					if (cs1[i] == cs2[j]) d = 0;
					else d = 1;
					v1 = m[i - 1, j - 1] + d;
					v2 = m[i - 1, j] + 1;
					v3 = m[i, j - 1] + 1;
					m[i, j] = Math.Min(v1, Math.Min(v2, v3));
				}
			return m[s1.Length, s2.Length];
		}

		public List<string> GetWordPrediction(in string input) {
			string[] splitted = input.Split(new char[] { ' ', ',' });
			string lastWord = splitted[splitted.Length - 1];
			string secondLastWord;
			int totalBigrams = this.biGramStat.Values.ToArray().Aggregate(0, (acc, x) => acc + x);
			int totalTrigrams = this.triGramStat.Values.ToArray().Aggregate(0, (acc, x) => acc + x);
			if (splitted.Length == 1) secondLastWord = "*";
			else secondLastWord = splitted[splitted.Length - 2];
			List<string> output = new List<string>();
			Dictionary<string, double> candidate = new Dictionary<string, double>();
			if (!this.monoGramStat.ContainsKey(lastWord)) return output;
			foreach (KeyValuePair<string, int> pair in this.biGramStat) {
				string[] biGramWords = pair.Key.Split(new char[] { ' ', ',' });
				if (lastWord == biGramWords[0] && pair.Value >= 1) {
					candidate.Add(biGramWords[1], (double)pair.Value / (double)totalBigrams);
				}
			}
			Dictionary<string, double> triGramCandidate = new Dictionary<string, double>();
			foreach (KeyValuePair<string, int> pair in this.biGramStat) {
				string[] triGramWords = pair.Key.Split(new char[] { ' ', ',' });
				if (lastWord == triGramWords[1] && secondLastWord == triGramWords[0] && pair.Value >= 1) {
					triGramCandidate.Add(triGramWords[1], pair.Value / (double)totalTrigrams);
				}
			}

			foreach(KeyValuePair<string, double> pair in triGramCandidate){
				if (candidate.ContainsKey(pair.Key)) candidate[pair.Key] += 0.7 * pair.Value + 0.3 * candidate[pair.Key];
				else candidate.Add(pair.Key, 0.7 * pair.Value);
			}

			var orderedCandidate = from pair in candidate orderby pair.Value descending select pair.Key;
			int i = 0;
			foreach(string word in orderedCandidate){
				if (word == "STOP") continue;
				output.Add(word);
				i++;
				if (i == 5) break;
			}
			return output;
		}
	}

	class DataLoader {
		private string folderPath;
		public ModelStat modelStat;

		public DataLoader(string folderPath) {
			this.folderPath = folderPath;
			this.modelStat = new ModelStat();
		}

		public void LoadData() {
			Console.WriteLine("Loading data from: " + this.folderPath);
			this.modelStat.monoGramStat = FileUtility.MonoGramStatsFromFolder(folderPath);
			this.modelStat.biGramStat = FileUtility.BiGramStatsFromFolder(folderPath);
			this.modelStat.triGramStat = FileUtility.TriGramStatsFromFolder(folderPath);
		}
	}

	class FileUtility {
		public static readonly char[] wordSeperators = {
			',',        ';',        ':',        '\'',       '-',        '.',
			'…',        '|',        '`',        '~',        '(',        ')',
			'{',         '}',       '[',        ']',        '-',        '\"',
			'#',        '‘',        '’',        '.',        '\t',       '“',
			'”',        '/',        '\\',       ' ',        '—',
			'\u00A0',   '\u1680',   '\u180E',   '\u2000',   '\u2001',
			'\u2002',   '\u2003',   '\u2004',   '\u2005',   '\u2006',   '\u2007',
			'\u2008',   '\u2009',   '\u200A',   '\u200B',   '\u202F',   '\u205f',
			'\u3000',   '\uFEFF'
		};
		public static readonly char[] sentenceSeperators = { '\n', '।', '?', '!', (char)13, '.' };

		public static List<string> WordsInSentence(in string sentence) {
			string[] splitted = sentence.Split(wordSeperators);
			List<string> output = splitted.Where(split => split.Length > 0).ToList();
			return output;
		}
		public static List<string> SentencesInFile(in string relativeFilePath) {
			try {
				string textContent = File.ReadAllText(relativeFilePath);
				string[] possibleSentences = textContent.Split(sentenceSeperators);
				List<string> output = possibleSentences.Where(split => split.Length > 0).ToList();
				return output;
			} catch (Exception exception) {
				Console.WriteLine("From FileUtility.SentencesInFile: " + exception.Message);
				return new List<string>();
			}
		}
		public static List<string> FilesInFolder(in string path) {
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				FileInfo[] fileInfos = directoryInfo.GetFiles();
				List<string> output = fileInfos.Select(fileInfo => fileInfo.Name).ToList();
				return output;
			} catch (Exception exception) {
				Console.WriteLine("From FilesInFolder: " + exception.Message);
				return new List<string>();
			}
		}
		public static List<string> FoldersInFolder(in string path) {
			try {
				DirectoryInfo directoryInfo = new DirectoryInfo(path);
				DirectoryInfo[] subDirectoryInfos = directoryInfo.GetDirectories();
				List<string> output = subDirectoryInfos.Select(fileInfo => fileInfo.Name).ToList();
				return output;
			} catch (Exception exception) {
				Console.WriteLine("From FilesInFolder: " + exception.Message);
				return new List<string>();
			}
		}

		public static Dictionary<string, int> CombineStats(in Dictionary<string, int> first, in Dictionary<string, int> second) {
			Dictionary<string, int> output = new Dictionary<string, int>(first);
			foreach (KeyValuePair<string, int> row in second) {
				if (output.ContainsKey(row.Key)) output[row.Key] += row.Value;
				else output.Add(row.Key, row.Value);
			}
			return output;
		}

		#region StatCollection
		//Level: Sentence
		public static Dictionary<string, int> MonoGramStatsFromSentence(in string sentence) {
			Dictionary<string, int> output = new Dictionary<string, int>();
			string[] words = WordsInSentence("*,*," + sentence).ToArray();
			string current = "*";
			for (int i = 0; i <= words.Length; i++) {
				if (i == words.Length) current = "STOP";
				else current = words[i];
				if (output.ContainsKey(current)) output[current]++;
				else output.Add(current, 1);
			}
			return output;
		}
		public static Dictionary<string, int> BiGramStatsFromSentence(in string sentence) {
			Dictionary<string, int> output = new Dictionary<string, int>();
			string[] words = WordsInSentence("*," + sentence).ToArray();
			string prev = "*", current = "*";
			for (int i = 0; i <= words.Length; i++) {
				prev = current;
				if (i == words.Length) current = "STOP";
				else current = words[i];
				string joined = prev + "," + current;
				if (output.ContainsKey(joined)) output[joined]++;
				else output.Add(joined, 1);
			}
			return output;
		}
		public static Dictionary<string, int> TriGramStatsFromSentence(in string sentence) {
			Dictionary<string, int> output = new Dictionary<string, int>();
			string[] words = WordsInSentence(sentence).ToArray();
			string prevPrev = "*", prev = "*", current = "*";
			for (int i = 0; i <= words.Length; i++) {
				prevPrev = prev;
				prev = current;
				if (i == words.Length) current = "STOP";
				else current = words[i];
				string joined = prevPrev + "," + prev + "," + current;
				if (output.ContainsKey(joined)) output[joined]++;
				else output.Add(joined, 1);
			}
			return output;
		}
		//Level: File
		public static Dictionary<string, int> MonoGramStatsFromFile(in string path) {
			List<string> sentences = SentencesInFile(path);
			Dictionary<string, int> output = new Dictionary<string, int>();
			foreach (string sentence in sentences) {
				Dictionary<string, int> sentenceStat = MonoGramStatsFromSentence(sentence);
				output = CombineStats(output, sentenceStat);
			}
			return output;
		}
		public static Dictionary<string, int> BiGramStatsFromFile(in string path) {
			List<string> sentences = SentencesInFile(path);
			Dictionary<string, int> output = new Dictionary<string, int>();
			foreach (string sentence in sentences) {
				Dictionary<string, int> sentenceStat = BiGramStatsFromSentence(sentence);
				output = CombineStats(output, sentenceStat);
			}
			return output;
		}
		public static Dictionary<string, int> TriGramStatsFromFile(in string path) {
			List<string> sentences = SentencesInFile(path);
			Dictionary<string, int> output = new Dictionary<string, int>();
			foreach (string sentence in sentences) {
				Dictionary<string, int> sentenceStat = TriGramStatsFromSentence(sentence);
				output = CombineStats(output, sentenceStat);
			}
			return output;
		}
		//Level: Folder
		public static Dictionary<string, int> MonoGramStatsFromFolder(in string path) {
			List<string> files = FilesInFolder(path);
			Dictionary<string, int> output = new Dictionary<string, int>();
			foreach (string file in files) {
				Dictionary<string, int> fileStat = MonoGramStatsFromFile(path + "/" + file);
				output = CombineStats(output, fileStat);
			}
			return output;
		}
		public static Dictionary<string, int> BiGramStatsFromFolder(in string path) {
			List<string> files = FilesInFolder(path);
			Dictionary<string, int> output = new Dictionary<string, int>();
			foreach (string file in files) {
				Dictionary<string, int> fileStat = BiGramStatsFromFile(path + "/" + file);
				output = CombineStats(output, fileStat);
			}
			return output;
		}
		public static Dictionary<string, int> TriGramStatsFromFolder(in string path) {
			List<string> files = FilesInFolder(path);
			Dictionary<string, int> output = new Dictionary<string, int>();
			foreach (string file in files) {
				Dictionary<string, int> fileStat = TriGramStatsFromFile(path + "/" + file);
				output = CombineStats(output, fileStat);
			}
			return output;
		}
		//Level: SuperFolder
		public static ModelStat SeriallyTrainStatsFromCorpusFolder(in string corpusFolder) {
			List<string> testNames = FoldersInFolder(corpusFolder);
			Dictionary<string, int> monoGramStat = new Dictionary<string, int>();
			Dictionary<string, int> biGramStat = new Dictionary<string, int>();
			Dictionary<string, int> triGramStat = new Dictionary<string, int>();
			foreach (string testName in testNames) {
				monoGramStat = CombineStats(monoGramStat, MonoGramStatsFromFolder(corpusFolder + "/" + testName));
				biGramStat = CombineStats(biGramStat, BiGramStatsFromFolder(corpusFolder + "/" + testName));
				triGramStat = CombineStats(triGramStat, TriGramStatsFromFolder(corpusFolder + "/" + testName));
			}
			return new ModelStat(monoGramStat, biGramStat, triGramStat);
		}
		public static ModelStat ParallellyTrainStatsFromCorpusFolder(string corpusFolder) {
			string[] trainSegments = FoldersInFolder(corpusFolder).ToArray();
			Task[] tasks = new Task[trainSegments.Length];
			DataLoader[] dataLoaders = new DataLoader[trainSegments.Length];
			for (int i = 0; i < trainSegments.Length; i++) {
				dataLoaders[i] = new DataLoader(corpusFolder + "/" + trainSegments[i]);
				//Console.WriteLine("Assaigned: " + corpusFolder + "/" + trainSegments[i]);
			}
			Console.WriteLine("Starting parallel tasks...");
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < trainSegments.Length; i++) {
				//Console.WriteLine("Starting data collection from: " + trainSegments[i]);
				DataLoader currentDataLoader = dataLoaders[i];
				Task currentTask = new Task(() => currentDataLoader.LoadData());
				tasks[i] = currentTask;
				currentTask.Start();
			}
			Task.WaitAll(tasks);
			stopwatch.Stop();
			Console.WriteLine("Took " + stopwatch.ElapsedMilliseconds + "ms to collect all data");

			Dictionary<string, int> monoGramStats = new Dictionary<string, int>();
			for (int i = 0; i < trainSegments.Length; i++) monoGramStats = CombineStats(monoGramStats, dataLoaders[i].modelStat.monoGramStat);
			Dictionary<string, int> biGramStats = new Dictionary<string, int>();
			for (int i = 0; i < trainSegments.Length; i++) biGramStats = CombineStats(biGramStats, dataLoaders[i].modelStat.biGramStat);
			Dictionary<string, int> triGramStats = new Dictionary<string, int>();
			for (int i = 0; i < trainSegments.Length; i++) triGramStats = CombineStats(triGramStats, dataLoaders[i].modelStat.triGramStat);

			return new ModelStat(monoGramStats, biGramStats, triGramStats);
		}
		#endregion
	}
}
