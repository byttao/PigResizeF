namespace PigResizeF
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            buttonClearList = new Button();
            listBoxFiles = new ListBox();
            richTextBoxLog = new RichTextBox();
            r1440f = new RadioButton();
            r1440c = new RadioButton();
            r800f = new RadioButton();
            tableLayoutPanel1 = new TableLayoutPanel();
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            tableLayoutPanel2 = new TableLayoutPanel();
            rspct = new RadioButton();
            tableLayoutPanel3 = new TableLayoutPanel();
            buttonAddFolder = new Button();
            buttonProcess = new Button();
            tableLayoutPanel1.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            SuspendLayout();
            // 
            // buttonClearList
            // 
            buttonClearList.Anchor = AnchorStyles.None;
            buttonClearList.BackColor = Color.LightCoral;
            buttonClearList.Location = new Point(21, 163);
            buttonClearList.Name = "buttonClearList";
            buttonClearList.Size = new Size(102, 34);
            buttonClearList.TabIndex = 0;
            buttonClearList.Text = "清空";
            buttonClearList.UseVisualStyleBackColor = false;
            buttonClearList.Click += buttonClearList_Click;
            // 
            // listBoxFiles
            // 
            listBoxFiles.AllowDrop = true;
            listBoxFiles.Dock = DockStyle.Fill;
            listBoxFiles.FormattingEnabled = true;
            listBoxFiles.ItemHeight = 17;
            listBoxFiles.Location = new Point(3, 19);
            listBoxFiles.Name = "listBoxFiles";
            listBoxFiles.SelectionMode = SelectionMode.MultiExtended;
            listBoxFiles.Size = new Size(498, 274);
            listBoxFiles.TabIndex = 1;
            listBoxFiles.DragDrop += PanelDropTarget_DragDrop;
            listBoxFiles.DragEnter += PanelDropTarget_DragEnter;
            // 
            // richTextBoxLog
            // 
            richTextBoxLog.Dock = DockStyle.Fill;
            richTextBoxLog.Location = new Point(8, 310);
            richTextBoxLog.Name = "richTextBoxLog";
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.Size = new Size(504, 360);
            richTextBoxLog.TabIndex = 2;
            richTextBoxLog.Text = "";
            // 
            // r1440f
            // 
            r1440f.Anchor = AnchorStyles.Left;
            r1440f.AutoSize = true;
            r1440f.Checked = true;
            r1440f.Location = new Point(3, 19);
            r1440f.Name = "r1440f";
            r1440f.Size = new Size(78, 21);
            r1440f.TabIndex = 3;
            r1440f.TabStop = true;
            r1440f.Text = "1440方图";
            r1440f.UseVisualStyleBackColor = true;
            r1440f.CheckedChanged += radioButton_CheckedChanged;
            // 
            // r1440c
            // 
            r1440c.Anchor = AnchorStyles.Left;
            r1440c.AutoSize = true;
            r1440c.Location = new Point(3, 78);
            r1440c.Name = "r1440c";
            r1440c.Size = new Size(78, 21);
            r1440c.TabIndex = 4;
            r1440c.Text = "1440长图";
            r1440c.UseVisualStyleBackColor = true;
            r1440c.CheckedChanged += radioButton_CheckedChanged;
            // 
            // r800f
            // 
            r800f.Anchor = AnchorStyles.Left;
            r800f.AutoSize = true;
            r800f.Location = new Point(3, 137);
            r800f.Name = "r800f";
            r800f.Size = new Size(71, 21);
            r800f.TabIndex = 5;
            r800f.Text = "800方图";
            r800f.UseVisualStyleBackColor = true;
            r800f.CheckedChanged += radioButton_CheckedChanged;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            tableLayoutPanel1.Controls.Add(groupBox1, 0, 0);
            tableLayoutPanel1.Controls.Add(groupBox2, 1, 0);
            tableLayoutPanel1.Controls.Add(tableLayoutPanel3, 1, 2);
            tableLayoutPanel1.Controls.Add(richTextBoxLog, 0, 2);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.Padding = new Padding(5);
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle());
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(670, 678);
            tableLayoutPanel1.TabIndex = 6;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(listBoxFiles);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(8, 8);
            groupBox1.Name = "groupBox1";
            tableLayoutPanel1.SetRowSpan(groupBox1, 2);
            groupBox1.Size = new Size(504, 296);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "待转换图片";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tableLayoutPanel2);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(518, 8);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(144, 261);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "转换样式";
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(rspct, 0, 3);
            tableLayoutPanel2.Controls.Add(r1440f, 0, 0);
            tableLayoutPanel2.Controls.Add(r1440c, 0, 1);
            tableLayoutPanel2.Controls.Add(r800f, 0, 2);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 19);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 4;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            tableLayoutPanel2.Size = new Size(138, 239);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // rspct
            // 
            rspct.Anchor = AnchorStyles.Left;
            rspct.AutoSize = true;
            rspct.Location = new Point(3, 197);
            rspct.Name = "rspct";
            rspct.Size = new Size(74, 21);
            rspct.TabIndex = 7;
            rspct.Text = "商品长图";
            rspct.UseVisualStyleBackColor = true;
            rspct.CheckedChanged += radioButton_CheckedChanged;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 1;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Controls.Add(buttonAddFolder, 0, 0);
            tableLayoutPanel3.Controls.Add(buttonProcess, 0, 2);
            tableLayoutPanel3.Controls.Add(buttonClearList, 0, 1);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(518, 310);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 3;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel3.Size = new Size(144, 360);
            tableLayoutPanel3.TabIndex = 3;
            // 
            // buttonAddFolder
            // 
            buttonAddFolder.Anchor = AnchorStyles.None;
            buttonAddFolder.Location = new Point(21, 43);
            buttonAddFolder.Name = "buttonAddFolder";
            buttonAddFolder.Size = new Size(102, 34);
            buttonAddFolder.TabIndex = 2;
            buttonAddFolder.Text = "添加文件夹";
            buttonAddFolder.UseVisualStyleBackColor = true;
            // 
            // buttonProcess
            // 
            buttonProcess.Anchor = AnchorStyles.None;
            buttonProcess.BackColor = Color.MediumSpringGreen;
            buttonProcess.Location = new Point(21, 283);
            buttonProcess.Name = "buttonProcess";
            buttonProcess.Size = new Size(102, 34);
            buttonProcess.TabIndex = 1;
            buttonProcess.Text = "转换";
            buttonProcess.UseVisualStyleBackColor = false;
            buttonProcess.Click += buttonProcess_Click;
            // 
            // Form1
            // 
            AcceptButton = buttonProcess;
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(940, 760);
            Controls.Add(tableLayoutPanel1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "图片批量裁切工具 V1.5 260726";
            tableLayoutPanel1.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel2.PerformLayout();
            tableLayoutPanel3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Button buttonClearList;
        private ListBox listBoxFiles;
        private RichTextBox richTextBoxLog;
        private RadioButton r1440f;
        private RadioButton r1440c;
        private RadioButton r800f;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private TableLayoutPanel tableLayoutPanel2;
        private RadioButton rspct;
        private TableLayoutPanel tableLayoutPanel3;
        private Button buttonAddFolder;
        private Button buttonProcess;
    }
}
