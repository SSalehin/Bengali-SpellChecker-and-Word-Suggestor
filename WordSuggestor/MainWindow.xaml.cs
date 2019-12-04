using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WordSuggestor {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private ModelStat modelStat;
		
		public MainWindow() {
			//modelStat = ModelStat.LoadFromFolder("../../TrainStats");
			modelStat = FileUtility.ParallellyTrainStatsFromCorpusFolder("../../Corpus");
			modelStat.WriteInFolder("../../TrainStats");
			InitializeComponent();
		}

		void CheckSpell (string input){
			if (input == string.Empty) return;
			string[] splitted = input.Split(new char[] { ' ', ',', ';', '-' });
			SpellCheck.Text = "";
			foreach (string inputWord in splitted) {
				Dictionary<string, List<string>> spellCheckReasult = this.modelStat.CheckSpell(inputWord);
				foreach (KeyValuePair<string, List<string>> pair in spellCheckReasult) {
					SpellCheck.Text += ">" + pair.Key + ":\n";
					foreach (string word in pair.Value) SpellCheck.Text += word + "\n";
				}
			}
		}

		private void SpellCheckButton_Click(object sender, RoutedEventArgs e) {
			string input = Input.Text;
			CheckSpell(input);
		}

		private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key.Equals(System.Windows.Input.Key.Space)) {
				List<string> suggestions = modelStat.GetWordPrediction(Input.Text);
				Suggestion.Text = "";
				foreach (string word in suggestions) Suggestion.Text += word + "\n";
			}
		}
	}

	
}
