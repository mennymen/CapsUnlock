using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
    private const int UppercaseHotkeyId = 1;
    private const int LowercaseHotkeyId = 2;
    private const int InvertCaseHotkeyId = 3;
    private const int TitleCaseHotkeyId = 4;
    private const int WmHotkey = 0x0312;
    private const int VkControl = 0x11;
    private const int VkShift = 0x10;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModNoRepeat = 0x4000;
    private const uint VkU = 0x55;
    private const uint VkL = 0x4C;
    private const uint VkI = 0x49;
    private const uint VkT = 0x54;

    private readonly NotifyIcon _trayIcon;
    private readonly string _cliPath;
    private bool _upperHotkeyRegistered;
    private bool _lowerHotkeyRegistered;
    private bool _invertCaseHotkeyRegistered;
    private bool _titleCaseHotkeyRegistered;

    public HotkeyWindow()
    {
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        _cliPath = FindCliPath();

        var menu = new ContextMenuStrip();
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

        _upperHotkeyRegistered = RegisterHotKey(
            Handle,
            UppercaseHotkeyId,
            ModControl | ModShift | ModNoRepeat,
            VkU);
        _lowerHotkeyRegistered = RegisterHotKey(
            Handle,
            LowercaseHotkeyId,
            ModControl | ModShift | ModNoRepeat,
            VkL);
        _invertCaseHotkeyRegistered = RegisterHotKey(
            Handle,
            InvertCaseHotkeyId,
            ModControl | ModShift | ModNoRepeat,
            VkI);
        _titleCaseHotkeyRegistered = RegisterHotKey(
            Handle,
            TitleCaseHotkeyId,
            ModControl | ModShift | ModNoRepeat,
            VkT);

        if (!_upperHotkeyRegistered
            || !_lowerHotkeyRegistered
            || !_invertCaseHotkeyRegistered
            || !_titleCaseHotkeyRegistered)
        {
            var error = new Win32Exception(Marshal.GetLastWin32Error());
            Close();
            MessageBox.Show(
                $"Could not register the global shortcuts. They may already be in use.\n\n{error.Message}",
                "CapsUnlock",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        _trayIcon.ShowBalloonTip(
            3000,
            "CapsUnlock is ready",
            "Select text and use Ctrl+Shift+U/L/I/T to change its case.",
            ToolTipIcon.Info);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey)
        {
            var operation = message.WParam.ToInt32() switch
            {
                UppercaseHotkeyId => ("uppercase", (int)VkU),
                LowercaseHotkeyId => ("lowercase", (int)VkL),
                InvertCaseHotkeyId => ("invert", (int)VkI),
                TitleCaseHotkeyId => ("title", (int)VkT),
                _ => ((string Operation, int Key)?)null,
            };

            if (operation is not null)
            {
                BeginInvoke(new Action(
                    () => _ = TransformSelectionAfterHotkeyRelease(
                        operation.Value.Operation,
                        operation.Value.Key)));
            }

            return;
        }

        base.WndProc(ref message);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_upperHotkeyRegistered)
        {
            UnregisterHotKey(Handle, UppercaseHotkeyId);
        }

        if (_lowerHotkeyRegistered)
        {
            UnregisterHotKey(Handle, LowercaseHotkeyId);
        }

        if (_invertCaseHotkeyRegistered)
        {
            UnregisterHotKey(Handle, InvertCaseHotkeyId);
        }

        if (_titleCaseHotkeyRegistered)
        {
            UnregisterHotKey(Handle, TitleCaseHotkeyId);
        }

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        base.OnFormClosed(e);
    }

    private void TransformSelection(string operation)
    {
        try
        {
            var focusedElement = AutomationElement.FocusedElement;
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

            var transformed = RunTransformation(operation, input);
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
                or InvalidOperationException
                or IOException
                or TimeoutException
                or Win32Exception
                or COMException)
        {
            ShowNotice($"Could not transform the selection: {exception.Message}");
        }
    }

    private async Task TransformSelectionAfterHotkeyRelease(string operation, int hotkey)
    {
        await Task.Delay(100);
        var stopwatch = Stopwatch.StartNew();
        while (IsKeyDown(VkControl) || IsKeyDown(VkShift) || IsKeyDown(hotkey))
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

    private string RunTransformation(string operation, string input)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "node",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardInputEncoding = Encoding.UTF8,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true,
            },
        };
        process.StartInfo.ArgumentList.Add(_cliPath);
        process.StartInfo.ArgumentList.Add(operation);

        if (!process.Start())
        {
            throw new InvalidOperationException("Could not start Node.js.");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        process.StandardInput.Write(input);
        process.StandardInput.Close();

        if (!process.WaitForExit(10_000))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The text transformation timed out.");
        }

        var output = outputTask.GetAwaiter().GetResult();
        var error = errorTask.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error.Trim());
        }

        return output;
    }

    private void ShowNotice(string message)
    {
        _trayIcon.ShowBalloonTip(3000, "CapsUnlock", message, ToolTipIcon.Warning);
    }

    private static string FindCliPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "cli.js");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            "Could not locate src\\cli.js. Run CapsUnlock from its repository.");
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
