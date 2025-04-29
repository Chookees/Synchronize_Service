using Backup_Service.Models;
using Backup_Service.Services;
using System.Text.Json;

namespace Backup_Service.Forms;

/// <summary>
/// Formular zur Konfiguration des Backup-Services
/// </summary>
public partial class ConfigForm : Form
{
    private const int FORM_WIDTH = 488;
    private const int FORM_HEIGHT = 175;
    private const int LABEL_WIDTH = 80;
    private const int TEXTBOX_WIDTH = 300;
    private const int BUTTON_WIDTH = 70;
    private const int VERTICAL_SPACING = 30;
    private const int HORIZONTAL_SPACING = 12;

    private readonly string configPath;
    private TextBox txtSource = new();
    private TextBox txtTarget = new();
    private CheckBox chkAutoStart = new();
    private TextBox txtPolling = new();
    private Button btnSave = new();
    private Button btnBrowseSource = new();
    private Button btnBrowseTarget = new();
    private Label lblSource = new();
    private Label lblTarget = new();
    private Label lblPolling = new();

    public ConfigForm(string configPath)
    {
        InitializeComponent();
        this.configPath = configPath;
        LoadConfig();
    }

    private void InitializeComponent()
    {
        InitializeLabels();
        InitializeTextboxes();
        InitializeButtons();
        InitializeForm();
    }

    private void InitializeLabels()
    {
        InitializeLabel(lblSource, "Quellpfad:", 15);
        InitializeLabel(lblTarget, "Zielpfad:", 45);
        InitializeLabel(lblPolling, "Intervall (Sek.):", 105);
    }

    private void InitializeLabel(Label label, string text, int top)
    {
        label.Location = new System.Drawing.Point(HORIZONTAL_SPACING, top);
        label.Size = new System.Drawing.Size(LABEL_WIDTH, 20);
        label.Text = text;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void InitializeTextboxes()
    {
        InitializeTextbox(txtSource, "txtSource", 15);
        InitializeTextbox(txtTarget, "txtTarget", 45);
        InitializeTextbox(txtPolling, "txtPolling", 105, 100);
    }

    private void InitializeTextbox(TextBox textbox, string name, int top, int width = TEXTBOX_WIDTH)
    {
        textbox.Location = new System.Drawing.Point(100, top);
        textbox.Size = new System.Drawing.Size(width, 20);
        textbox.Name = name;
    }

    private void InitializeButtons()
    {
        InitializeBrowseButton(btnBrowseSource, "Durchsuchen", 14, BtnBrowseSource_Click);
        InitializeBrowseButton(btnBrowseTarget, "Durchsuchen", 44, BtnBrowseTarget_Click);
        InitializeSaveButton();
    }

    private void InitializeBrowseButton(Button button, string text, int top, EventHandler clickHandler)
    {
        button.Location = new System.Drawing.Point(406, top);
        button.Size = new System.Drawing.Size(BUTTON_WIDTH, 22);
        button.Text = text;
        button.Click += clickHandler;
    }

    private void InitializeSaveButton()
    {
        btnSave.Location = new System.Drawing.Point(HORIZONTAL_SPACING, 140);
        btnSave.Size = new System.Drawing.Size(FORM_WIDTH - (2 * HORIZONTAL_SPACING), 23);
        btnSave.Text = "Speichern";
        btnSave.Click += btnSave_Click;
    }

    private void InitializeForm()
    {
        InitializeCheckbox();
        AddControls();
        ConfigureForm();
    }

    private void InitializeCheckbox()
    {
        chkAutoStart.Location = new System.Drawing.Point(100, 75);
        chkAutoStart.Size = new System.Drawing.Size(200, 20);
        chkAutoStart.Text = "Start mit Windows";
        chkAutoStart.Name = "chkAutoStart";
    }

    private void AddControls()
    {
        Controls.AddRange(new Control[] {
                lblSource, txtSource, btnBrowseSource,
                lblTarget, txtTarget, btnBrowseTarget,
                chkAutoStart, lblPolling, txtPolling,
                btnSave
            });
    }

    private void ConfigureForm()
    {
        ClientSize = new System.Drawing.Size(FORM_WIDTH, FORM_HEIGHT);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ConfigForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Backup Service Konfiguration";
        ResumeLayout(false);
        PerformLayout();
    }

    private void LoadConfig()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var config = LoadConfigFromFile();
                if (config != null)
                {
                    ApplyConfigToForm(config);
                }
            }
        }
        catch (Exception ex)
        {
            HandleConfigLoadError(ex);
        }
    }

    private Config? LoadConfigFromFile()
    {
        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<Config>(json);
    }

    private void ApplyConfigToForm(Config config)
    {
        txtSource.Text = config.Source;
        txtTarget.Text = config.Target;
        chkAutoStart.Checked = config.AutoStart;
        txtPolling.Text = config.PollingInS.ToString();
    }

    private void HandleConfigLoadError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Fehler beim Laden der Konfiguration: {ex.Message}");
        MessageBox.Show(
            $"Fehler beim Laden der Konfiguration: {ex.Message}",
            "Fehler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void BtnBrowseSource_Click(object? sender, EventArgs e)
    {
        SelectFolder(txtSource);
    }

    private void BtnBrowseTarget_Click(object? sender, EventArgs e)
    {
        SelectFolder(txtTarget);
    }

    private void SelectFolder(TextBox targetTextBox)
    {
        using var dialog = new FolderBrowserDialog();
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            targetTextBox.Text = dialog.SelectedPath;
        }
    }

    private void btnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!ValidateInput()) return;

            var config = CreateConfigFromForm();
            SaveConfig(config);

            // Update Windows startup setting
            StartupManager.StartWithWindows = config.AutoStart;

            Logger.Log(LogLevel.Information, "Konfiguration erfolgreich gespeichert");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            HandleConfigSaveError(ex);
        }
    }

    private bool ValidateInput()
    {
        if (!int.TryParse(txtPolling.Text, out int polling))
        {
            ShowValidationError("Bitte geben Sie eine gültige Zahl für das Polling-Intervall ein.");
            return false;
        }

        if (polling <= 0)
        {
            ShowValidationError("Das Polling-Intervall muss größer als 0 sein.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtSource.Text))
        {
            ShowValidationError("Bitte wählen Sie ein Quellverzeichnis aus.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtTarget.Text))
        {
            ShowValidationError("Bitte wählen Sie ein Zielverzeichnis aus.");
            return false;
        }

        return true;
    }

    private void ShowValidationError(string message)
    {
        MessageBox.Show(message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private Config CreateConfigFromForm()
    {
        return new Config
        {
            Source = txtSource.Text,
            Target = txtTarget.Text,
            AutoStart = chkAutoStart.Checked,
            PollingInS = int.Parse(txtPolling.Text)
        };
    }

    private void SaveConfig(Config config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configPath, json);
    }

    private void HandleConfigSaveError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Fehler beim Speichern der Konfiguration: {ex.Message}");
        MessageBox.Show(
            $"Fehler beim Speichern der Konfiguration: {ex.Message}",
            "Fehler",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}