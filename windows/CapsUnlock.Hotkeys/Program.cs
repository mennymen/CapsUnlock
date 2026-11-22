using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Forms;

namespace CapsUnlock.Hotkeys;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new HotkeyWindow());
    }
}

internal sealed class HotkeyWindow : Form
{
    private const int WmHotkey = 0x0312;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;
    private const int VkAlt = 0x12;
    private const uint ModNoRepeat = 0x4000;

    private readonly NotifyIcon _trayIcon;
    private readonly HashSet<int> _registeredHotkeys = [];
    private List<HotkeyPreference> _preferences = [];

    public HotkeyWindow()
    {
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;

        var menu = new ContextMenuStrip();
        menu.Items.Add("Settings...", null, (_, _) => OpenSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit CapsUnlock", null, (_, _) => Close());
        _trayIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "CapsUnlock hotkeys",
            ContextMenuStrip = menu,
            Visible = true,
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Hide();

        try
        {
            _preferences = HotkeySettingsStore.Load();
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or System.Text.Json.JsonException)
        {
            MessageBox.Show(
                $"Could not load CapsUnlock settings. Default shortcuts will be used.\n\n{exception.Message}",
                "CapsUnlock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            _preferences = HotkeyCatalog.CreateDefaults();
        }

        var conflicts = RegisterHotkeys(_preferences);
        if (conflicts.Count > 0)
        {
            ShowNotice($"Some shortcuts could not be registered: {string.Join(", ", conflicts)}. Open Settings to change them.");
        }
        else
        {
            _trayIcon.ShowBalloonTip(
                3000,
                "CapsUnlock is ready",
                "Select text, then use a shortcut. Right-click the tray icon for Settings.",
                ToolTipIcon.Info);
        }
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey)
        {
            var hotkeyId = message.WParam.ToInt32();
            var definition = HotkeyCatalog.Definitions.FirstOrDefault(
                item => item.Id == hotkeyId);
            var preference = definition is null
                ? null
                : _preferences.FirstOrDefault(item => item.Operation == definition.Operation);
            if (definition is not null && preference is { Enabled: true })
            {
                BeginInvoke(new Action(
                    () => _ = TransformSelectionAfterHotkeyRelease(
                        definition.Operation,
                        char.ToUpperInvariant(preference.Key[0]),
                        HotkeyCatalog.GetModifierFlags(preference.Modifiers))));
            }

            return;
        }

        base.WndProc(ref message);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnregisterAllHotkeys();
        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnFormClosed(e);
    }

    private void TransformSelection(string operation)
    {
        try
        {
            var focusedElement = AutomationElement.FocusedElement;
            if (focusedElement is not null && !IsSupportedTarget(focusedElement))
            {
                ShowNotice("For safety, text replacement is currently supported only in Windows Notepad.");
                return;
            }

            if (focusedElement is null
                || !focusedElement.TryGetCurrentPattern(TextPattern.Pattern, out var patternObject)
                || patternObject is not TextPattern textPattern)
            {
                ShowNotice("This app does not expose selected text for direct transformation.");
                return;
            }

            var selections = textPattern.GetSelection();
            if (selections.Length != 1)
            {
                ShowNotice(selections.Length == 0
                    ? "Select text before using a shortcut."
                    : "This app has multiple selections; select one range at a time.");
                return;
            }

            var selection = selections[0];
            var input = selection.GetText(-1);
            if (input.Length == 0)
            {
                ShowNotice("Select text before using a shortcut.");
                return;
            }

            if (!focusedElement.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObject)
                || valuePatternObject is not ValuePattern valuePattern
                || valuePattern.Current.IsReadOnly)
            {
                ShowNotice("This text field does not support direct text replacement.");
                return;
            }

            var documentRange = textPattern.DocumentRange;
            var documentText = documentRange.GetText(-1);
            var prefixRange = documentRange.Clone();
            prefixRange.MoveEndpointByRange(
                TextPatternRangeEndpoint.End,
                selection,
                TextPatternRangeEndpoint.Start);
            var prefix = prefixRange.GetText(-1);

            var suffixRange = documentRange.Clone();
            suffixRange.MoveEndpointByRange(
                TextPatternRangeEndpoint.Start,
                selection,
                TextPatternRangeEndpoint.End);
            var suffix = suffixRange.GetText(-1);

            if (!string.Equals(prefix + input + suffix, documentText, StringComparison.Ordinal))
            {
                ShowNotice("The app did not expose a consistent text range. No text was changed.");
                return;
            }

            var transformed = TextTransformations.Transform(operation, input);
            var replacement = prefix + transformed + suffix;
            valuePattern.SetValue(replacement);

            try
            {
                var updatedPattern = (TextPattern)focusedElement.GetCurrentPattern(TextPattern.Pattern);
                var updatedSelection = updatedPattern.DocumentRange;
                updatedSelection.MoveEndpointByUnit(
                    TextPatternRangeEndpoint.Start,
                    TextUnit.Character,
                    prefix.Length);
                updatedSelection.MoveEndpointByUnit(
                    TextPatternRangeEndpoint.End,
                    TextUnit.Character,
                    -suffix.Length);
                updatedSelection.Select();
            }
            catch (Exception exception) when (
                exception is ElementNotAvailableException
                    or InvalidOperationException
                    or COMException)
            {
                ShowNotice("Text was transformed, but its selection could not be restored.");
            }
        }
        catch (Exception exception) when (
            exception is ElementNotAvailableException
                or ArgumentException
                or InvalidOperationException
                or IOException
                or TimeoutException
                or Win32Exception
                or COMException)
        {
            ShowNotice($"Could not transform the selection: {exception.Message}");
        }
    }

    private static bool IsSupportedTarget(AutomationElement element)
    {
        var processId = (int)element.GetCurrentPropertyValue(AutomationElement.ProcessIdProperty);
        using var process = Process.GetProcessById(processId);
        return IsSupportedTarget(process.ProcessName, element.Current.ControlType.Id);
    }

    internal static bool IsSupportedTarget(string processName, int controlTypeId)
    {
        return string.Equals(processName, "notepad", StringComparison.OrdinalIgnoreCase)
            && controlTypeId == ControlType.Edit.Id;
    }

    private async Task TransformSelectionAfterHotkeyRelease(
        string operation,
        int hotkey,
        uint modifiers)
    {
        await Task.Delay(100);
        var stopwatch = Stopwatch.StartNew();
        while (IsKeyDown(hotkey) || AreModifiersDown(modifiers))
        {
            if (stopwatch.ElapsedMilliseconds >= 2000)
            {
                ShowNotice("Release Ctrl, Shift, and the shortcut key, then try again.");
                return;
            }

            await Task.Delay(25);
        }

        TransformSelection(operation);
    }

    private bool ApplySettings(IReadOnlyList<HotkeyPreference> updated)
    {
        var previous = _preferences;
        UnregisterAllHotkeys();

        var conflicts = RegisterHotkeys(updated);
        if (conflicts.Count > 0)
        {
            UnregisterAllHotkeys();
            var restoreConflicts = RegisterHotkeys(previous);
            var restoreMessage = restoreConflicts.Count == 0
                ? ""
                : $" Previous shortcuts also could not be restored: {string.Join(", ", restoreConflicts)}.";
            ShowNotice($"Could not register {string.Join(", ", conflicts)}. Settings were not changed.{restoreMessage}");
            return false;
        }

        try
        {
            HotkeySettingsStore.Save(updated);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            UnregisterAllHotkeys();
            var restoreConflicts = RegisterHotkeys(previous);
            var restoreMessage = restoreConflicts.Count == 0
                ? ""
                : $" Previous shortcuts also could not be restored: {string.Join(", ", restoreConflicts)}.";
            ShowNotice($"Could not save settings: {exception.Message}{restoreMessage}");
            return false;
        }

        _preferences = updated.ToList();
        return true;
    }

    private List<string> RegisterHotkeys(IReadOnlyList<HotkeyPreference> preferences)
    {
        var conflicts = new List<string>();
        foreach (var definition in HotkeyCatalog.Definitions)
        {
            var preference = preferences.First(item => item.Operation == definition.Operation);
            if (!preference.Enabled)
            {
                continue;
            }

            var modifiers = HotkeyCatalog.GetModifierFlags(preference.Modifiers) | ModNoRepeat;
            var key = char.ToUpperInvariant(preference.Key[0]);
            if (RegisterHotKey(Handle, definition.Id, modifiers, key))
            {
                _registeredHotkeys.Add(definition.Id);
            }
            else
            {
                conflicts.Add($"{preference.Modifiers}+{key}");
            }
        }

        return conflicts;
    }

    private void UnregisterAllHotkeys()
    {
        foreach (var id in _registeredHotkeys)
        {
            UnregisterHotKey(Handle, id);
        }

        _registeredHotkeys.Clear();
    }

    private void OpenSettings()
    {
        using var dialog = new HotkeySettingsDialog(_preferences);
        if (dialog.ShowDialog(this) == DialogResult.OK
            && dialog.Preferences is not null
            && ApplySettings(dialog.Preferences))
        {
            _trayIcon.ShowBalloonTip(
                2500,
                "CapsUnlock settings saved",
                "Your shortcut changes are active.",
                ToolTipIcon.Info);
        }
    }

    private static bool AreModifiersDown(uint modifiers)
    {
        return ((modifiers & 0x0002) != 0 && IsKeyDown(VkControl))
            || ((modifiers & 0x0004) != 0 && IsKeyDown(VkShift))
            || ((modifiers & 0x0001) != 0 && IsKeyDown(VkAlt));
    }

    private void ShowNotice(string message)
    {
        _trayIcon.ShowBalloonTip(3000, "CapsUnlock", message, ToolTipIcon.Warning);
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window, int id);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int key);

    private static bool IsKeyDown(int key)
    {
        return (GetAsyncKeyState(key) & 0x8000) != 0;
    }
}
