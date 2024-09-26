using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Collections.Generic;
using ImageMagick;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class ScreenRecorderApp : Form
{
    private Button startButton;
    private List<Bitmap> frames;
    private bool isRecording;
    private const int HOTKEY_ID = 1;

    public ScreenRecorderApp()
    {
        startButton = new Button() { Text = "Start Recording", Left = 50, Top = 20, Width = 150 };
        startButton.Click += async (sender, e) => await StartRecording();
        Controls.Add(startButton);

        frames = new List<Bitmap>();

        bool hotkeyRegistered = RegisterHotKey(this.Handle, HOTKEY_ID, MOD_CONTROL | MOD_SHIFT, Keys.S.GetHashCode());

        if (!hotkeyRegistered)
        {
            MessageBox.Show("Failed to register the hotkey Ctrl + Shift + S. It might be in use by another application.", "Hotkey Conflict", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;

    protected override void WndProc(ref Message m)
    {
        const int WM_HOTKEY = 0x0312;
        if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            if (isRecording)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                SetForegroundWindow(this.Handle);
                StopRecording();
            }
        }
        base.WndProc(ref m);
    }

    private async Task StartRecording()
    {
        string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string logFilePath = Path.Combine(userDownloads, "screen_capture_debug_log.txt");
        string gifFilePath = Path.Combine(userDownloads, "screen_recording.gif");

        if (File.Exists(logFilePath)) File.Delete(logFilePath);
        if (File.Exists(gifFilePath)) File.Delete(gifFilePath);

        this.Hide();

        isRecording = true;
        startButton.Enabled = false;

        await Task.Run(() => RecordScreen());
    }

    private async void StopRecording()
    {
        isRecording = false;

        await Task.Delay(100);

        await Task.Run(() => SaveAsGif());

        string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Process.Start("explorer.exe", userDownloads);

        startButton.Enabled = true;
    }

    private void RecordScreen()
    {
        while (isRecording)
        {
            Bitmap screenshot = CaptureScreenWithGDI();
            frames.Add(screenshot);
            Task.Delay(200).Wait();
        }
    }

    private Bitmap CaptureScreenWithGDI()
    {
        string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string logFilePath = Path.Combine(userDownloads, "screen_capture_debug_log.txt");

        using (StreamWriter log = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
        {
            log.WriteLine("GDI Screen Capture Debug Log");
            log.WriteLine($"Timestamp: {DateTime.Now}");

            try
            {
                IntPtr desktopWnd = GetDesktopWindow();
                IntPtr desktopDC = GetWindowDC(desktopWnd);
                Rectangle bounds = Screen.PrimaryScreen.Bounds;

                float scalingFactor = GetScalingFactorForWindow();
                log.WriteLine($"Screen scaling factor: {scalingFactor}");

                bounds.Width = (int)(bounds.Width * scalingFactor);
                bounds.Height = (int)(bounds.Height * scalingFactor);

                log.WriteLine($"Adjusted Screen Width: {bounds.Width}, Screen Height: {bounds.Height}");

                IntPtr memoryDC = CreateCompatibleDC(desktopDC);
                IntPtr bitmap = CreateCompatibleBitmap(desktopDC, bounds.Width, bounds.Height);
                IntPtr oldBitmap = SelectObject(memoryDC, bitmap);

                BitBlt(memoryDC, 0, 0, bounds.Width, bounds.Height, desktopDC, 0, 0, SRCCOPY);
                SelectObject(memoryDC, oldBitmap);

                Bitmap finalBitmap = Image.FromHbitmap(bitmap);

                DeleteObject(bitmap);
                DeleteDC(memoryDC);

                return finalBitmap;
            }
            catch (Exception ex)
            {
                log.WriteLine($"Error: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }

    private void SaveAsGif()
    {
        string userDownloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        string logFilePath = Path.Combine(userDownloads, "screen_capture_debug_log.txt");

        using (StreamWriter log = new StreamWriter(new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)))
        {
            log.WriteLine($"Timestamp: {DateTime.Now} - Starting GIF Conversion");

            string gifPath = Path.Combine(userDownloads, "screen_recording.gif");

            List<Bitmap> framesCopy;

            lock (frames)
            {
                framesCopy = new List<Bitmap>(frames);
            }

            using (MagickImageCollection collection = new MagickImageCollection())
            {
                try
                {
                    foreach (Bitmap frame in framesCopy)
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            frame.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            memoryStream.Position = 0;

                            var img = new MagickImage(memoryStream);
                            img.AnimationDelay = 20;
                            collection.Add(img);
                        }
                    }

                    collection[0].AnimationIterations = 0;
                    collection.Write(gifPath);

                    log.WriteLine($"GIF saved to: {gifPath}");
                }
                catch (Exception ex)
                {
                    log.WriteLine($"Error during GIF saving: {ex.Message}");
                    MessageBox.Show($"An error occurred during GIF saving: {ex.Message}");
                }
            }

            frames.Clear();
        }
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hdc);

    const int SRCCOPY = 0x00CC0020;

    private float GetScalingFactorForWindow()
    {
        IntPtr hWnd = GetDesktopWindow();
        int dpi = GetDpiForWindow(hWnd);
        return dpi / 96.0f;
    }

    [DllImport("user32.dll")]
    private static extern int GetDpiForWindow(IntPtr hWnd);

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        UnregisterHotKey(this.Handle, HOTKEY_ID);
        base.OnFormClosed(e);
    }

    [STAThread]
    static void Main()
    {
        Application.Run(new ScreenRecorderApp());
    }
}
