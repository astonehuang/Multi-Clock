using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class ClockForm : Form
{
    private System.Windows.Forms.Timer updateTimer;

    public ClockForm()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.Manual;
        this.Size = new Size(150, 150);
        this.DoubleBuffered = true;
        this.TopMost = true;
        this.BackColor = Color.White;
        
        // Timer to update the clock
        updateTimer = new System.Windows.Forms.Timer();
        // Set background to LightGray to blend with the outer ring edges.
        this.BackColor = Color.LightGray;
        // Removing TransparencyKey to avoid anti-aliasing artifacts (purple dots).
        // The Region handles the circular shape.
        this.BackColor = Color.Gray;
        
        // Initial init
        updateTimer = new System.Windows.Forms.Timer();
        updateTimer.Interval = 1000; // Update every second
        updateTimer.Tick += (s, e) => this.Invalidate();
        updateTimer.Start();
        DisplayedTimeZone = TimeZoneInfo.Local;
        ClockName = "Local";
    }

    public TimeZoneInfo DisplayedTimeZone { get; set; }
    public string ClockName { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.HighQuality;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        int w = this.ClientSize.Width;
        int h = this.ClientSize.Height;
        int cx = w / 2;
        int cy = h / 2;
        int radius = Math.Min(cx, cy) - 2;

        // Create a circular path for the clock
        using (GraphicsPath path = new GraphicsPath())
        {
            path.AddEllipse(cx - radius, cy - radius, radius * 2, radius * 2);
            // Set the window region to this circle to effectively make corners transparent
            this.Region = new Region(path);
        }

        // 0. Draw Dark Gray Outer Rim (3D Style)
        // This acts as the container/housing
        Rectangle outerRect = new Rectangle(cx - radius, cy - radius, radius * 2, radius * 2);
        // Changed to White -> Gray gradient as requested
        using (LinearGradientBrush outerBrush = new LinearGradientBrush(outerRect, Color.White, Color.Gray, 90f))
        {
            e.Graphics.FillEllipse(outerBrush, outerRect);
        }

        // 1. Draw 3D Frame/Bevel (Metallic)
        // Shrink slightly to let the dark rim show
        // Made thinner: framePad reduced from 3 to 1
        int framePad = 3;
        int frameThickness = 8;
        Rectangle frameRect = new Rectangle(cx - radius + framePad, cy - radius + framePad, (radius - framePad) * 2, (radius - framePad) * 2);
        
        using (LinearGradientBrush frameBrush = new LinearGradientBrush(frameRect, Color.Gray, Color.White, 45f))
        {
            e.Graphics.FillEllipse(frameBrush, frameRect);
        }
        
        // Inner Bevel (Dark border inside)
        Rectangle faceRect = new Rectangle(frameRect.Left + frameThickness, frameRect.Top + frameThickness, frameRect.Width - frameThickness * 2, frameRect.Height - frameThickness * 2);
        using (Pen bevelPen = new Pen(Color.DimGray, 2))
        {
            e.Graphics.DrawEllipse(bevelPen, faceRect);
        }

        // 2. Draw Gradient Face (White Upper -> Gray Bottom)
        using (LinearGradientBrush faceBrush = new LinearGradientBrush(faceRect, Color.White, Color.Silver, 90f))
        {
            e.Graphics.FillEllipse(faceBrush, faceRect);
        }

        // 3. Draw Ticks (inset from the face rect)
        int tickRadius = faceRect.Width / 2 - 5;
        for (int i = 0; i < 12; i++)
        {
            double angle = i * 30 * Math.PI / 180;
            float x1 = cx + (float)(Math.Cos(angle) * (tickRadius - 8));
            float y1 = cy + (float)(Math.Sin(angle) * (tickRadius - 8));
            float x2 = cx + (float)(Math.Cos(angle) * tickRadius);
            float y2 = cy + (float)(Math.Sin(angle) * tickRadius);
            
            // Thicker ticks for 12, 3, 6, 9
            int capWidth = (i % 3 == 0) ? 3 : 1;
            using (Pen tickPen = new Pen(Color.Black, capWidth))
            {
                e.Graphics.DrawLine(tickPen, x1, y1, x2, y2);
            }
        }

        // 4. Draw Time Zone Text (Upper Middle)
        // Position: Roughly halfway between center and top (in the upper half)
        using (StringFormat sf = new StringFormat())
        {
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            
            // Use Custom Clock Name if available, otherwise fallback or empty
            string textToDisplay = string.IsNullOrEmpty(ClockName) ? "Clock" : ClockName;
            
            using (Font f = new Font("Segoe UI", 8, FontStyle.Bold))
            {
                 // Define a rect in the upper half
                 RectangleF textRect = new RectangleF(cx - tickRadius/2, cy - tickRadius/2 - 15, tickRadius, 20);
                 e.Graphics.DrawString(textToDisplay, f, Brushes.DarkSlateGray, textRect, sf);
            }
        }

        DateTime now = TimeZoneInfo.ConvertTime(DateTime.UtcNow, DisplayedTimeZone);

        // Drop Shadow for hands
        int shadowOffset = 2;

        // Draw Hour Hand
        float hourAngle = (now.Hour % 12 + now.Minute / 60.0f) * 30; 
        DrawHand(e.Graphics, Pens.Gray, hourAngle, tickRadius * 0.5f, 5, shadowOffset); // Shadow
        DrawHand(e.Graphics, Pens.Red, hourAngle, tickRadius * 0.5f, 5, 0);

        // Draw Minute Hand
        float minuteAngle = now.Minute * 6; 
        DrawHand(e.Graphics, Pens.Gray, minuteAngle, tickRadius * 0.8f, 3, shadowOffset); // Shadow
        DrawHand(e.Graphics, Pens.DarkBlue, minuteAngle, tickRadius * 0.8f, 3, 0);

        // Center Cap (Metallic look)
        Rectangle capRect = new Rectangle(cx - 5, cy - 5, 10, 10);
        using (LinearGradientBrush capBrush = new LinearGradientBrush(capRect, Color.White, Color.Gray, 45f))
        {
            e.Graphics.FillEllipse(capBrush, capRect);
            e.Graphics.DrawEllipse(Pens.Black, capRect);
        }
    }

    private void DrawHand(Graphics g, Pen pen, float angleDeg, float length, int thickness, int offset)
    {
        double angleRad = (angleDeg - 90) * Math.PI / 180;
        int cx = this.ClientSize.Width / 2 + offset;
        int cy = this.ClientSize.Height / 2 + offset;

        float x = cx + (float)(Math.Cos(angleRad) * length);
        float y = cy + (float)(Math.Sin(angleRad) * length);

        using (Pen p = new Pen(pen.Color, thickness))
        {
            p.EndCap = LineCap.Round; 
            p.StartCap = LineCap.Round;
            g.DrawLine(p, cx, cy, x, y);
        }
    }
}
