using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClickAuto;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private const int HotkeyId = 1;
    private const int WmHotkey = 0x0312;
    private const int WmKeydown = 0x0100;
    private const int WmSyskeydown = 0x0104;
    private const int WhKeyboardLl = 13;
    private const uint ModAlt = 0x0001;
    private const uint VkW = 0x57;
    private const int VkMenu = 0x12;

    private readonly System.Windows.Forms.Timer clickTimer = new();
    private readonly Label statusLabel = new();
    private readonly Label hintLabel = new();
    private readonly Button toggleButton = new();
    private readonly LowLevelKeyboardProc keyboardProc;
    private IntPtr keyboardHook;
    private DateTime lastToggleAt = DateTime.MinValue;
    private bool isRunning;

    public MainForm()
    {
        Text = "ClickAuto";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(360, 155);
        BackColor = Color.FromArgb(248, 249, 251);

        statusLabel.AutoSize = false;
        statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        statusLabel.Font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold);
        statusLabel.Bounds = new Rectangle(20, 18, 320, 42);

        hintLabel.AutoSize = false;
        hintLabel.TextAlign = ContentAlignment.MiddleCenter;
        hintLabel.Font = new Font("Microsoft YaHei UI", 9.5F);
        hintLabel.ForeColor = Color.FromArgb(84, 92, 105);
        hintLabel.Bounds = new Rectangle(20, 62, 320, 28);
        hintLabel.Text = "快捷键 Alt+W 暂停或开启，每 1 秒点击一次左键";

        toggleButton.Bounds = new Rectangle(105, 102, 150, 34);
        toggleButton.Font = new Font("Microsoft YaHei UI", 9.5F);
        toggleButton.Click += (_, _) => ToggleRunning();

        Controls.Add(statusLabel);
        Controls.Add(hintLabel);
        Controls.Add(toggleButton);

        clickTimer.Interval = 1000;
        clickTimer.Tick += (_, _) => ClickLeftMouseButton();
        clickTimer.Enabled = false;

        keyboardProc = KeyboardHookCallback;
        UpdateStatus();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        if (!RegisterHotKey(Handle, HotkeyId, ModAlt, VkW))
        {
            CornerNotification.Show("Alt+W 注册失败，可用窗口按钮切换。", false);
        }

        keyboardHook = SetKeyboardHook(keyboardProc);
        if (keyboardHook == IntPtr.Zero)
        {
            CornerNotification.Show("游戏热键兜底启用失败。", false);
        }
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        if (keyboardHook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(keyboardHook);
            keyboardHook = IntPtr.Zero;
        }

        UnregisterHotKey(Handle, HotkeyId);
        base.OnHandleDestroyed(e);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == WmHotkey && message.WParam.ToInt32() == HotkeyId)
        {
            ToggleRunning();
            return;
        }

        base.WndProc(ref message);
    }

    private void ToggleRunning()
    {
        if ((DateTime.UtcNow - lastToggleAt).TotalMilliseconds < 250)
        {
            return;
        }

        lastToggleAt = DateTime.UtcNow;
        isRunning = !isRunning;
        clickTimer.Enabled = isRunning;
        UpdateStatus();
        CornerNotification.Show(isRunning ? "自动点击已开启" : "自动点击已暂停", isRunning);
    }

    private void UpdateStatus()
    {
        statusLabel.Text = isRunning ? "正在自动点击" : "已暂停";
        statusLabel.ForeColor = isRunning ? Color.FromArgb(21, 128, 61) : Color.FromArgb(185, 28, 28);
        toggleButton.Text = isRunning ? "暂停" : "开启";
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == WmKeydown || wParam == WmSyskeydown))
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var altPressed = (GetAsyncKeyState(VkMenu) & 0x8000) != 0;

            if (vkCode == VkW && altPressed)
            {
                BeginInvoke(ToggleRunning);
            }
        }

        return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
    }

    private static IntPtr SetKeyboardHook(LowLevelKeyboardProc proc)
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        var moduleHandle = currentModule is null ? IntPtr.Zero : GetModuleHandle(currentModule.ModuleName);
        return SetWindowsHookEx(WhKeyboardLl, proc, moduleHandle, 0);
    }

    private static void ClickLeftMouseButton()
    {
        var inputs = new[]
        {
            Input.Mouse(MouseEventFlags.LeftDown),
            Input.Mouse(MouseEventFlags.LeftUp)
        };

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>());
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint inputCount, Input[] inputs, int inputSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public MouseInput MouseInput;

        public static Input Mouse(MouseEventFlags flags)
        {
            return new Input
            {
                Type = 0,
                MouseInput = new MouseInput
                {
                    Flags = flags
                }
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public MouseEventFlags Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [Flags]
    private enum MouseEventFlags : uint
    {
        LeftDown = 0x0002,
        LeftUp = 0x0004
    }
}

public sealed class CornerNotification : Form
{
    private const int WsExToolwindow = 0x00000080;
    private const int WsExTopmost = 0x00000008;
    private const int WsExNoactivate = 0x08000000;

    private readonly System.Windows.Forms.Timer closeTimer = new();

    private CornerNotification(string text, bool success)
    {
        ShowInTaskbar = false;
        TopMost = true;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(238, 58);
        BackColor = success ? Color.FromArgb(21, 128, 61) : Color.FromArgb(185, 28, 28);
        Opacity = 0.94;

        var label = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = Color.White,
            Text = text,
            TextAlign = ContentAlignment.MiddleCenter
        };

        Controls.Add(label);

        var area = Screen.PrimaryScreen?.WorkingArea ?? SystemInformation.WorkingArea;
        Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18);

        closeTimer.Interval = 1400;
        closeTimer.Tick += (_, _) =>
        {
            closeTimer.Stop();
            Close();
        };
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= WsExToolwindow | WsExTopmost | WsExNoactivate;
            return cp;
        }
    }

    public static void Show(string text, bool success)
    {
        var notification = new CornerNotification(text, success);
        notification.Shown += (_, _) => notification.closeTimer.Start();
        notification.Show();
    }
}
