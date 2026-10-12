using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing; // WinForms 绘图类型
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;
using Size = System.Drawing.Size; // 避免与 ImageSharp.Size 重名
using PigResizeF.Core.Services;
using PigResizeF.Core.Models;
using PigResizeF.Core.Common;

namespace PigResizeF
{
    public partial class Form1 : Form
    {
        // 服务层实例
        private readonly IImageProcessor _imageProcessor;
        private readonly IUpdateChecker _updateChecker;

        // 三个页签各自维护输入文件，避免切换功能时互相影响。
        private List<string> droppedFilePaths = new List<string>();
        private readonly List<string> jdFilePaths = new List<string>();
        private readonly List<string> cropFilePaths = new List<string>();
        // 裁切页按图片保存模式，支持自动识别后逐项手动覆盖。
        private readonly Dictionary<string, CropMode> cropFileModes = new(StringComparer.OrdinalIgnoreCase);
        private Size targetSize; // 当前常规转换的目标尺寸
        private string sname = "";
        private ListBox jdListBox = null!;
        private JdPreviewCanvas jdPreviewCanvas = null!;
        private TabControl mainTabs = null!;
        private Button buttonJdMoveUp = null!;
        private Button buttonJdMoveDown = null!;
        private Button buttonJdPreview = null!;
        private Button buttonJdExport = null!;
        private DataGridView cropGridView = null!;
        private RichTextBox cropLogTextBox = null!;
        private RichTextBox jdLogTextBox = null!;
        private RadioButton cropAutoRadioButton = null!;
        private RadioButton cropRandomRadioButton = null!;
        private RadioButton cropGrid2RadioButton = null!;
        private RadioButton cropGrid3RadioButton = null!;
        private CheckBox cropQuickModeCheckBox = null!;
        private CheckBox cropUseCustomOutputCheckBox = null!;
        private TextBox cropOutputDirectoryTextBox = null!;
        private Button cropBrowseOutputButton = null!;
        private Button cropStartButton = null!;
        private bool cropIsProcessing;
        private bool cropGridRefreshing;

        public Form1()
        {
            InitializeComponent();

            // 初始化服务层
            _imageProcessor = new ImageProcessor();
            _updateChecker = new UpdateChecker();

            richTextBoxLog.Rtf = ""; // 用 RTF 空内容初始化，保留后续彩色日志能力
            // 常规转换日志只读，三个页签通过 LogMessage 同步显示。
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.ScrollBars = RichTextBoxScrollBars.Vertical;

            // 常规转换页支持将文件直接拖放到列表或空白区域。
            tableLayoutPanel1.DragEnter += PanelDropTarget_DragEnter;
            tableLayoutPanel1.DragDrop += PanelDropTarget_DragDrop;

            InitializeAdditionalTabs();
            ApplyVisualStyle();
            Shown += Form1_Shown;

        }

        private async void Form1_Shown(object? sender, EventArgs e)
        {
            await CheckForUpdateAsync();
        }

        private async Task CheckForUpdateAsync()
        {
            try
            {
                var updateInfo = await _updateChecker.CheckForUpdateAsync();

                if (updateInfo.HasUpdate && !IsDisposed && !Disposing)
                {
                    var message = $"发现新版本 {updateInfo.LatestVersion}。\r\n当前版本：{updateInfo.CurrentVersion}\r\n\r\n是否打开 GitHub Release 页面？";
                    if (MessageBox.Show(message, "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo(updateInfo.ReleaseUrl) { UseShellExecute = true });
                    }
                }
            }
            catch (HttpRequestException)
            {
                // 更新服务不可用时不影响本地图片处理。
            }
            catch (TaskCanceledException)
            {
                // 超时或窗口关闭时静默结束检查。
            }
            catch (Exception)
            {
                // 忽略异常的 Release 数据，避免启动时出现无意义提示。
            }
        }

        // 拖入文件时只接受资源管理器提供的文件列表。
        private void PanelDropTarget_DragEnter(object sender, DragEventArgs e)
        {
            // 根据数据类型决定鼠标指针效果。
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // 常规转换页：追加新文件，已存在的路径不重复加入。
        private void PanelDropTarget_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // 仅保留支持的图片扩展名。
            var validImageFiles = files.Where(f => IsImageFile(f)).ToList();

            if (validImageFiles.Any())
            {
                foreach (var filePath in validImageFiles)
                {
                    // 保留先前队列，重复路径只显示一次。
                    if (!droppedFilePaths.Contains(filePath))
                    {
                        droppedFilePaths.Add(filePath);
                        listBoxFiles.Items.Add(filePath);
                    }
                }


                LogMessage($"成功追加 {validImageFiles.Count} 张图片。", LogLevel.Info);
                // 逐项记录拖入文件，便于核对待转换队列。
                foreach (var filePath in validImageFiles)
                {
                    if (droppedFilePaths.Contains(filePath) && droppedFilePaths.Count(p => p == filePath) == 1)
                    {
                        LogMessage($"  + {Path.GetFileName(filePath)}", LogLevel.Info);
                    }
                }
                LogMessage($"当前列表共有 {droppedFilePaths.Count} 张图片。", LogLevel.Info);
            }
            else
            {
                LogMessage("没有找到有效图片文件。", LogLevel.Warning);
            }
        }


        // 保持与三个拖放页一致的图片格式白名单。
        private bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();
            return extension != null && (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp" || extension == ".gif");
        }

        private void InitializeAdditionalTabs()
        {
            // 设计器生成的是常规转换页，运行时再组合三个功能页签。
            Controls.Remove(tableLayoutPanel1);

            mainTabs = new TabControl { Dock = DockStyle.Fill };
            var resizePage = new TabPage("图片转换");
            resizePage.Controls.Add(tableLayoutPanel1);
            tableLayoutPanel1.Dock = DockStyle.Fill;

            var cropPage = new TabPage("图片裁切");
            cropPage.Controls.Add(CreateImageCropLayout());

            var jdPage = new TabPage("京东详情页裁切");
            jdPage.Controls.Add(CreateJdDetailsLayout());

            mainTabs.TabPages.Add(resizePage);
            mainTabs.TabPages.Add(cropPage);
            mainTabs.TabPages.Add(jdPage);
            Controls.Add(mainTabs);
        }

        private Control CreateImageCropLayout()
        {
            // 上方管理图片与模式，中间设置输出目录，底部保留完整日志。
            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 3,
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 290F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var imageGroup = new GroupBox { Text = "待裁切图片", Dock = DockStyle.Fill };
            cropGridView = new DataGridView
            {
                AllowDrop = true,
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                MultiSelect = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            cropGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "cropFileColumn",
                HeaderText = "图片文件",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                ReadOnly = true
            });
            var modeColumn = new DataGridViewComboBoxColumn
            {
                Name = "cropModeColumn",
                HeaderText = "识别模式",
                Width = 130,
                FlatStyle = FlatStyle.Flat
            };
            modeColumn.Items.AddRange("自动识别", "四角乱切", "四宫格拆分", "九宫格拆分");
            cropGridView.Columns.Add(modeColumn);
            cropGridView.DragEnter += cropGridView_DragEnter;
            cropGridView.DragDrop += cropGridView_DragDrop;
            cropGridView.CurrentCellDirtyStateChanged += cropGridView_CurrentCellDirtyStateChanged;
            cropGridView.CellValueChanged += cropGridView_CellValueChanged;
            imageGroup.Controls.Add(cropGridView);

            var modeGroup = new GroupBox { Text = "裁切模式", Dock = DockStyle.Fill };
            var modeLayout = new TableLayoutPanel { ColumnCount = 1, RowCount = 5, Dock = DockStyle.Fill };
            for (var row = 0; row < 5; row++)
            {
                modeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            }
            cropAutoRadioButton = new RadioButton { Text = "自动识别", Checked = true, AutoSize = true, Anchor = AnchorStyles.Left };
            cropRandomRadioButton = new RadioButton { Text = "四角乱切", AutoSize = true, Anchor = AnchorStyles.Left };
            cropGrid2RadioButton = new RadioButton { Text = "四宫格拆分", AutoSize = true, Anchor = AnchorStyles.Left };
            cropGrid3RadioButton = new RadioButton { Text = "九宫格拆分", AutoSize = true, Anchor = AnchorStyles.Left };
            cropAutoRadioButton.CheckedChanged += cropModeRadioButton_CheckedChanged;
            cropRandomRadioButton.CheckedChanged += cropModeRadioButton_CheckedChanged;
            cropGrid2RadioButton.CheckedChanged += cropModeRadioButton_CheckedChanged;
            cropGrid3RadioButton.CheckedChanged += cropModeRadioButton_CheckedChanged;
            cropQuickModeCheckBox = new CheckBox { Text = "快速模式", AutoSize = true, Anchor = AnchorStyles.Left };
            modeLayout.Controls.Add(cropAutoRadioButton, 0, 0);
            modeLayout.Controls.Add(cropRandomRadioButton, 0, 1);
            modeLayout.Controls.Add(cropGrid2RadioButton, 0, 2);
            modeLayout.Controls.Add(cropGrid3RadioButton, 0, 3);
            modeLayout.Controls.Add(cropQuickModeCheckBox, 0, 4);
            modeGroup.Controls.Add(modeLayout);

            var outputGroup = new GroupBox { Text = "保存设置", Dock = DockStyle.Fill };
            var outputLayout = new TableLayoutPanel { ColumnCount = 3, RowCount = 2, Dock = DockStyle.Fill };
            outputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            outputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            outputLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118F));
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            outputLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            cropUseCustomOutputCheckBox = new CheckBox { Text = "自定义保存路径", AutoSize = true, Anchor = AnchorStyles.Left };
            cropUseCustomOutputCheckBox.CheckedChanged += cropUseCustomOutputCheckBox_CheckedChanged;
            cropOutputDirectoryTextBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Enabled = false, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            cropBrowseOutputButton = new Button { Text = "选择文件夹", Anchor = AnchorStyles.None, Size = new Size(102, 34), Enabled = false };
            cropBrowseOutputButton.Click += cropBrowseOutputButton_Click;
            cropStartButton = new Button { Text = "开始裁切", Anchor = AnchorStyles.None, Size = new Size(116, 38) };
            cropStartButton.Click += cropStartButton_Click;
            outputLayout.Controls.Add(cropUseCustomOutputCheckBox, 0, 0);
            outputLayout.SetColumnSpan(cropUseCustomOutputCheckBox, 3);
            outputLayout.Controls.Add(cropOutputDirectoryTextBox, 0, 1);
            outputLayout.SetColumnSpan(cropOutputDirectoryTextBox, 2);
            outputLayout.Controls.Add(cropBrowseOutputButton, 2, 1);
            outputGroup.Controls.Add(outputLayout);

            var actionPanel = new Panel { Dock = DockStyle.Fill };
            actionPanel.Controls.Add(cropStartButton);
            cropStartButton.Location = new System.Drawing.Point(18, 40);

            var logGroup = new GroupBox { Text = "处理日志", Dock = DockStyle.Fill };
            cropLogTextBox = CreateLogTextBox();
            logGroup.Controls.Add(cropLogTextBox);

            layout.Controls.Add(imageGroup, 0, 0);
            layout.Controls.Add(modeGroup, 1, 0);
            layout.Controls.Add(outputGroup, 0, 1);
            layout.Controls.Add(actionPanel, 1, 1);
            layout.Controls.Add(logGroup, 0, 2);
            layout.SetColumnSpan(logGroup, 2);
            return layout;
        }

        private Control CreateJdDetailsLayout()
        {
            // 京东页保留图片排序区、长图预览区和独立日志区。
            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 3,
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 154F));

            var imageGroup = new GroupBox { Text = "详情图片", Dock = DockStyle.Fill };
            jdListBox = new ListBox
            {
                AllowDrop = true,
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.MultiExtended,
                HorizontalScrollbar = true
            };
            jdListBox.DragEnter += jdListBox_DragEnter;
            jdListBox.DragDrop += jdListBox_DragDrop;
            jdListBox.SelectedIndexChanged += jdListBox_SelectedIndexChanged;
            jdListBox.KeyDown += jdListBox_KeyDown;
            imageGroup.Controls.Add(jdListBox);

            var actionGroup = new GroupBox { Text = "操作", Dock = DockStyle.Fill };
            var actionLayout = new TableLayoutPanel { ColumnCount = 1, RowCount = 4, Dock = DockStyle.Fill };
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            buttonJdMoveUp = new Button
            {
                Text = "上移",
                Anchor = AnchorStyles.None,
                Size = new Size(102, 34)
            };
            buttonJdMoveUp.Click += buttonJdMoveUp_Click;
            buttonJdMoveDown = new Button
            {
                Text = "下移",
                Anchor = AnchorStyles.None,
                Size = new Size(102, 34)
            };
            buttonJdMoveDown.Click += buttonJdMoveDown_Click;
            buttonJdPreview = new Button
            {
                Text = "拼接预览",
                Anchor = AnchorStyles.None,
                Size = new Size(102, 34)
            };
            buttonJdPreview.Click += buttonJdPreview_Click;
            buttonJdExport = new Button
            {
                Text = "输出",
                BackColor = System.Drawing.Color.MediumSpringGreen,
                Anchor = AnchorStyles.None,
                Size = new Size(102, 34)
            };
            buttonJdExport.Click += buttonJdExport_Click;
            actionLayout.Controls.Add(buttonJdMoveUp, 0, 0);
            actionLayout.Controls.Add(buttonJdMoveDown, 0, 1);
            actionLayout.Controls.Add(buttonJdPreview, 0, 2);
            actionLayout.Controls.Add(buttonJdExport, 0, 3);
            actionGroup.Controls.Add(actionLayout);

            var previewGroup = new GroupBox { Text = "拼接预览", Dock = DockStyle.Fill };
            jdPreviewCanvas = new JdPreviewCanvas { Dock = DockStyle.Fill };
            previewGroup.Controls.Add(jdPreviewCanvas);

            var logGroup = new GroupBox { Text = "处理日志", Dock = DockStyle.Fill };
            jdLogTextBox = CreateLogTextBox();
            logGroup.Controls.Add(jdLogTextBox);

            layout.Controls.Add(imageGroup, 0, 0);
            layout.Controls.Add(actionGroup, 1, 0);
            layout.Controls.Add(previewGroup, 0, 1);
            layout.SetColumnSpan(previewGroup, 2);
            layout.Controls.Add(logGroup, 0, 2);
            layout.SetColumnSpan(logGroup, 2);
            UpdateJdMoveButtons();
            return layout;
        }

        private void ApplyVisualStyle()
        {
            // 统一三页的字体、色彩和控件边框，布局控件仍由各页自行创建。
            var canvas = System.Drawing.Color.FromArgb(245, 247, 250);
            var surface = System.Drawing.Color.White;
            var text = System.Drawing.Color.FromArgb(30, 41, 59);

            ClientSize = new Size(940, 760);
            MinimumSize = new Size(820, 640);
            BackColor = canvas;
            Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);

            mainTabs.Appearance = TabAppearance.Normal;
            mainTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            mainTabs.ItemSize = new Size(156, 42);
            mainTabs.Padding = new System.Drawing.Point(18, 6);
            mainTabs.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            mainTabs.DrawItem += mainTabs_DrawItem;
            mainTabs.SelectedIndexChanged += (_, _) => mainTabs.Invalidate();

            foreach (TabPage page in mainTabs.TabPages)
            {
                page.BackColor = canvas;
                page.Padding = new Padding(0);
            }

            tableLayoutPanel1.Padding = new Padding(14);
            tableLayoutPanel1.BackColor = canvas;
            tableLayoutPanel1.ColumnStyles[1].Width = 178;
            tableLayoutPanel1.RowStyles[0] = new RowStyle(SizeType.Absolute, 300F);
            tableLayoutPanel1.RowStyles[1] = new RowStyle(SizeType.Absolute, 14F);

            StyleGroupBox(groupBox1, surface, text);
            StyleGroupBox(groupBox2, surface, text);
            StyleListBox(listBoxFiles, surface, text);
            StyleLogTextBox(richTextBoxLog, surface, text);

            foreach (var radioButton in new[] { r1440f, r1440c, r800f, rspct })
            {
                radioButton.BackColor = surface;
                radioButton.ForeColor = text;
                radioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            }

            StyleButton(buttonProcess, System.Drawing.Color.FromArgb(22, 163, 74), System.Drawing.Color.White);
            StyleButton(buttonClearList, System.Drawing.Color.FromArgb(254, 226, 226), System.Drawing.Color.FromArgb(185, 28, 28));
            StyleButton(buttonAddFolder, System.Drawing.Color.FromArgb(224, 242, 254), System.Drawing.Color.FromArgb(3, 105, 161));

            ApplyCropVisualStyle(surface, text, canvas);
            ApplyJdVisualStyle(surface, text, canvas);
        }

        private void ApplyCropVisualStyle(System.Drawing.Color surface, System.Drawing.Color text, System.Drawing.Color canvas)
        {
            var cropPage = mainTabs.TabPages[1];
            var layout = (TableLayoutPanel)cropPage.Controls[0];
            layout.Padding = new Padding(14);
            layout.BackColor = canvas;
            layout.ColumnStyles[1].Width = 214;

            foreach (Control control in layout.Controls)
            {
                if (control is GroupBox groupBox)
                {
                    StyleGroupBox(groupBox, surface, text);
                }
            }

            StyleDataGridView(cropGridView, surface, text);
            StyleLogTextBox(cropLogTextBox, surface, text);
            foreach (var radioButton in new[] { cropAutoRadioButton, cropRandomRadioButton, cropGrid2RadioButton, cropGrid3RadioButton })
            {
                radioButton.BackColor = surface;
                radioButton.ForeColor = text;
                radioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            }
            cropQuickModeCheckBox.BackColor = surface;
            cropQuickModeCheckBox.ForeColor = text;
            cropOutputDirectoryTextBox.BackColor = System.Drawing.Color.FromArgb(248, 250, 252);
            cropOutputDirectoryTextBox.ForeColor = text;
            StyleButton(cropBrowseOutputButton, System.Drawing.Color.FromArgb(224, 242, 254), System.Drawing.Color.FromArgb(3, 105, 161));
            StyleButton(cropStartButton, System.Drawing.Color.FromArgb(37, 99, 235), System.Drawing.Color.White);
        }

        private void ApplyJdVisualStyle(System.Drawing.Color surface, System.Drawing.Color text, System.Drawing.Color canvas)
        {
            var jdPage = mainTabs.TabPages[2];
            var layout = (TableLayoutPanel)jdPage.Controls[0];
            layout.Padding = new Padding(14);
            layout.BackColor = canvas;
            layout.ColumnStyles[1].Width = 178;
            layout.RowStyles[0] = new RowStyle(SizeType.Absolute, 284F);

            foreach (Control control in layout.Controls)
            {
                if (control is GroupBox groupBox)
                {
                    StyleGroupBox(groupBox, surface, text);
                }
            }

            StyleListBox(jdListBox, surface, text);
            jdPreviewCanvas.BackColor = System.Drawing.Color.FromArgb(252, 253, 255);
            StyleLogTextBox(jdLogTextBox, surface, text);
            StyleButton(buttonJdMoveUp, System.Drawing.Color.FromArgb(224, 242, 254), System.Drawing.Color.FromArgb(3, 105, 161));
            StyleButton(buttonJdMoveDown, System.Drawing.Color.FromArgb(224, 242, 254), System.Drawing.Color.FromArgb(3, 105, 161));
            StyleButton(buttonJdPreview, System.Drawing.Color.FromArgb(15, 118, 110), System.Drawing.Color.White);
            StyleButton(buttonJdExport, System.Drawing.Color.FromArgb(37, 99, 235), System.Drawing.Color.White);
        }

        private static void StyleGroupBox(GroupBox groupBox, System.Drawing.Color surface, System.Drawing.Color text)
        {
            groupBox.BackColor = surface;
            groupBox.ForeColor = text;
            groupBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            groupBox.Padding = new Padding(10, 26, 10, 10);
        }

        private static void StyleListBox(ListBox listBox, System.Drawing.Color surface, System.Drawing.Color text)
        {
            listBox.BackColor = surface;
            listBox.ForeColor = text;
            listBox.BorderStyle = BorderStyle.FixedSingle;
            listBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            listBox.ItemHeight = 28;
        }

        private static void StyleDataGridView(DataGridView gridView, System.Drawing.Color surface, System.Drawing.Color text)
        {
            gridView.BackgroundColor = surface;
            gridView.GridColor = System.Drawing.Color.FromArgb(226, 232, 240);
            gridView.BorderStyle = BorderStyle.FixedSingle;
            gridView.EnableHeadersVisualStyles = false;
            gridView.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            gridView.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(241, 245, 249);
            gridView.ColumnHeadersDefaultCellStyle.ForeColor = text;
            gridView.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            gridView.DefaultCellStyle.BackColor = surface;
            gridView.DefaultCellStyle.ForeColor = text;
            gridView.DefaultCellStyle.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
            gridView.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(219, 234, 254);
            gridView.DefaultCellStyle.SelectionForeColor = text;
            gridView.RowTemplate.Height = 30;
        }

        private static RichTextBox CreateLogTextBox()
        {
            return new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                HideSelection = false
            };
        }

        private static void StyleLogTextBox(RichTextBox logTextBox, System.Drawing.Color surface, System.Drawing.Color text)
        {
            logTextBox.BackColor = surface;
            logTextBox.ForeColor = text;
            logTextBox.BorderStyle = BorderStyle.FixedSingle;
            logTextBox.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F);
        }

        private static void StyleButton(Button button, System.Drawing.Color backColor, System.Drawing.Color foreColor)
        {
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(
                Math.Max(0, backColor.R - 12),
                Math.Max(0, backColor.G - 12),
                Math.Max(0, backColor.B - 12));
            button.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(
                Math.Max(0, backColor.R - 24),
                Math.Max(0, backColor.G - 24),
                Math.Max(0, backColor.B - 24));
            button.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            button.Size = new Size(116, 38);
            button.Cursor = Cursors.Hand;
        }

        private void mainTabs_DrawItem(object? sender, DrawItemEventArgs e)
        {
            var selected = e.Index == mainTabs.SelectedIndex;
            var bounds = e.Bounds;
            var background = selected
                ? System.Drawing.Color.FromArgb(15, 118, 110)
                : System.Drawing.Color.FromArgb(226, 232, 240);
            var foreground = selected ? System.Drawing.Color.White : System.Drawing.Color.FromArgb(51, 65, 85);
            using var brush = new System.Drawing.SolidBrush(background);
            e.Graphics.FillRectangle(brush, bounds);
            TextRenderer.DrawText(
                e.Graphics,
                mainTabs.TabPages[e.Index].Text,
                mainTabs.Font,
                bounds,
                foreground,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private void jdListBox_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = GetJdDragEffect(e);
        }

        private static DragDropEffects GetJdDragEffect(DragEventArgs e)
        {
            return e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void cropGridView_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = GetJdDragEffect(e);
        }

        private async void cropGridView_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) != true || e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            {
                return;
            }

            var addedFiles = AddCropFiles(files);
            // 先完成识别再启动快速模式，确保显示模式与实际输出一致。
            await DetectCropModesAsync(addedFiles);
            if (cropQuickModeCheckBox.Checked && addedFiles.Length > 0)
            {
                await StartCropAsync(addedFiles);
            }
        }

        private string[] AddCropFiles(IEnumerable<string> files)
        {
            var newFiles = files
                .Where(IsImageFile)
                .Where(path => !cropFilePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            cropFilePaths.AddRange(newFiles);
            var selectedMode = GetCropMode();
            foreach (var filePath in newFiles)
            {
                cropFileModes[filePath] = selectedMode;
            }
            RefreshCropFileList();
            if (newFiles.Length > 0)
            {
                LogMessage($"图片裁切：已添加 {newFiles.Length} 张图片。", LogLevel.Info);
            }
            return newFiles;
        }

        private void RefreshCropFileList()
        {
            cropGridRefreshing = true;
            try
            {
                cropGridView.Rows.Clear();
                foreach (var filePath in cropFilePaths)
                {
                    var mode = cropFileModes.TryGetValue(filePath, out var value) ? value : CropMode.Auto;
                    var rowIndex = cropGridView.Rows.Add(Path.GetFileName(filePath), GetCropModeDisplay(mode));
                    cropGridView.Rows[rowIndex].Tag = filePath;
                }
            }
            finally
            {
                cropGridRefreshing = false;
            }
        }

        private void cropGridView_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (cropGridView.IsCurrentCellDirty)
            {
                cropGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void cropGridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (cropGridRefreshing || e.RowIndex < 0 || e.ColumnIndex != 1 || cropGridView.Rows[e.RowIndex].Tag is not string filePath)
            {
                return;
            }

            var selectedMode = ParseCropMode(cropGridView.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString());
            cropFileModes[filePath] = selectedMode;
            if (selectedMode == CropMode.Auto)
            {
                _ = DetectCropModesAsync(new[] { filePath });
            }
        }

        private async void cropModeRadioButton_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton { Checked: true } && !cropIsProcessing)
            {
                await ApplySelectedCropModeToAllAsync();
            }
        }

        private async Task ApplySelectedCropModeToAllAsync()
        {
            var selectedMode = GetCropMode();
            foreach (var filePath in cropFilePaths)
            {
                cropFileModes[filePath] = selectedMode;
            }
            RefreshCropFileList();
            if (selectedMode == CropMode.Auto)
            {
                await DetectCropModesAsync(cropFilePaths.ToArray());
            }
        }

        private async Task DetectCropModesAsync(IEnumerable<string> files)
        {
            // 只处理仍标记为自动识别的图片，手动选择的模式不能被后台任务覆盖。
            var pendingFiles = files
                .Where(path => cropFileModes.TryGetValue(path, out var mode) && mode == CropMode.Auto)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (pendingFiles.Length == 0)
            {
                return;
            }

            // 识别需读取图片像素，放入后台线程避免拖放操作阻塞界面。
            var detectionResults = await Task.Run(() => pendingFiles
                .Select(path => (Path: path, Mode: TryDetectCropMode(path)))
                .ToArray());
            foreach (var result in detectionResults)
            {
                if (result.Mode.HasValue && cropFileModes.TryGetValue(result.Path, out var currentMode) && currentMode == CropMode.Auto)
                {
                    cropFileModes[result.Path] = result.Mode.Value;
                }
            }
            RefreshCropFileList();
        }

        private static CropMode? TryDetectCropMode(string filePath)
        {
            try
            {
                using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
                OrientImage(image);
                return DetectCropMode(image);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetCropModeDisplay(CropMode mode)
        {
            return mode switch
            {
                CropMode.Auto => "自动识别",
                CropMode.RandomCorners => "四角乱切",
                CropMode.Grid2 => "四宫格拆分",
                CropMode.Grid3 => "九宫格拆分",
                _ => "自动识别"
            };
        }

        private static CropMode ParseCropMode(string? modeText)
        {
            return modeText switch
            {
                "四角乱切" => CropMode.RandomCorners,
                "四宫格拆分" => CropMode.Grid2,
                "九宫格拆分" => CropMode.Grid3,
                _ => CropMode.Auto
            };
        }

        private void cropUseCustomOutputCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            var useCustomOutput = cropUseCustomOutputCheckBox.Checked && !cropIsProcessing;
            cropOutputDirectoryTextBox.Enabled = useCustomOutput;
            cropBrowseOutputButton.Enabled = useCustomOutput;
        }

        private void cropBrowseOutputButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "选择裁切图片保存位置",
                UseDescriptionForTitle = true,
                InitialDirectory = Directory.Exists(cropOutputDirectoryTextBox.Text)
                    ? cropOutputDirectoryTextBox.Text
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                cropOutputDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private async void cropStartButton_Click(object? sender, EventArgs e)
        {
            await StartCropAsync(cropFilePaths.ToArray());
        }

        private async Task StartCropAsync(IEnumerable<string> sourceFiles)
        {
            var files = sourceFiles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0)
            {
                MessageBox.Show("请先拖拽图片到列表。", "图片裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (cropIsProcessing)
            {
                return;
            }

            string? customOutputDirectory = null;
            if (cropUseCustomOutputCheckBox.Checked)
            {
                customOutputDirectory = cropOutputDirectoryTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(customOutputDirectory))
                {
                    MessageBox.Show("请选择自定义保存路径，或关闭自定义保存路径。", "图片裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            // 处理前冻结每张图片的模式，处理中用户对列表的操作不会影响本批输出。
            var jobs = files
                .Select(path => new CropJob(path, cropFileModes.TryGetValue(path, out var mode) ? mode : CropMode.Auto))
                .ToArray();
            cropIsProcessing = true;
            SetCropControlsEnabled(false);
            LogMessage($"图片裁切：开始处理 {jobs.Length} 张图片。", LogLevel.Processing);
            try
            {
                var result = await Task.Run(() => CropImages(jobs, customOutputDirectory));
                var failedSet = result.FailedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
                cropFilePaths.RemoveAll(path => !failedSet.Contains(path));
                foreach (var filePath in files.Where(path => !failedSet.Contains(path)))
                {
                    cropFileModes.Remove(filePath);
                }
                RefreshCropFileList();

                if (result.FailedFiles.Count == 0)
                {
                    LogMessage($"图片裁切：已输出 {result.OutputCount} 张图片。", LogLevel.Success);
                    MessageBox.Show($"已输出 {result.OutputCount} 张图片。", "图片裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage($"图片裁切：已输出 {result.OutputCount} 张图片，{result.FailedFiles.Count} 张未完成。", LogLevel.Warning);
                    MessageBox.Show($"已输出 {result.OutputCount} 张图片。\r\n以下文件未完成，已保留在列表：\r\n{string.Join("\r\n", result.FailedFiles.Select(Path.GetFileName))}", "图片裁切", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"图片裁切失败：{ex.Message}", LogLevel.Error);
                MessageBox.Show($"裁切失败：{ex.Message}", "图片裁切", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                cropIsProcessing = false;
                SetCropControlsEnabled(true);
            }
        }

        private CropMode GetCropMode()
        {
            return cropAutoRadioButton.Checked
                ? CropMode.Auto
                : cropGrid2RadioButton.Checked
                ? CropMode.Grid2
                : cropGrid3RadioButton.Checked
                    ? CropMode.Grid3
                    : CropMode.RandomCorners;
        }

        private void SetCropControlsEnabled(bool enabled)
        {
            cropGridView.Enabled = enabled;
            cropAutoRadioButton.Enabled = enabled;
            cropRandomRadioButton.Enabled = enabled;
            cropGrid2RadioButton.Enabled = enabled;
            cropGrid3RadioButton.Enabled = enabled;
            cropQuickModeCheckBox.Enabled = enabled;
            cropUseCustomOutputCheckBox.Enabled = enabled;
            cropStartButton.Enabled = enabled;
            cropOutputDirectoryTextBox.Enabled = enabled && cropUseCustomOutputCheckBox.Checked;
            cropBrowseOutputButton.Enabled = enabled && cropUseCustomOutputCheckBox.Checked;
        }

        private static CropBatchResult CropImages(IEnumerable<CropJob> jobs, string? customOutputDirectory)
        {
            var outputCount = 0;
            var failedFiles = new List<string>();
            // 顺序处理大图，避免多张高分辨率图片同时解码导致内存峰值过高。
            foreach (var job in jobs)
            {
                try
                {
                    var outputDirectory = customOutputDirectory ?? Path.GetDirectoryName(job.Path) ?? Environment.CurrentDirectory;
                    Directory.CreateDirectory(outputDirectory);
                    outputCount += CropImage(job.Path, outputDirectory, job.Mode);
                }
                catch (Exception)
                {
                    failedFiles.Add(job.Path);
                }
            }
            return new CropBatchResult(outputCount, failedFiles);
        }

        private static int CropImage(string inputFilePath, string outputDirectory, CropMode mode)
        {
            using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(inputFilePath);
            OrientImage(image);
            var originalName = Path.GetFileNameWithoutExtension(inputFilePath);
            var encoder = new JpegEncoder { Quality = 100 };

            // 自动模式在实际输出前再识别一次，防止拖入后文件被替换。
            var resolvedMode = mode == CropMode.Auto ? DetectCropMode(image) : mode;
            return resolvedMode switch
            {
                CropMode.RandomCorners => CropRandomCorners(image, originalName, outputDirectory, encoder),
                CropMode.Grid2 => CropGrid(image, originalName, outputDirectory, encoder, 2),
                CropMode.Grid3 => CropGrid(image, originalName, outputDirectory, encoder, 3),
                _ => throw new InvalidOperationException("未知裁切模式。")
            };
        }

        private static int CropRandomCorners(Image<Rgba32> image, string originalName, string outputDirectory, JpegEncoder encoder)
        {
            // 在原图 85% 宽高范围内取最大的 3:4 区域，再分别锚定到四个角。
            var maxWidth = Math.Max(1, (int)Math.Floor(image.Width * 0.85));
            var maxHeight = Math.Max(1, (int)Math.Floor(image.Height * 0.85));
            var cropWidth = Math.Max(1, Math.Min(maxWidth, (int)Math.Floor(maxHeight * 3D / 4D)));
            var cropHeight = Math.Max(1, Math.Min(maxHeight, (int)Math.Round(cropWidth * 4D / 3D)));
            var positions = new[]
            {
                new SixLabors.ImageSharp.Point(0, 0),
                new SixLabors.ImageSharp.Point(image.Width - cropWidth, 0),
                new SixLabors.ImageSharp.Point(0, image.Height - cropHeight),
                new SixLabors.ImageSharp.Point(image.Width - cropWidth, image.Height - cropHeight)
            };

            for (var index = 0; index < positions.Length; index++)
            {
                using var cropped = image.Clone(context => context
                    .Crop(new SixLabors.ImageSharp.Rectangle(positions[index].X, positions[index].Y, cropWidth, cropHeight))
                    .Resize(1440, 1920));
                cropped.Save(Path.Combine(outputDirectory, $"{originalName}-乱切{index + 1}.jpg"), encoder);
            }
            return positions.Length;
        }

        private static int CropGrid(Image<Rgba32> image, string originalName, string outputDirectory, JpegEncoder encoder, int gridSize)
        {
            if (image.Width < gridSize || image.Height < gridSize)
            {
                throw new InvalidOperationException("图片尺寸不足以进行宫格拆分。");
            }

            // 使用整数边界分割，余下的像素会分配到相邻宫格，不会丢失画面。
            var index = 1;
            for (var row = 0; row < gridSize; row++)
            {
                var top = row * image.Height / gridSize;
                var bottom = (row + 1) * image.Height / gridSize;
                for (var column = 0; column < gridSize; column++)
                {
                    var left = column * image.Width / gridSize;
                    var right = (column + 1) * image.Width / gridSize;
                    using var cropped = image.Clone(context => context.Crop(new SixLabors.ImageSharp.Rectangle(left, top, right - left, bottom - top)));
                    cropped.Save(Path.Combine(outputDirectory, $"{originalName}-拆分{index++}.jpg"), encoder);
                }
            }
            return gridSize * gridSize;
        }

        private static CropMode DetectCropMode(Image<Rgba32> image)
        {
            // 九宫格需四条理论分界线同时明显，降低普通照片被误判为拼图的概率。
            var grid3Scores = new[]
            {
                GetPeakSeamScore(image, true, image.Width / 3),
                GetPeakSeamScore(image, true, image.Width * 2 / 3),
                GetPeakSeamScore(image, false, image.Height / 3),
                GetPeakSeamScore(image, false, image.Height * 2 / 3)
            };
            if (grid3Scores.Min() >= 8D && grid3Scores.Sum() >= 70D)
            {
                return CropMode.Grid3;
            }

            // 若九宫格不成立，再检测中间横线和竖线组成的四宫格。
            var grid2Scores = new[]
            {
                GetPeakSeamScore(image, true, image.Width / 2),
                GetPeakSeamScore(image, false, image.Height / 2)
            };
            if (grid2Scores.Min() >= 12D && grid2Scores.Sum() >= 35D)
            {
                return CropMode.Grid2;
            }

            return CropMode.RandomCorners;
        }

        private static double GetPeakSeamScore(Image<Rgba32> image, bool vertical, int center)
        {
            var dimension = vertical ? image.Width : image.Height;
            var searchRadius = Math.Max(12, (int)Math.Round(dimension * 0.012D));
            var sampleStep = Math.Max(1, (vertical ? image.Height : image.Width) / 600);
            var maxScore = 0D;

            // 在理论分界线附近搜索，可兼容带细分隔线或轻微偏移的拼图。
            for (var position = Math.Max(1, center - searchRadius); position < Math.Min(dimension, center + searchRadius + 1); position++)
            {
                var totalDifference = 0D;
                var sampleCount = 0;
                if (vertical)
                {
                    for (var y = 0; y < image.Height; y += sampleStep)
                    {
                        var left = image[position - 1, y];
                        var right = image[position, y];
                        totalDifference += (Math.Abs(left.R - right.R) + Math.Abs(left.G - right.G) + Math.Abs(left.B - right.B)) / 3D;
                        sampleCount++;
                    }
                }
                else
                {
                    for (var x = 0; x < image.Width; x += sampleStep)
                    {
                        var top = image[x, position - 1];
                        var bottom = image[x, position];
                        totalDifference += (Math.Abs(top.R - bottom.R) + Math.Abs(top.G - bottom.G) + Math.Abs(top.B - bottom.B)) / 3D;
                        sampleCount++;
                    }
                }

                maxScore = Math.Max(maxScore, totalDifference / sampleCount);
            }
            return maxScore;
        }

        private enum CropMode
        {
            Auto,
            RandomCorners,
            Grid2,
            Grid3
        }

        private sealed record CropJob(string Path, CropMode Mode);
        private sealed record CropBatchResult(int OutputCount, IReadOnlyList<string> FailedFiles);

        private void jdListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateJdMoveButtons();
        }

        private void jdListBox_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true && e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                AddJdFiles(files);
            }
        }

        private void AddJdFiles(IEnumerable<string> files)
        {
            // 每次拖入的批次单独按文件名排序，再追加到用户已调整的队列末尾。
            var selectedIndices = GetSelectedJdIndices();
            var newFiles = files
                .Where(IsImageFile)
                .Where(path => !jdFilePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            jdFilePaths.AddRange(newFiles);
            RefreshJdFileList(selectedIndices);
            if (newFiles.Length > 0)
            {
                LogMessage($"京东详情页：已添加 {newFiles.Length} 张图片。", LogLevel.Info);
            }
        }

        private void buttonJdMoveUp_Click(object? sender, EventArgs e)
        {
            var selectedIndices = GetSelectedJdIndices();
            if (selectedIndices.Length == 0 || selectedIndices[0] == 0)
            {
                return;
            }

            foreach (var index in selectedIndices)
            {
                (jdFilePaths[index - 1], jdFilePaths[index]) = (jdFilePaths[index], jdFilePaths[index - 1]);
            }
            RefreshJdFileList(selectedIndices.Select(index => index - 1));
        }

        private void buttonJdMoveDown_Click(object? sender, EventArgs e)
        {
            var selectedIndices = GetSelectedJdIndices();
            if (selectedIndices.Length == 0 || selectedIndices[^1] == jdFilePaths.Count - 1)
            {
                return;
            }

            foreach (var index in selectedIndices.Reverse())
            {
                (jdFilePaths[index + 1], jdFilePaths[index]) = (jdFilePaths[index], jdFilePaths[index + 1]);
            }
            RefreshJdFileList(selectedIndices.Select(index => index + 1));
        }

        private void jdListBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && jdListBox.SelectedIndices.Count > 0)
            {
                DeleteSelectedJdFiles();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void DeleteSelectedJdFiles()
        {
            var selectedIndices = GetSelectedJdIndices();
            foreach (var index in selectedIndices.Reverse())
            {
                jdFilePaths.RemoveAt(index);
            }

            var nextIndex = Math.Min(selectedIndices[0], jdFilePaths.Count - 1);
            RefreshJdFileList(nextIndex >= 0 ? new[] { nextIndex } : Array.Empty<int>());
        }

        private int[] GetSelectedJdIndices()
        {
            return jdListBox.SelectedIndices.Cast<int>().OrderBy(index => index).ToArray();
        }

        private void RefreshJdFileList(IEnumerable<int>? selectedIndices = null)
        {
            jdListBox.BeginUpdate();
            jdListBox.Items.Clear();
            jdListBox.Items.AddRange(jdFilePaths.Select(Path.GetFileName).Cast<object>().ToArray());
            if (selectedIndices != null)
            {
                foreach (var index in selectedIndices.Where(index => index >= 0 && index < jdFilePaths.Count))
                {
                    jdListBox.SetSelected(index, true);
                }
            }
            jdListBox.EndUpdate();
            UpdateJdMoveButtons();
        }

        private void UpdateJdMoveButtons()
        {
            var selectedIndices = GetSelectedJdIndices();
            buttonJdMoveUp.Enabled = selectedIndices.Length > 0 && selectedIndices[0] > 0;
            buttonJdMoveDown.Enabled = selectedIndices.Length > 0 && selectedIndices[^1] < jdFilePaths.Count - 1;
        }

        private void buttonJdPreview_Click(object? sender, EventArgs e)
        {
            if (!jdFilePaths.Any())
            {
                MessageBox.Show("请先拖拽详情图片到列表。", "京东详情页裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                jdPreviewCanvas.SetImages(jdFilePaths);
                LogMessage($"京东详情页：已加载 {jdFilePaths.Count} 张图片预览。", LogLevel.Info);
            }
            catch (Exception ex)
            {
                LogMessage($"京东详情页预览失败：{ex.Message}", LogLevel.Error);
                MessageBox.Show($"无法生成预览：{ex.Message}", "京东详情页裁切", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void buttonJdExport_Click(object? sender, EventArgs e)
        {
            if (!jdFilePaths.Any())
            {
                MessageBox.Show("请先拖拽详情图片到列表。", "京东详情页裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 固化当前列表顺序，后台输出时不受用户继续操作影响。
            var files = jdFilePaths.ToArray();
            var outputDirectory = Path.GetDirectoryName(files[0]) ?? Environment.CurrentDirectory;
            if (Directory.EnumerateFiles(outputDirectory, "京东详情-*.jpg").Any() &&
                MessageBox.Show("输出目录已有京东详情图片，是否覆盖同名文件？", "京东详情页裁切", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            buttonJdPreview.Enabled = false;
            buttonJdExport.Enabled = false;
            LogMessage($"京东详情页：开始输出 {files.Length} 张图片。", LogLevel.Processing);
            try
            {
                var outputCount = await Task.Run(() => ExportJdDetails(files, outputDirectory));
                LogMessage($"京东详情页：已输出 {outputCount} 张图片。", LogLevel.Success);
                MessageBox.Show($"已输出 {outputCount} 张图片。\r\n保存位置：{outputDirectory}", "京东详情页裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearJdFiles();
            }
            catch (Exception ex)
            {
                LogMessage($"京东详情页输出失败：{ex.Message}", LogLevel.Error);
                MessageBox.Show($"输出失败：{ex.Message}", "京东详情页裁切", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonJdPreview.Enabled = true;
                buttonJdExport.Enabled = true;
            }
        }

        private void ClearJdFiles()
        {
            jdFilePaths.Clear();
            RefreshJdFileList();
            jdPreviewCanvas.ClearImages();
        }

        private int ExportJdDetails(IEnumerable<string> files, string outputDirectory)
        {
            // 以 790 像素宽度拼接，并按 1500 像素高度逐张写出，避免创建超长整图。
            const int outputWidth = 790;
            const int segmentHeight = 1500;
            var encoder = new JpegEncoder { Quality = 100 };
            Image<Rgba32>? segment = null;
            var usedHeight = 0;
            var outputIndex = 1;

            try
            {
                foreach (var filePath in files)
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
                    OrientImage(image);
                    var scaledHeight = Math.Max(1, (int)Math.Round(image.Height * (double)outputWidth / image.Width));
                    image.Mutate(context => context.Resize(outputWidth, scaledHeight));

                    var sourceY = 0;
                    while (sourceY < scaledHeight)
                    {
                        segment ??= new Image<Rgba32>(outputWidth, segmentHeight, Color.White);
                        var copyHeight = Math.Min(segmentHeight - usedHeight, scaledHeight - sourceY);
                        segment.Mutate(context => context.DrawImage(
                            image,
                            new SixLabors.ImageSharp.Point(0, usedHeight),
                            new SixLabors.ImageSharp.Rectangle(0, sourceY, outputWidth, copyHeight),
                            1F));
                        sourceY += copyHeight;
                        usedHeight += copyHeight;

                        if (usedHeight == segmentHeight)
                        {
                            SaveJdSegment(segment, segmentHeight, outputDirectory, outputIndex++, encoder);
                            segment.Dispose();
                            segment = null;
                            usedHeight = 0;
                        }
                    }
                }

                if (segment != null && usedHeight > 0)
                {
                    SaveJdSegment(segment, usedHeight, outputDirectory, outputIndex++, encoder);
                }

                return outputIndex - 1;
            }
            finally
            {
                segment?.Dispose();
            }
        }

        private static void SaveJdSegment(Image<Rgba32> segment, int height, string outputDirectory, int outputIndex, JpegEncoder encoder)
        {
            var outputPath = Path.Combine(outputDirectory, $"京东详情-{outputIndex:D3}.jpg");
            if (height == segment.Height)
            {
                segment.Save(outputPath, encoder);
                return;
            }

            using var cropped = segment.Clone(context => context.Crop(new SixLabors.ImageSharp.Rectangle(0, 0, segment.Width, height)));
            cropped.Save(outputPath, encoder);
        }

        // 常规转换入口：并行处理当前列表中的全部图片。
        private async void buttonProcess_Click(object sender, EventArgs e)
        {
            if (!droppedFilePaths.Any())
            {
                LogMessage("请先拖拽图片或指定文件夹。", LogLevel.Error);
                return;
            }

            SetTargetSize(); // 根据单选项刷新目标尺寸与输出后缀

            // 处理期间禁用按钮，防止重复提交相同任务。
            buttonProcess.Enabled = false;
            LogMessage("开始处理图片...", LogLevel.Info);


            // 每个文件独立处理，单个失败不会阻断其他文件。
            var tasks = droppedFilePaths.Select(filePath => ProcessImageAsync(filePath));
            await Task.WhenAll(tasks);

            LogMessage("所有图片处理完成！", LogLevel.Success);
            
            droppedFilePaths.Clear();
            listBoxFiles.Items.Clear();
            LogMessage("图片列表已清空。", LogLevel.Info);
            // 批量任务结束后恢复交互。
            buttonProcess.Enabled = true;
        }

        // 将常规转换页的单选项映射为目标尺寸和输出文件后缀。
        private void SetTargetSize()
        {
            if (r1440f.Checked)
            {
                targetSize = new Size(1440, 1440);
                sname = "1440f";
                LogMessage("目标尺寸已设置为 1440x1440。", LogLevel.Info);
            }
            else if (r1440c.Checked)
            {
                targetSize = new Size(1440, 1920);
                sname = "1440c";
                LogMessage("目标尺寸已设置为 1440x1920。", LogLevel.Info);
            }
            else if (r800f.Checked)
            {
                targetSize = new Size(800, 800);
                sname = "800f";
                LogMessage("目标尺寸已设置为 800x800。", LogLevel.Info);
            }
            else if (rspct.Checked)
            {
                targetSize = new Size(1440, 2160);
                sname = "商品长图";
                LogMessage("目标尺寸已设置为 1440x2160。", LogLevel.Info);
            }
        }


        // 单张常规转换逻辑：等比缩放、白底补齐并输出 JPG。
        private async Task ProcessImageAsync(string inputFilePath)
        {
            try
            {
                using (var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputFilePath))
                {
                    string originalFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                    string outputFileName = $"{originalFileName}_{sname}.jpg";
                    string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), outputFileName);

                    LogMessage($"[*] 正在处理: {Path.GetFileName(inputFilePath)}", LogLevel.Processing);

                    // 先生成目标画布尺寸的图片。
                    var resizedImage = ResizeImageWithPadding(image, targetSize);

                    // 常规转换沿用质量 90，以平衡文件体积与清晰度。
                    await resizedImage.SaveAsync(outputFilePath, new JpegEncoder { Quality = 90 });

                    LogMessage($"    -> 已保存到: {outputFileName} (尺寸: {resizedImage.Width}x{resizedImage.Height})", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"    [!] 处理失败 - {Path.GetFileName(inputFilePath)}: {ex.Message}", LogLevel.Error);
            }
        }

        // 等比缩放原图，并将不足部分用白色背景补齐到指定尺寸。
        private Image<Rgba32> ResizeImageWithPadding(Image<Rgba32> sourceImage, Size targetDimensions)
        {
            sourceImage = OrientImage(sourceImage); // 先按 EXIF 朝向旋转
            // 取较小比例，确保原图完整落在目标画布内。
            float widthRatio = (float)targetDimensions.Width / sourceImage.Width;
            float heightRatio = (float)targetDimensions.Height / sourceImage.Height;
            float ratio = Math.Min(widthRatio, heightRatio);

            // 计算等比缩放后的尺寸。
            int newWidth = (int)(sourceImage.Width * ratio);
            int newHeight = (int)(sourceImage.Height * ratio);

            // 先缩放原图。
            sourceImage.Mutate(x => x.Resize(newWidth, newHeight));

            // 使用白色作为补边背景。
            var paddedImage = new Image<Rgba32>(targetDimensions.Width, targetDimensions.Height, Color.White);

            // 居中放置缩放后的图片。
            int xOffset = (targetDimensions.Width - newWidth) / 2;
            int yOffset = (targetDimensions.Height - newHeight) / 2;

            // 绘制到目标画布后返回新的图片对象。
            paddedImage.Mutate(x => x.DrawImage(sourceImage, new SixLabors.ImageSharp.Point(xOffset, yOffset), 1.0f));

            return paddedImage;
        }
        
        // 根据 EXIF Orientation 修正手机拍摄图片的显示方向。
        private static Image<Rgba32> OrientImage(Image<Rgba32> image)
        {
            // 检查 EXIF 中记录的原始拍摄方向。
            if (image.Metadata.ExifProfile != null &&
                image.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientationValue) &&
                orientationValue?.Value is ushort orientation)
            {
                switch (orientation)
                {
                    case 2: // 水平翻转
                        image.Mutate(x => x.Flip(FlipMode.Horizontal));
                        break;
                    case 3: // 旋转 180 度
                        image.Mutate(x => x.Rotate(RotateMode.Rotate180));
                        break;
                    case 4: // 垂直翻转
                        image.Mutate(x => x.Flip(FlipMode.Vertical));
                        break;
                    case 5: // 垂直翻转后顺时针旋转 90 度
                        image.Mutate(x => x.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90));
                        break;
                    case 6: // 顺时针旋转 90 度
                        image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                        break;
                    case 7: // 水平翻转后顺时针旋转 90 度
                        image.Mutate(x => x.Flip(FlipMode.Horizontal).Rotate(RotateMode.Rotate90));
                        break;
                    case 8: // 顺时针旋转 270 度
                        image.Mutate(x => x.Rotate(RotateMode.Rotate270));
                        break;
                    default:
                        // 方向正常或未知时保持原像素不变。
                        break;
                }
            }
            return image;
        }

        // 日志级别决定前缀和颜色，三个页签共用这一套记录。
        private enum LogLevel
        {
            Info,
            Warning,
            Error,
            Success,
            Processing
        }

        private void LogMessage(string message, LogLevel level)
        {
            if (IsDisposed || Disposing)
            {
                return;
            }
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(message, level)));
            }
            else
            {
                AppendLog(message, level);
            }
        }

        private void AppendLog(string message, LogLevel level)
        {
            foreach (var logTextBox in GetLogTextBoxes())
            {
                AppendLog(logTextBox, message, level);
            }
        }

        private IEnumerable<RichTextBox> GetLogTextBoxes()
        {
            yield return richTextBoxLog;
            if (cropLogTextBox != null)
            {
                yield return cropLogTextBox;
            }
            if (jdLogTextBox != null)
            {
                yield return jdLogTextBox;
            }
        }

        private static void AppendLog(RichTextBox logTextBox, string message, LogLevel level)
        {
            string prefix = "";
            System.Drawing.Color color = System.Drawing.Color.Black;

            switch (level)
            {
                case LogLevel.Info:
                    prefix = "[INFO] ";
                    color = System.Drawing.Color.Blue;
                    break;
                case LogLevel.Warning:
                    prefix = "[WARN] ";
                    color = System.Drawing.Color.Orange;
                    break;
                case LogLevel.Error:
                    prefix = "[ERROR] ";
                    color = System.Drawing.Color.Red;
                    break;
                case LogLevel.Success:
                    prefix = "[SUCCESS] ";
                    color = System.Drawing.Color.Green;
                    break;
                case LogLevel.Processing:
                    prefix = "[PROCESS] ";
                    color = System.Drawing.Color.Purple;
                    break;
            }

            // 统一生成带时间、级别前缀的日志文本。
            string formattedMessage = $"{DateTime.Now:HH:mm:ss} {prefix}{message}\r\n";

            // 只给刚追加的文本设置颜色。
            int startIndex = logTextBox.Text.Length;
            logTextBox.AppendText(formattedMessage);
            int endIndex = logTextBox.Text.Length;

            logTextBox.Select(startIndex, endIndex - startIndex);
            logTextBox.SelectionColor = color;
            logTextBox.SelectionCharOffset = 0;
            logTextBox.DeselectAll();

            // 始终滚动到最新记录。
            logTextBox.ScrollToCaret();
        }

        // 常规转换页的模式切换立即更新目标尺寸。
        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                SetTargetSize();
            }
        }

        private void buttonClearList_Click(object sender, EventArgs e)
        {
            
            droppedFilePaths.Clear();
            listBoxFiles.Items.Clear();
            LogMessage("图片列表已清空。", LogLevel.Info);
        }
    }

}
