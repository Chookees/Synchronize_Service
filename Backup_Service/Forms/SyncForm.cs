using Backup_Service.Services;
using System.Text.Json;

namespace Backup_Service.Forms;

/// <summary>
/// Form for displaying and managing files to be synchronized
/// </summary>
public class SyncForm : Form
{
    private const string COMPANY_NAME = "AZDev";
    private const string APP_NAME = "Backup_Service";
    private const string IGNORED_FILES_FILE = "ignored_files.json";
    private const int FORM_WIDTH = 800;
    private const int FORM_HEIGHT = 500;
    private const int BUTTON_PANEL_HEIGHT = 40;
    private const int BUTTON_WIDTH = 150;
    private const int BUTTON_MARGIN = 10;

    private readonly DataGridView dataGridView;
    private readonly Button btnSelectAll;
    private readonly Button btnSync;
    private readonly string ignoredFilesPath;
    private readonly List<SyncItem> syncItems;
    private readonly string sourceRoot;
    private readonly string targetRoot;

    /// <summary>
    /// Represents a file to be synchronized
    /// </summary>
    public class SyncItem
    {
        public string FileName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public bool Selected { get; set; } = true;
        public DateTime LastModified { get; set; }
    }

    /// <summary>
    /// Represents an ignored file
    /// </summary>
    public class IgnoredFile
    {
        public string FileName { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public bool PermanentlyIgnored { get; set; }
    }

    public SyncForm(List<dynamic> differences, string sourceRoot, string targetRoot)
    {
        this.sourceRoot = sourceRoot;
        this.targetRoot = targetRoot;

        // Initialize fields
        dataGridView = new DataGridView();
        btnSelectAll = new Button();
        btnSync = new Button();
        syncItems = new List<SyncItem>();
        ignoredFilesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            COMPANY_NAME,
            APP_NAME,
            IGNORED_FILES_FILE);

        InitializeComponent();
        LoadIgnoredFiles();
        InitializeData(differences);
        UpdateSyncButtonText();
    }

    private void InitializeComponent()
    {
        ConfigureForm();
        InitializeDataGridView();
        InitializeButtons();
        SetupLayout();
        SetupEvents();
    }

    private void ConfigureForm()
    {
        Text = "Synchronization";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(FORM_WIDTH, FORM_HEIGHT);
    }

    private void InitializeDataGridView()
    {
        dataGridView.Dock = DockStyle.Fill;
        dataGridView.AllowUserToAddRows = false;
        dataGridView.AllowUserToDeleteRows = false;
        dataGridView.AllowUserToResizeRows = false;
        dataGridView.MultiSelect = false;
        dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        AddDataGridViewColumns();
    }

    private void AddDataGridViewColumns()
    {
        dataGridView.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Selected",
            HeaderText = "Select",
            Width = 50
        });

        dataGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "FileName",
            HeaderText = "File",
            ReadOnly = true
        });

        dataGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Source",
            HeaderText = "From",
            ReadOnly = true
        });

        dataGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Target",
            HeaderText = "To",
            ReadOnly = true
        });

        dataGridView.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Reason",
            HeaderText = "Reason",
            ReadOnly = true
        });
    }

    private void InitializeButtons()
    {
        InitializeSyncButton();
        InitializeSelectAllButton();
    }

    private void InitializeSyncButton()
    {
        btnSync.Text = "Synchronize all";
        btnSync.Width = BUTTON_WIDTH;
        btnSync.Click += BtnSync_Click;
    }

    private void InitializeSelectAllButton()
    {
        btnSelectAll.Text = "Select all";
        btnSelectAll.Width = BUTTON_WIDTH;
        btnSelectAll.Margin = new Padding(0, 0, BUTTON_MARGIN, 0);
        btnSelectAll.Click += BtnSelectAll_Click;
    }

    private void SetupLayout()
    {
        var buttonPanel = CreateButtonPanel();
        Controls.Add(dataGridView);
        Controls.Add(buttonPanel);
    }

    private FlowLayoutPanel CreateButtonPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = BUTTON_PANEL_HEIGHT,
            Padding = new Padding(5)
        };

        panel.Controls.Add(btnSync);
        panel.Controls.Add(btnSelectAll);

        return panel;
    }

    private void SetupEvents()
    {
        dataGridView.CellValueChanged += DataGridView_CellValueChanged;
        dataGridView.CurrentCellDirtyStateChanged += DataGridView_CurrentCellDirtyStateChanged;
    }

    private void LoadIgnoredFiles()
    {
        EnsureIgnoredFilesFileExists();
    }

    private void EnsureIgnoredFilesFileExists()
    {
        if (!File.Exists(ignoredFilesPath))
        {
            File.WriteAllText(ignoredFilesPath, "[]");
        }
    }

    private void InitializeData(List<dynamic> differences)
    {
        var ignoredFiles = LoadIgnoredFilesList();

        foreach (var diff in differences)
        {
            if (ShouldSkipFile(diff, ignoredFiles)) continue;

            AddSyncItem(diff);
        }

        PopulateDataGridView();
    }

    private bool ShouldSkipFile(dynamic diff, List<IgnoredFile> ignoredFiles)
    {
        var fileName = Path.GetFileName(diff.Source);
        var lastModified = File.GetLastWriteTime(diff.Source);
        var ignoredFile = ignoredFiles.FirstOrDefault(f => f.FileName == fileName);

        return ignoredFile != null &&
               (ignoredFile.PermanentlyIgnored || ignoredFile.LastModified == lastModified);
    }

    private void AddSyncItem(dynamic diff)
    {
        syncItems.Add(new SyncItem
        {
            FileName = Path.GetFileName(diff.Source),
            Source = diff.Source,
            Target = diff.Target,
            Reason = diff.Action.Replace("Source", "From").Replace("Target", "To"),
            Selected = true,
            LastModified = File.GetLastWriteTime(diff.Source)
        });
    }

    private void PopulateDataGridView()
    {
        dataGridView.Rows.Clear();
        dataGridView.Rows.Add(syncItems.Count);

        for (int i = 0; i < syncItems.Count; i++)
        {
            var item = syncItems[i];
            UpdateDataGridViewRow(i, item);
        }
    }

    private void UpdateDataGridViewRow(int rowIndex, SyncItem item)
    {
        dataGridView.Rows[rowIndex].Cells["Selected"].Value = item.Selected;
        dataGridView.Rows[rowIndex].Cells["FileName"].Value = item.FileName;
        UpdatePathCells(rowIndex, item);
        dataGridView.Rows[rowIndex].Cells["Reason"].Value = item.Reason;
    }

    private void UpdatePathCells(int rowIndex, SyncItem item)
    {
        if (item.Reason.Contains("From -> To"))
        {
            UpdateSourceToTargetPaths(rowIndex, item);
        }
        else if (item.Reason.Contains("To -> From"))
        {
            UpdateTargetToSourcePaths(rowIndex, item);
        }
        else
        {
            ClearPathCells(rowIndex);
        }
    }

    private void UpdateSourceToTargetPaths(int rowIndex, SyncItem item)
    {
        UpdatePathCell(rowIndex, "Source", sourceRoot, item.Source, "From");
        UpdatePathCell(rowIndex, "Target", targetRoot, item.Target, "To");
    }

    private void UpdateTargetToSourcePaths(int rowIndex, SyncItem item)
    {
        UpdatePathCell(rowIndex, "Source", targetRoot, item.Source, "To");
        UpdatePathCell(rowIndex, "Target", sourceRoot, item.Target, "From");
    }

    private void UpdatePathCell(int rowIndex, string columnName, string root, string fullPath, string prefix)
    {
        var relPath = Path.GetDirectoryName(Path.GetRelativePath(root, fullPath)) ?? string.Empty;
        dataGridView.Rows[rowIndex].Cells[columnName].Value =
            string.IsNullOrEmpty(relPath) ? prefix : $"{prefix}/{relPath.Replace("\\", "/")}";
    }

    private void ClearPathCells(int rowIndex)
    {
        dataGridView.Rows[rowIndex].Cells["Source"].Value = "";
        dataGridView.Rows[rowIndex].Cells["Target"].Value = "";
    }

    private List<IgnoredFile> LoadIgnoredFilesList()
    {
        try
        {
            if (!File.Exists(ignoredFilesPath))
            {
                return new List<IgnoredFile>();
            }

            var json = File.ReadAllText(ignoredFilesPath);
            return JsonSerializer.Deserialize<List<IgnoredFile>>(json) ?? new List<IgnoredFile>();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error loading ignored files: {ex.Message}");
            return new List<IgnoredFile>();
        }
    }

    private void SaveIgnoredFiles(List<IgnoredFile> ignoredFiles)
    {
        try
        {
            var json = JsonSerializer.Serialize(ignoredFiles, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(ignoredFilesPath, json);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error saving ignored files: {ex.Message}");
            ShowError("Error", "Error saving ignored files.");
        }
    }

    private void ShowError(string title, string message)
    {
        Logger.Log(LogLevel.Error, $"{title}: {message}");
        MessageBox.Show(
            message,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private void BtnSelectAll_Click(object? sender, EventArgs e)
    {
        try
        {
            var allSelected = syncItems.All(item => item.Selected);
            foreach (var item in syncItems)
            {
                item.Selected = !allSelected;
            }
            UpdateDataGridView();
            UpdateSyncButtonText();
        }
        catch (Exception ex)
        {
            ShowError("Error", $"Error selecting files: {ex.Message}");
        }
    }

    private void UpdateDataGridView()
    {
        for (int i = 0; i < syncItems.Count; i++)
        {
            UpdateDataGridViewRow(i, syncItems[i]);
        }
    }

    private void DataGridView_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        if (dataGridView.CurrentCell is DataGridViewCheckBoxCell)
        {
            dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void DataGridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex >= 0 && e.ColumnIndex == 0)
        {
            syncItems[e.RowIndex].Selected = (bool)dataGridView.Rows[e.RowIndex].Cells[0].Value;
            UpdateSyncButtonText();
        }
    }

    private void UpdateSyncButtonText()
    {
        var selectedCount = syncItems.Count(item => item.Selected);
        btnSync.Text = selectedCount == syncItems.Count
            ? "Synchronize all"
            : $"Synchronize {selectedCount} of {syncItems.Count}";
    }

    private void BtnSync_Click(object? sender, EventArgs e)
    {
        try
        {
            var selectedItems = GetSelectedItems();
            if (selectedItems.Count == 0)
            {
                ShowError("Warning", "Please select at least one file.");
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            ShowError("Error", $"Error starting synchronization: {ex.Message}");
        }
    }

    public List<SyncItem> GetSelectedItems()
    {
        return syncItems.Where(item => item.Selected).ToList();
    }
}