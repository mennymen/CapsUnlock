using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace CapsUnlock.Hotkeys;

internal sealed record HotkeyDefinition(
    int Id,
    string Operation,
    string DisplayName,
    char DefaultKey);

internal sealed class HotkeyPreference
{
    public string Operation { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Modifiers { get; set; } = "Ctrl+Shift";
    public string Key { get; set; } = "";
}

internal static class HotkeyCatalog
{
    public static IReadOnlyList<HotkeyDefinition> Definitions { get; } =
    [
        new(1, "uppercase", "UPPERCASE", 'U'),
        new(2, "lowercase", "lowercase", 'L'),
        new(3, "invert", "Invert case", 'I'),
        new(4, "title", "Title Case", 'T'),
        new(5, "sentence", "Sentence case", 'S'),
        new(6, "camel", "camelCase", 'C'),
        new(7, "pascal", "PascalCase", 'P'),
        new(8, "snake", "snake_case", 'N'),
        new(9, "kebab", "kebab-case", 'K'),
        new(10, "trim", "Trim selection", 'R'),
        new(11, "collapse-whitespace", "Collapse whitespace", 'W'),
        new(12, "remove-blank-lines", "Remove blank lines", 'B'),
    ];

    public static List<HotkeyPreference> CreateDefaults()
    {
        return Definitions.Select(definition => new HotkeyPreference
        {
            Operation = definition.Operation,
            Key = definition.DefaultKey.ToString(),
        }).ToList();
    }

    public static uint GetModifierFlags(string modifiers)
    {
        return modifiers switch
        {
            "Ctrl+Shift" => 0x0002 | 0x0004,
            "Ctrl+Alt" => 0x0002 | 0x0001,
            "Alt+Shift" => 0x0001 | 0x0004,
            "Ctrl+Alt+Shift" => 0x0002 | 0x0001 | 0x0004,
            _ => throw new ArgumentOutOfRangeException(nameof(modifiers), modifiers, "Unknown modifier combination."),
        };
    }

    public static bool IsValidPreference(string? modifiers, string? key)
    {
        return (modifiers is "Ctrl+Shift"
                or "Ctrl+Alt"
                or "Alt+Shift"
                or "Ctrl+Alt+Shift")
            && key is { Length: 1 }
            && char.IsAsciiLetter(key[0]);
    }
}

internal static class HotkeySettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CapsUnlock",
        "settings.json");

    public static List<HotkeyPreference> Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return HotkeyCatalog.CreateDefaults();
        }

        var saved = JsonSerializer.Deserialize<List<HotkeyPreference>>(
            File.ReadAllText(SettingsPath),
            JsonOptions);
        if (saved is null)
        {
            throw new InvalidDataException($"CapsUnlock settings are empty: {SettingsPath}");
        }

        var preferences = HotkeyCatalog.CreateDefaults();
        foreach (var item in saved)
        {
            var preference = preferences.FirstOrDefault(
                candidate => candidate.Operation == item.Operation);
            if (preference is null)
            {
                continue;
            }

            if (item.Key is not { Length: 1 }
                || !char.IsAsciiLetter(item.Key[0])
                || !IsSupportedModifier(item.Modifiers))
            {
                throw new InvalidDataException($"Invalid CapsUnlock shortcut settings: {SettingsPath}");
            }

            preference.Enabled = item.Enabled;
            preference.Modifiers = item.Modifiers;
            preference.Key = item.Key.ToUpperInvariant();
        }

        return preferences;
    }

    public static void Save(IReadOnlyCollection<HotkeyPreference> preferences)
    {
        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("Could not determine the settings directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = SettingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preferences, JsonOptions));
        File.Move(temporaryPath, SettingsPath, overwrite: true);
    }

    private static bool IsSupportedModifier(string modifiers)
    {
        return modifiers is "Ctrl+Shift"
            or "Ctrl+Alt"
            or "Alt+Shift"
            or "Ctrl+Alt+Shift";
    }
}

internal sealed class HotkeySettingsDialog : Form
{
    private readonly DataGridView _grid;

    public List<HotkeyPreference>? Preferences { get; private set; }

    public HotkeySettingsDialog(IReadOnlyList<HotkeyPreference> preferences)
    {
        Text = "CapsUnlock shortcuts";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(630, 440);

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
        };
        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "On",
            FillWeight = 35,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Operation",
            HeaderText = "Transformation",
            ReadOnly = true,
            FillWeight = 115,
        });
        var modifiersColumn = new DataGridViewComboBoxColumn
        {
            Name = "Modifiers",
            HeaderText = "Modifiers",
            FillWeight = 120,
            FlatStyle = FlatStyle.Flat,
        };
        modifiersColumn.Items.AddRange("Ctrl+Shift", "Ctrl+Alt", "Alt+Shift", "Ctrl+Alt+Shift");
        _grid.Columns.Add(modifiersColumn);
        var keyColumn = new DataGridViewComboBoxColumn
        {
            Name = "Key",
            HeaderText = "Key",
            FillWeight = 45,
            FlatStyle = FlatStyle.Flat,
        };
        keyColumn.Items.AddRange(Enumerable.Range('A', 26).Select(value => (object)((char)value).ToString()).ToArray());
        _grid.Columns.Add(keyColumn);

        foreach (var definition in HotkeyCatalog.Definitions)
        {
            var preference = preferences.First(item => item.Operation == definition.Operation);
            _grid.Rows.Add(
                preference.Enabled,
                definition.DisplayName,
                preference.Modifiers,
                preference.Key);
        }

        var saveButton = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.None,
            AutoSize = true,
        };
        saveButton.Click += (_, _) => SavePreferences();
        var cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
        };

        var instructions = new Label
        {
            Text = "Enable the shortcuts you want. Choose modifiers and a letter for each one.",
            Dock = DockStyle.Top,
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 5, 0, 0),
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        Controls.Add(_grid);
        Controls.Add(instructions);
        Controls.Add(buttons);
        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void SavePreferences()
    {
        _grid.EndEdit();
        var result = new List<HotkeyPreference>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            var enabled = row.Cells["Enabled"].Value is true;
            var modifiers = row.Cells["Modifiers"].Value?.ToString();
            var key = row.Cells["Key"].Value?.ToString();
            if (!HotkeyCatalog.Definitions.Any(definition => definition.DisplayName == row.Cells["Operation"].Value?.ToString())
                || !HotkeyCatalog.IsValidPreference(modifiers, key))
            {
                MessageBox.Show(
                    this,
                    "Choose a modifier combination and a letter for every shortcut.",
                    "Invalid shortcut",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            result.Add(new HotkeyPreference
            {
                Operation = HotkeyCatalog.Definitions[result.Count].Operation,
                Enabled = enabled,
                Modifiers = modifiers!,
                Key = key!.ToUpperInvariant(),
            });
        }

        var duplicate = result
            .Where(item => item.Enabled)
            .GroupBy(item => $"{item.Modifiers}|{item.Key}", StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            MessageBox.Show(
                this,
                $"The shortcut {duplicate.Key.Replace("|", " + ", StringComparison.Ordinal)} is assigned more than once.",
                "Duplicate shortcut",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        Preferences = result;
        DialogResult = DialogResult.OK;
        Close();
    }
}
