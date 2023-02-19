using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Collections;
using System.Diagnostics;

namespace ytb_dl
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Initializes the window and sets up some default values
        public MainWindow()
        {
            InitializeComponent();
            string downloadsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            textBoxOut.Text = downloadsPath;

            textBox_CreatGhost(textBoxUrl, "Paste YouTube Link...");

            vidListBox.ItemsSource = Constants.checkedListBoxVid;
            audListBox.ItemsSource = Constants.checkedListBoxAud;

            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1;
        }

        // Setup and return a process object for command line execution
        private Process setupProcess()
        {
            var process = new Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = false;
            return process;
        }

        // Retrieve the details of a given url and generate a string array containing them
        private async Task<string[]> getDetailsAsync(string url)
        {
            using (var process = setupProcess())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = @"/c yt-dlp -F " + url;
                process.Start();

                string strOutput = await process.StandardOutput.ReadToEndAsync();
                
                process.WaitForExit();

                string[] result = strOutput.Split('\n');
                string[] trimmedResult = result.Skip(6).ToArray();
                return trimmedResult;
            }
        }

        // Get the title of a video at a given url
        private async Task<string> getTitle(string url)
        {
            using (var process = setupProcess())
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = @"/c yt-dlp -e " + url;

                process.Start();

                string strOutput = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                return strOutput;
            }
        }


        // Run any command line command provided (Not useful if output is required)
        private async Task<int> runCommand(string command)
        {
            using (var process = setupProcess())
            {

                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = @"/c " + command;

                // Start the process
                process.Start();

                // Read the output and error streams asynchronously
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Wait for the process to exit
                var exitCode = await Task.Run(() =>
                {
                    process.WaitForExit();
                    return process.ExitCode;
                });

                // Get the output and error strings
                var output = await outputTask;
                var error = await errorTask;

                // Log any errors to the console
                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine($"Error running command: {error}");
                }

                return exitCode;
            }
        }

        // Convert a file that has been produced via a job into an mp3 file using ffmpeg
        private async Task<int> convertToMP3(string[] arg)
        {
            string outputFilename = $"{arg[5]}{arg[6]}({arg[2]}).mp3";
            string inputFilename = $"{arg[5]}{arg[6]}({arg[2]}).{arg[3]}";
            string command = $"ffmpeg -i \"{inputFilename}\" -c:v copy -c:a libmp3lame -q:a 4 \"{outputFilename}\"";
            command = command.Replace("\n", "");
            return await runCommand(command);
        }

        // Run a job that exists in the jobs constant
        private async Task<int> runJob(string[] arg)
        {

            string argFormat = arg[0] + "+" + arg[2];
            string argFormatAudOnly = arg[2];
            string argURL = arg[4];
            string command = "";


            if (arg[7] == "True")
            {
                command = $"yt-dlp -P \"{textBoxOut.Text}\" -o %(title)s({argFormatAudOnly}).%(ext)s -f {argFormatAudOnly} {argURL}";

                await runCommand(command);
                return await convertToMP3(arg);
            }
            else
            {
                command = $"yt-dlp -P \"{textBoxOut.Text}\" -o %(title)s({argFormat}).%(ext)s -f {argFormat} {argURL}";
                return await runCommand(command);
            }

            
        }

        // Update the jobs ListBox to reflect the real state of the jobs constant
        private void updateJobList()
        {
            listBoxJobs.Items.Clear();
            foreach (string[] s in Constants.jobs)
            {
                listBoxJobs.Items.Add($"{s[6]} | {s[0]}+{s[2]}{s[7]}");
            }
            Constants.totaljobs = Constants.jobs.Count();
        }

        // Sanatizes a given string removing clutter
        private string sanatizeDetails(string input)
        {
            string s = Regex.Replace(input, @"\s+", " ");
            s = Regex.Replace(s, @"\d+k https", "");
            s = Regex.Replace(s, @"(video|audio) only", "");
            return s;
        }

        // Creates "ghosted" text in a given box
        private void textBox_CreatGhost(TextBox tb, string text)
        {

            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = text;
                tb.Foreground = Brushes.Gray;
            }
            
        }

        // Erased "ghosted" text from a given box
        private void textBox_EraseGhost(TextBox tb, string text)
        {
            if (tb.Text == text)
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
            }
        }

        // Handles ghosted text for text boxes
        private void textBoxUrl_GotFocus(object sender, RoutedEventArgs e)
        {
            textBox_EraseGhost(textBoxUrl, "Paste YouTube Link...");
        }
        private void textBoxUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            textBox_CreatGhost(textBoxUrl, "Paste YouTube Link...");
        }

        // Toggle whether the user is allowed to change values
        private void toggleInput(bool state)
        {
            buttonDl.IsEnabled = state;
            buttonAdd.IsEnabled = state;
            buttonCheck.IsEnabled = state;
            buttonRemove.IsEnabled = state;
            vidListBox.IsEnabled = state & !checkAudOnly.IsChecked ?? true;
            audListBox.IsEnabled = state;
            textBoxOut.IsEnabled = state;
            textBoxUrl.IsEnabled = state;
        }

        // Handles the Check Link button, gets the url information and then adds format options to the video and audio format lists so a user can select them
        private async void buttonCheck_Click(object sender, RoutedEventArgs e)
        {
            Constants.checkedListBoxVid.Clear();
            Constants.checkedListBoxAud.Clear();

            Constants.link = textBoxUrl.Text;

            string[] result = await getDetailsAsync(Constants.link);
            string[] audioStrings = result.Where(s => s.Contains("audio")).ToArray();
            string[] videoStrings = result.Where(s => s.Contains("video")).ToArray();
            Array.Reverse(audioStrings);
            Array.Reverse(videoStrings);

            foreach (string s in videoStrings)
            {
                string regS = sanatizeDetails(s);
                Constants.checkedListBoxVid.Add(new CheckBoxItem { Name = regS });
            }
            if (Constants.checkedListBoxVid.Count > 0)
            {
                Constants.checkedListBoxVid[0].IsChecked = true;
            }

            foreach (string s in audioStrings)
            {
                string regS = sanatizeDetails(s);
                Constants.checkedListBoxAud.Add(new CheckBoxItem { Name = regS });
            }
            if (Constants.checkedListBoxAud.Count > 0)
            {
                Constants.checkedListBoxAud[0].IsChecked = true;
            }
        }

        // Handles the Add button, using the selected video and audio format this constructs a 'job' and adds it to the jobs constant
        private async void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            string checkedVid = Constants.checkedListBoxVid.FirstOrDefault(item => item.IsChecked).Name;
            string checkedAud = Constants.checkedListBoxAud.FirstOrDefault(item => item.IsChecked).Name;

            string[] jobVid = checkedVid.Split(' ');
            string[] jobAud = checkedAud.Split(' ');
            string title = await getTitle(Constants.link);
            string path = textBoxOut.Text;
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            string[] arg = new string[] { jobVid[0], jobVid[1], jobAud[0], jobAud[1], Constants.link, path, title.Replace("\n", ""), checkAudOnly.IsChecked.ToString() };
            bool exists = false;

            foreach (string[] job in Constants.jobs)
            {
                if (arg.SequenceEqual(job))
                {
                    exists = true;
                    break;
                }
            }
            if (!exists)
            {
                Constants.jobs.Add(arg);
            }
            updateJobList();
        }

        // Handles the Remove button, removes any user selected item in the jobs list box from the jobs constant
        private void buttonRemove_Click(object sender, RoutedEventArgs e)
        {
            IList selectedItems = listBoxJobs.SelectedItems;

            foreach (string item in selectedItems)
            {
                foreach (string[] arg in Constants.jobs.ToList())
                {
                    if ($"{arg[6]} | {arg[0]}+{arg[2]}{arg[7]}" == item)
                    {
                        Constants.jobs.Remove(arg);
                    }
                }
                    
            }

            updateJobList();
        }

        // Handles the Download button, recurses through the jobs constant calling the runJob method for each job. It also records total progress and prompts the user when all jobs are complete.
        private async void buttonDl_Click(object sender, RoutedEventArgs e)
        {
            toggleInput(false);
            int completedJobs = 0;
            progressBar1.Value = 0;
            progressBar1.Maximum = Constants.totaljobs * 2;

            foreach (string[] arg in Constants.jobs.ToList())
            {
                completedJobs++;
                progressBar1.Value = completedJobs;
                await runJob(arg);
                Constants.jobs.Remove(arg);
                completedJobs++;
                progressBar1.Value = completedJobs;
                updateJobList();
            }
            toggleInput(true);
            MessageBox.Show($"{progressBar1.Value / 2} jobs completed.", "Download Complete");
            progressBar1.Value = 0;
        }

        // Disables the Video Format box if audio only is enabled
        private void checkAudOnly_Checked(object sender, RoutedEventArgs e)
        {
            vidListBox.IsEnabled = false;
        }

        // Enables the Video Format box if audio only is enabled
        private void checkAudOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            vidListBox.IsEnabled = true;
        }
    }
}
