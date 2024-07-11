using SysBot.Pokemon.WinForms.Properties;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms;

partial class Main
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        TC_Main = new TabControl();
        Tab_Bots = new TabPage();
        CB_Protocol = new ComboBox();
        FLP_Bots = new FlowLayoutPanel();
        TB_IP = new TextBox();
        CB_Routine = new ComboBox();
        NUD_Port = new NumericUpDown();
        B_New = new Button();
        Tab_Hub = new TabPage();
        PG_Hub = new PropertyGrid();
        Tab_Logs = new TabPage();
        RTB_Logs = new RichTextBox();
        B_Stop = new Button();
        B_Start = new Button();
        TC_Main.SuspendLayout();
        Tab_Bots.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)NUD_Port).BeginInit();
        Tab_Hub.SuspendLayout();
        Tab_Logs.SuspendLayout();
        SuspendLayout();
        // 
        // TC_Main
        // 
        TC_Main.Controls.Add(Tab_Bots);
        TC_Main.Controls.Add(Tab_Hub);
        TC_Main.Controls.Add(Tab_Logs);
        TC_Main.Dock = DockStyle.Fill;
        TC_Main.ItemSize = new System.Drawing.Size(90, 40);
        TC_Main.Location = new System.Drawing.Point(0, 0);
        TC_Main.Margin = new Padding(6, 7, 6, 7);
        TC_Main.Name = "TC_Main";
        TC_Main.SelectedIndex = 0;
        TC_Main.Size = new System.Drawing.Size(990, 761);
        TC_Main.TabIndex = 3;
        // 
        // Tab_Bots
        // 
        Tab_Bots.Controls.Add(CB_Protocol);
        Tab_Bots.Controls.Add(FLP_Bots);
        Tab_Bots.Controls.Add(TB_IP);
        Tab_Bots.Controls.Add(CB_Routine);
        Tab_Bots.Controls.Add(NUD_Port);
        Tab_Bots.Controls.Add(B_New);
        Tab_Bots.Location = new System.Drawing.Point(8, 48);
        Tab_Bots.Margin = new Padding(6, 7, 6, 7);
        Tab_Bots.Name = "Tab_Bots";
        Tab_Bots.Size = new System.Drawing.Size(974, 705);
        Tab_Bots.TabIndex = 0;
        Tab_Bots.Text = "Bots";
        Tab_Bots.UseVisualStyleBackColor = true;
        // 
        // CB_Protocol
        // 
        CB_Protocol.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_Protocol.FormattingEnabled = true;
        CB_Protocol.Location = new System.Drawing.Point(588, 15);
        CB_Protocol.Margin = new Padding(6, 7, 6, 7);
        CB_Protocol.Name = "CB_Protocol";
        CB_Protocol.Size = new System.Drawing.Size(121, 40);
        CB_Protocol.TabIndex = 10;
        CB_Protocol.SelectedIndexChanged += CB_Protocol_SelectedIndexChanged;
        // 
        // FLP_Bots
        // 
        FLP_Bots.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        FLP_Bots.AutoScroll = false;
        FLP_Bots.VerticalScroll.Maximum = 0;
        FLP_Bots.VerticalScroll.Visible = false;
        FLP_Bots.HorizontalScroll.Maximum = 0;
        FLP_Bots.HorizontalScroll.Visible = false;
        FLP_Bots.AutoScroll = true;
        FLP_Bots.BorderStyle = BorderStyle.FixedSingle;
        FLP_Bots.Location = new System.Drawing.Point(0, 67);
        FLP_Bots.Margin = new Padding(0);
        FLP_Bots.Name = "FLP_Bots";
        FLP_Bots.Size = new System.Drawing.Size(973, 637);
        FLP_Bots.TabIndex = 9;
        FLP_Bots.Resize += FLP_Bots_Resize;
        // 
        // TB_IP
        // 
        TB_IP.Font = new System.Drawing.Font("Courier New", 8.25F);
        TB_IP.Location = new System.Drawing.Point(169, 20);
        TB_IP.Margin = new Padding(6, 7, 6, 7);
        TB_IP.Name = "TB_IP";
        TB_IP.Size = new System.Drawing.Size(245, 32);
        TB_IP.TabIndex = 8;
        TB_IP.Text = "192.168.0.1";
        // 
        // CB_Routine
        // 
        CB_Routine.DropDownStyle = ComboBoxStyle.DropDownList;
        CB_Routine.FormattingEnabled = true;
        CB_Routine.Location = new System.Drawing.Point(731, 15);
        CB_Routine.Margin = new Padding(6, 7, 6, 7);
        CB_Routine.Name = "CB_Routine";
        CB_Routine.Size = new System.Drawing.Size(242, 40);
        CB_Routine.TabIndex = 7;
        // 
        // NUD_Port
        // 
        NUD_Port.Font = new System.Drawing.Font("Courier New", 8.25F);
        NUD_Port.Location = new System.Drawing.Point(440, 20);
        NUD_Port.Margin = new Padding(6, 7, 6, 7);
        NUD_Port.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
        NUD_Port.Name = "NUD_Port";
        NUD_Port.Size = new System.Drawing.Size(126, 32);
        NUD_Port.TabIndex = 6;
        NUD_Port.Value = new decimal(new int[] { 6000, 0, 0, 0 });
        // 
        // B_New
        // 
        B_New.Location = new System.Drawing.Point(0, 12);
        B_New.Margin = new Padding(6, 7, 6, 7);
        B_New.Name = "B_New";
        B_New.Size = new System.Drawing.Size(151, 45);
        B_New.TabIndex = 0;
        B_New.Text = "Add";
        B_New.UseVisualStyleBackColor = true;
        B_New.Click += B_New_Click;
        // 
        // Tab_Hub
        // 
        Tab_Hub.Controls.Add(PG_Hub);
        Tab_Hub.Location = new System.Drawing.Point(8, 48);
        Tab_Hub.Margin = new Padding(6, 7, 6, 7);
        Tab_Hub.Name = "Tab_Hub";
        Tab_Hub.Padding = new Padding(6, 7, 6, 7);
        Tab_Hub.Size = new System.Drawing.Size(974, 705);
        Tab_Hub.TabIndex = 2;
        Tab_Hub.Text = "Hub";
        Tab_Hub.UseVisualStyleBackColor = true;
        // 
        // PG_Hub
        // 
        PG_Hub.Dock = DockStyle.Fill;
        PG_Hub.Location = new System.Drawing.Point(6, 7);
        PG_Hub.Margin = new Padding(6, 7, 6, 7);
        PG_Hub.Name = "PG_Hub";
        PG_Hub.PropertySort = PropertySort.Categorized;
        PG_Hub.Size = new System.Drawing.Size(962, 691);
        PG_Hub.TabIndex = 0;
        // 
        // Tab_Logs
        // 
        Tab_Logs.Controls.Add(RTB_Logs);
        Tab_Logs.Location = new System.Drawing.Point(8, 48);
        Tab_Logs.Margin = new Padding(6, 7, 6, 7);
        Tab_Logs.Name = "Tab_Logs";
        Tab_Logs.Size = new System.Drawing.Size(974, 705);
        Tab_Logs.TabIndex = 1;
        Tab_Logs.Text = "Logs";
        Tab_Logs.UseVisualStyleBackColor = true;
        // 
        // RTB_Logs
        // 
        RTB_Logs.Dock = DockStyle.Fill;
        RTB_Logs.Location = new System.Drawing.Point(0, 0);
        RTB_Logs.Margin = new Padding(6, 7, 6, 7);
        RTB_Logs.Name = "RTB_Logs";
        RTB_Logs.ReadOnly = true;
        RTB_Logs.Size = new System.Drawing.Size(974, 705);
        RTB_Logs.TabIndex = 0;
        RTB_Logs.Text = "";
        // 
        // B_Stop
        // 
        B_Stop.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        B_Stop.Location = new System.Drawing.Point(778, 0);
        B_Stop.Margin = new Padding(6, 7, 6, 7);
        B_Stop.Name = "B_Stop";
        B_Stop.Size = new System.Drawing.Size(128, 44);
        B_Stop.TabIndex = 4;
        B_Stop.Text = "Stop All";
        B_Stop.UseVisualStyleBackColor = true;
        B_Stop.Click += B_Stop_Click;
        // 
        // B_Start
        // 
        B_Start.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        B_Start.Location = new System.Drawing.Point(637, 0);
        B_Start.Margin = new Padding(6, 7, 6, 7);
        B_Start.Name = "B_Start";
        B_Start.Size = new System.Drawing.Size(128, 44);
        B_Start.TabIndex = 3;
        B_Start.Text = "Start All";
        B_Start.UseVisualStyleBackColor = true;
        B_Start.Click += B_Start_Click;
        // 
        // Main
        // 
        AutoScaleDimensions = new System.Drawing.SizeF(13F, 32F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        ClientSize = new System.Drawing.Size(990, 761);
        Controls.Add(B_Stop);
        Controls.Add(B_Start);
        Controls.Add(TC_Main);
        Icon = Resources.icon;
        Name = "Main";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MergeBot v9.9";
        FormClosing += Main_FormClosing;
        TC_Main.ResumeLayout(false);
        Tab_Bots.ResumeLayout(false);
        Tab_Bots.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)NUD_Port).EndInit();
        Tab_Hub.ResumeLayout(false);
        Tab_Logs.ResumeLayout(false);
        ResumeLayout(false);
    }

    #endregion
    private TabControl TC_Main;
    private TabPage Tab_Bots;
    private TabPage Tab_Logs;
    private RichTextBox RTB_Logs;
    private TabPage Tab_Hub;
    private PropertyGrid PG_Hub;
    private Button B_Stop;
    private Button B_Start;
    private TextBox TB_IP;
    private ComboBox CB_Routine;
    private NumericUpDown NUD_Port;
    private Button B_New;
    private FlowLayoutPanel FLP_Bots;
    private ComboBox CB_Protocol;
}