using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon notifyIcon;
    private ClockForm clockForm;
    private Timer hoverCheckTimer;
    private DateTime lastHoverTime;
    private Point lastMousePosition;
    private string settingsFilePath = "settings.txt";

    public TrayApplicationContext()
    {
        // Create Clock Form
        clockForm = new ClockForm();

        // Load Settings
        LoadSettings();

        // Create Context Menu
        ContextMenuStrip contextMenu = new ContextMenuStrip();
        
        ToolStripMenuItem adjustItem = new ToolStripMenuItem("Adjust");
        adjustItem.Click += AdjustItem_Click;
        contextMenu.Items.Add(adjustItem);

        ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += ExitItem_Click;
        contextMenu.Items.Add(exitItem);

        // Create NotifyIcon
        notifyIcon = new NotifyIcon();
        // Create NotifyIcon
        notifyIcon = new NotifyIcon();
        try
        {
            if (File.Exists("app_icon.ico"))
            {
                notifyIcon.Icon = new Icon("app_icon.ico");
            }
            else if (File.Exists("app_icon.jpg"))
            {
                // Fallback: Convert JPG to Icon on the fly
                using (Bitmap bmp = new Bitmap("app_icon.jpg"))
                {
                    // Create an HIcon from the Bitmap
                    // Note: This creates a temporary handle. Ideally we should destroy it, 
                    // but for a single app instance startup, it's acceptable.
                    IntPtr hIcon = bmp.GetHicon();
                    notifyIcon.Icon = Icon.FromHandle(hIcon);
                }
            }
            else
            {
                notifyIcon.Icon = SystemIcons.Application;
            }
        }
        catch
        {
            notifyIcon.Icon = SystemIcons.Application;
        } 
        notifyIcon.ContextMenuStrip = contextMenu;
        notifyIcon.Visible = true;
        notifyIcon.ContextMenuStrip = contextMenu;
        notifyIcon.Visible = true;
        notifyIcon.Text = ""; // explicitly empty to avoid tooltip

        // Events
        notifyIcon.MouseMove += NotifyIcon_MouseMove;
        
        // Timer to handle "Pseudo-MouseLeave" logic
        hoverCheckTimer = new Timer();
        hoverCheckTimer.Interval = 200; // Check every 200ms
        hoverCheckTimer.Tick += HoverCheckTimer_Tick;
        hoverCheckTimer.Start();
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(settingsFilePath))
            {
                string[] lines = File.ReadAllLines(settingsFilePath);
                if (lines.Length > 0)
                {
                    string zoneId = lines[0].Trim();
                    try {
                        TimeZoneInfo zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
                        clockForm.DisplayedTimeZone = zone;
                    } catch { } // Keep default if fail
                }
                if (lines.Length > 1)
                {
                    clockForm.ClockName = lines[1].Trim();
                }
                else
                {
                    clockForm.ClockName = "Clock";
                }
            }
        }
        catch
        {
            // If failed to load, defaults are already set in ClockForm
        }
    }

    // Save: Line 1 = ZoneID, Line 2 = ClockName
    private void SaveSettings(string zoneId, string clockName)
    {
        try
        {
            File.WriteAllLines(settingsFilePath, new string[] { zoneId, clockName });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Failed to save settings: " + ex.Message);
        }
    }

    private void AdjustItem_Click(object sender, EventArgs e)
    {
        using (SettingsForm sf = new SettingsForm(clockForm.DisplayedTimeZone.Id, clockForm.ClockName))
        {
            if (sf.ShowDialog() == DialogResult.OK)
            {
                string newZoneId = sf.SelectedTimeZoneId;
                string newName = sf.SelectedClockName;
                try
                {
                    TimeZoneInfo newZone = TimeZoneInfo.FindSystemTimeZoneById(newZoneId);
                    
                    // Update Clock Form
                    clockForm.DisplayedTimeZone = newZone;
                    clockForm.ClockName = newName;
                    clockForm.Invalidate(); // Redraw
                    
                    // Save
                    SaveSettings(newZoneId, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error setting time zone: " + ex.Message);
                }
            }
        }
    }

    private void NotifyIcon_MouseMove(object sender, MouseEventArgs e)
    {
        // Update last hover status
        lastHoverTime = DateTime.Now;
        lastMousePosition = Cursor.Position;

        if (!clockForm.Visible)
        {
            // Smart positioning relative to the Working Area (excludes taskbar)
            Rectangle workingArea = Screen.GetWorkingArea(Cursor.Position);
            
            // Default position: slightly above and centered on cursor
            int x = Cursor.Position.X - clockForm.Width / 2;
            int y = Cursor.Position.Y - clockForm.Height - 10;

            // Adjust if outside working area (which usually means we are in the tray)
            // If the cursor is below the bottom of the working area, the taskbar is at the bottom.
            if (Cursor.Position.Y >= workingArea.Bottom)
            {
                y = workingArea.Bottom - clockForm.Height - 5;
            }
            // If the cursor is to the right of the working area, taskbar is right.
            else if (Cursor.Position.X >= workingArea.Right)
            {
                x = workingArea.Right - clockForm.Width - 5;
                // Align y to center on cursor or keep it safe
                y = Cursor.Position.Y - clockForm.Height / 2;
            }
            // If the cursor is to the left, taskbar is left
            else if (Cursor.Position.X <= workingArea.Left)
            {
                x = workingArea.Left + 5;
                y = Cursor.Position.Y - clockForm.Height / 2;
            }
            // If the cursor is above, taskbar is top
            else if (Cursor.Position.Y <= workingArea.Top)
            {
                y = workingArea.Top + 5;
            }

            // Final clamp to ensure it's fully on screen (within working area)
            if (x < workingArea.Left) x = workingArea.Left + 5;
            if (x + clockForm.Width > workingArea.Right) x = workingArea.Right - clockForm.Width - 5;
            if (y < workingArea.Top) y = workingArea.Top + 5;
            if (y + clockForm.Height > workingArea.Bottom) y = workingArea.Bottom - clockForm.Height - 5;

            clockForm.Location = new Point(x, y);
            clockForm.Show();
            
            // Bring to front to ensure it's visible
            clockForm.TopMost = true;
        }
    }

    private void HoverCheckTimer_Tick(object sender, EventArgs e)
    {
        if (clockForm.Visible)
        {
            // If mouse has moved significantly from the last hover position, hide the window.
            // This allows the user to hold the mouse still on the icon without the window continually hiding (timeout).
            // Distance threshold: 50 pixels (approx icon size + margin)
            
            Point currentPos = Cursor.Position;
            double distance = Math.Sqrt(Math.Pow(currentPos.X - lastMousePosition.X, 2) + Math.Pow(currentPos.Y - lastMousePosition.Y, 2));

            if (distance > 50)
            {
               // Also check if mouse is OVER the clock form itself. If so, keep it open.
               // We use Bounds.Contains. Note Bounds are screen coordinates.
               if (!clockForm.Bounds.Contains(currentPos))
               {
                   clockForm.Hide();
               }
            }
        }
    }

    private void ExitItem_Click(object sender, EventArgs e)
    {
        notifyIcon.Visible = false;
        clockForm.Close();
        Application.Exit();
    }
}
