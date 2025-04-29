using Backup_Service.Models;
using Backup_Service.Services;
using System.Text.Json;

namespace Backup_Service.Forms;

/// <summary>
/// Form for configuring the backup service
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
    private Config? config;

    /// <summary>
    /// Initializes a new instance of the ConfigForm class
    /// </summary>
    /// <param name="configPath">Path to the configuration file</param>
    public ConfigForm(string configPath)
    {
        InitializeComponent();
        this.configPath = configPath;
        LoadConfig();
        InitializeControls();
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
        InitializeLabel(lblSource, "Source Path:", 15);
        InitializeLabel(lblTarget, "Target Path:", 45);
        InitializeLabel(lblPolling, "Interval (Sec.):", 105);
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
        InitializeBrowseButton(btnBrowseSource, "Browse", 14, BtnBrowseSource_Click);
        InitializeBrowseButton(btnBrowseTarget, "Browse", 44, BtnBrowseTarget_Click);
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
        btnSave.Text = "Save";
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
        chkAutoStart.Text = "Start with Windows";
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
        Text = "Backup Service Configuration";
        ResumeLayout(false);
        PerformLayout();
    }

    /// <summary>
    /// Loads the configuration from file
    /// </summary>
    private void LoadConfig()
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                config = System.Text.Json.JsonSerializer.Deserialize<Config>(json);
            }
            else
            {
                config = new Config();
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error loading configuration: {ex.Message}");
            MessageBox.Show(
                $"Error loading configuration: {ex.Message}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            config = new Config();
        }
    }

    /// <summary>
    /// Initializes the form controls with configuration values
    /// </summary>
    private void InitializeControls()
    {
        if (config == null) return;

        txtSource.Text = config.Source;
        txtTarget.Text = config.Target;
        txtPolling.Text = config.PollingInS.ToString();
        chkAutoStart.Checked = config.AutoStart;
    }

    /// <summary>
    /// Handles the Browse button click for source directory
    /// </summary>
    private void BtnBrowseSource_Click(object? sender, EventArgs e)
    {
        SelectFolder(txtSource);
    }

    /// <summary>
    /// Handles the Browse button click for target directory
    /// </summary>
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

    /// <summary>
    /// Handles the OK button click
    /// </summary>
    private void btnSave_Click(object? sender, EventArgs e)
    {
        try
        {
            if (!ValidateInput()) return;

            var config = CreateConfigFromForm();
            SaveConfig(config);

            // Update Windows startup setting
            StartupManager.StartWithWindows = config.AutoStart;

            Logger.Log(LogLevel.Information, "Configuration saved successfully");
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
            ShowValidationError("Please enter a valid number for the polling interval.");
            return false;
        }

        if (polling <= 0)
        {
            ShowValidationError("The polling interval must be greater than 0.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtSource.Text))
        {
            ShowValidationError("Please select a source directory.");
            return false;
        }

        if (!Directory.Exists(txtSource.Text))
        {
            ShowValidationError("The source directory does not exist.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(txtTarget.Text))
        {
            ShowValidationError("Please select a target directory.");
            return false;
        }

        if (!Directory.Exists(Path.GetDirectoryName(txtTarget.Text)))
        {
            ShowValidationError("The parent target directory does not exist.");
            return false;
        }

        if (txtSource.Text.Equals(txtTarget.Text, StringComparison.OrdinalIgnoreCase))
        {
            ShowValidationError("Source and target directories must not be identical.");
            return false;
        }

        return true;
    }

    private void ShowValidationError(string message)
    {
        Logger.Log(LogLevel.Warning, $"Validation error: {message}");
        MessageBox.Show(
            message,
            "Validation Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private Config CreateConfigFromForm()
    {
        return new Config
        {
            Source = txtSource.Text.Trim(),
            Target = txtTarget.Text.Trim(),
            AutoStart = chkAutoStart.Checked,
            PollingInS = int.Parse(txtPolling.Text)
        };
    }

    private void SaveConfig(Config config)
    {
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(configPath, json);
    }

    private void HandleConfigSaveError(Exception ex)
    {
        Logger.Log(LogLevel.Error, $"Error saving configuration: {ex.Message}");
        MessageBox.Show(
            $"Error saving configuration: {ex.Message}",
            "Error",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}