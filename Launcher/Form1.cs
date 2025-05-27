using System;
using System.Net;
using System.Windows.Forms;
using System.IO;
using System.Linq;

namespace Launcher
{
    public partial class Form1 : Form
    {
        private Button btnStart;
        private Button btnUpdate;
        private RichTextBox txtOutput;

        private const string newestVersionUrl = "https://raw.githubusercontent.com/SpacingKosmopan/FoT/main/newest_version.txt";
        private const string baseUpdateDescrUrl = "https://raw.githubusercontent.com/SpacingKosmopan/FoT/main/"; // zakładam, że pliki 2.0.txt, 2.1.txt są w repo w głównym katalogu

        private const string localVersionFile = "version.txt";

        private string localVersion = "";
        private string remoteVersion = "";

        public Form1()
        {
            this.Text = "Four of Them RPG: Installer";
            this.Size = new System.Drawing.Size(500, 300);

            btnStart = new Button()
            {
                Text = "Start",
                Left = 20,
                Top = 20,
                Width = 100
            };
            btnStart.Click += BtnStart_Click;

            btnUpdate = new Button()
            {
                Text = "Aktualizuj",
                Left = 140,
                Top = 20,
                Width = 100,
                Enabled = false
            };
            btnUpdate.Click += BtnUpdate_Click;

            txtOutput = new RichTextBox()
            {
                Left = 20,
                Top = 60,
                Width = 440,
                Height = 180,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            this.Controls.Add(btnStart);
            this.Controls.Add(btnUpdate);
            this.Controls.Add(txtOutput);

            LoadLocalVersion();
            CheckForUpdate();
        }

        private void LoadLocalVersion()
        {
            try
            {
                if (File.Exists(localVersionFile))
                {
                    localVersion = File.ReadAllText(localVersionFile).Trim();
                }
                else
                {
                    localVersion = ""; // brak pliku lokalnego
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd przy odczycie lokalnej wersji: " + ex.Message);
            }
        }

        private string DownloadTextFromUrl(string url)
        {
            using (WebClient client = new WebClient())
            {
                // Dodaj nagłówek do omijania cache
                client.Headers.Add("Cache-Control", "no-cache");
                client.Headers.Add("Pragma", "no-cache");

                return client.DownloadString(url).Trim();
            }
        }

        private string GetUpdateDescription(string version)
        {
            string path = $"{version}.txt";
            if (File.Exists(path))
                return File.ReadAllText(path) + "\n";
            else
                return $"Brak opisu aktualizacji dla wersji {version}\n";
        }

        private void CheckForUpdate()
        {
            try
            {
                remoteVersion = DownloadTextFromUrl(newestVersionUrl);
                UpdateLocalDescription(remoteVersion);

                txtOutput.Clear();

                if (string.IsNullOrEmpty(localVersion))
                {
                    // Nie ma wersji lokalnej, traktujemy to jak brak aktualizacji
                    AppendColoredText($"Brak lokalnej wersji.\n", System.Drawing.Color.Red);
                    AppendColoredText($"Dostępna jest nowa aktualizacja!\n", System.Drawing.Color.Red);
                    txtOutput.AppendText(GetUpdateDescription(remoteVersion));
                    btnUpdate.Enabled = true;
                }
                else
                {
                    int cmp = CompareVersions(remoteVersion, localVersion);
                    if (cmp > 0) // tylko jeśli remote jest nowsza od lokalnej
                    {
                        // Mamy starszą wersję lokalną
                        txtOutput.AppendText($"Wersja {localVersion} (stara)\n");
                        AppendColoredText($"Dostępna jest nowa aktualizacja!\n", System.Drawing.Color.Red);
                        txtOutput.AppendText(remoteVersion + " ");                 // linia z numerem nowej wersji
                        txtOutput.AppendText(GetUpdateDescription(remoteVersion));   // opis nowej wersji
                        btnUpdate.Enabled = true;
                    }
                    else
                    {
                        // Wersja jest aktualna
                        txtOutput.AppendText($"Wersja {localVersion}\n");
                        txtOutput.AppendText(GetUpdateDescription(localVersion));
                        btnUpdate.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd przy sprawdzaniu aktualizacji: " + ex.Message);
                btnUpdate.Enabled = false;
            }
        }

        private void UpdateLocalDescription(string version)
        {
            string urlDesc = $"https://raw.githubusercontent.com/SpacingKosmopan/FoT/main/{version}.txt";
            string localDescPath = $"{version}.txt";

            string remoteDesc;
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Cache-Control", "no-cache");
                    client.Headers.Add("Pragma", "no-cache");
                    remoteDesc = client.DownloadString(urlDesc).Trim();
                }
            }
            catch (Exception ex)
            {
                // Jeśli nie uda się pobrać, po prostu zakończ (można logować)
                return;
            }

            string localDesc = "";
            if (File.Exists(localDescPath))
                localDesc = File.ReadAllText(localDescPath).Trim();

            if (localDesc != remoteDesc)
            {
                File.WriteAllText(localDescPath, remoteDesc);
            }
        }

        private void AppendColoredText(string text, System.Drawing.Color color)
        {
            txtOutput.SelectionStart = txtOutput.TextLength;
            txtOutput.SelectionLength = 0;
            txtOutput.SelectionColor = color;
            txtOutput.AppendText(text);
            txtOutput.SelectionColor = txtOutput.ForeColor;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                // Tu możesz zaimplementować co "Start" ma robić — np. otworzyć plik wersji:
                if (File.Exists(localVersionFile))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                    {
                        FileName = localVersionFile,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show("Plik lokalny nie istnieje.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas otwierania pliku: " + ex.Message);
            }
        }

        // Zwraca:
        // 1 jeśli verA > verB (verA jest nowsza)
        // 0 jeśli verA == verB
        // -1 jeśli verA < verB (verA jest starsza)
        int CompareVersions(string verA, string verB)
        {
            Version vA, vB;

            // Jeśli jedna z wersji nie ma formatu x.y.z – dopełnij zerami
            if (verA.Count(c => c == '.') == 1) verA += ".0";
            if (verB.Count(c => c == '.') == 1) verB += ".0";

            if (!Version.TryParse(verA, out vA) || !Version.TryParse(verB, out vB))
                return 0; // Jeśli format jest błędny, zakładamy równość

            return vA.CompareTo(vB);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                // Pobierz najnowszą wersję i zapisz do pliku lokalnego
                string newVersion = DownloadTextFromUrl(newestVersionUrl);
                File.WriteAllText(localVersionFile, newVersion);
                localVersion = newVersion;

                // Odśwież opis i stan przycisku
                txtOutput.Clear();
                txtOutput.AppendText($"Aktualizacja zakończona. Nowa wersja: {localVersion}\n");
                txtOutput.AppendText(GetUpdateDescription(localVersion));
                btnUpdate.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas aktualizacji: " + ex.Message);
            }
        }
    }
}