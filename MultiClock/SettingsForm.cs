using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.ObjectModel;

public class SettingsForm : Form
{
    private ComboBox timeZoneComboBox;
    private TextBox nameTextBox;
    private Button setButton;
    private Button cancelButton;
    public string SelectedTimeZoneId { get; private set; }
    public string SelectedClockName { get; private set; }

    public SettingsForm(string currentTimeZoneId, string currentClockName)
    {
        this.Text = "Adjust Clock";
        this.Size = new Size(400, 220);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        // Label for Zone
        Label labelZone = new Label();
        labelZone.Text = "Select Time Zone:";
        labelZone.Location = new Point(10, 15);
        labelZone.AutoSize = true;
        this.Controls.Add(labelZone);

        // Combo for Zone
        timeZoneComboBox = new ComboBox();
        timeZoneComboBox.Location = new Point(10, 35);
        timeZoneComboBox.Width = 360;
        timeZoneComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        
        foreach (TimeZoneInfo zone in TimeZoneInfo.GetSystemTimeZones())
        {
            timeZoneComboBox.Items.Add(zone);
            timeZoneComboBox.DisplayMember = "DisplayName"; 
        }

        try {
            if (!string.IsNullOrEmpty(currentTimeZoneId))
            {
                 foreach (TimeZoneInfo item in timeZoneComboBox.Items)
                 {
                     if (item.Id == currentTimeZoneId)
                     {
                         timeZoneComboBox.SelectedItem = item;
                         break;
                     }
                 }
            }
        } catch {}
        if (timeZoneComboBox.SelectedItem == null) timeZoneComboBox.SelectedIndex = 0;
        this.Controls.Add(timeZoneComboBox);

        // Label for Name
        Label labelName = new Label();
        labelName.Text = "Clock Name (Max 10 chars):";
        labelName.Location = new Point(10, 75);
        labelName.AutoSize = true;
        this.Controls.Add(labelName);

        // TextBox for Name
        nameTextBox = new TextBox();
        nameTextBox.Location = new Point(10, 95);
        nameTextBox.Width = 360;
        nameTextBox.MaxLength = 10; // Constraint: Max 10 chars
        nameTextBox.Text = currentClockName ?? "";
        this.Controls.Add(nameTextBox);

        // Buttons
        setButton = new Button();
        setButton.Text = "Set";
        setButton.Location = new Point(210, 140);
        setButton.DialogResult = DialogResult.OK;
        setButton.Click += SetButton_Click;
        this.Controls.Add(setButton);

        cancelButton = new Button();
        cancelButton.Text = "Cancel";
        cancelButton.Location = new Point(295, 140);
        cancelButton.DialogResult = DialogResult.Cancel;
        this.Controls.Add(cancelButton);
        
        this.AcceptButton = setButton;
        this.CancelButton = cancelButton;
    }

    private void SetButton_Click(object sender, EventArgs e)
    {
        TimeZoneInfo selected = timeZoneComboBox.SelectedItem as TimeZoneInfo;
        if (selected != null)
        {
            SelectedTimeZoneId = selected.Id;
        }
        SelectedClockName = nameTextBox.Text;
    }
}
