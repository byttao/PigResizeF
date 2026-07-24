using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing; // ���� Color.White
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
using Size = System.Drawing.Size; // ImageSharp �� Color

namespace PigResizeF
{
    public partial class Form1 : Form
    {

        private List<string> droppedFilePaths = new List<string>();
        private readonly List<string> jdFilePaths = new List<string>();
        private Size targetSize; // ���ڴ洢��ǰѡ���Ŀ��ߴ�
        private string sname = "";
        private ListBox jdListBox = null!;
        private JdPreviewCanvas jdPreviewCanvas = null!;
        private TabControl mainTabs = null!;
        private Button buttonJdMoveUp = null!;
        private Button buttonJdMoveDown = null!;
        private Button buttonJdPreview = null!;
        private Button buttonJdExport = null!;
        private static readonly HttpClient UpdateHttpClient = CreateUpdateHttpClient();
        public Form1()
        {
            InitializeComponent();
            richTextBoxLog.Rtf = ""; // ��ʼ��Ϊ�գ���ȷ����������Ϊ RTF ģʽ
            // ���� RichTextBox Ϊֻ���������ù�����
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.ScrollBars = RichTextBoxScrollBars.Vertical;

            // ע���Ϸ��¼�
            tableLayoutPanel1.DragEnter += PanelDropTarget_DragEnter;
            tableLayoutPanel1.DragDrop += PanelDropTarget_DragDrop;

            InitializeJdDetailsTab();
            ApplyVisualStyle();
            Shown += Form1_Shown;

        }

        private static HttpClient CreateUpdateHttpClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PigResizeF/1.3");
            return client;
        }

        private async void Form1_Shown(object? sender, EventArgs e)
        {
            await CheckForUpdateAsync();
        }

        private async Task CheckForUpdateAsync()
        {
            try
            {
                using var response = await UpdateHttpClient.GetAsync("https://api.github.com/repos/byttao/PigResizeF/releases/latest");
                if (!response.IsSuccessStatusCode)
                {
                    return;
                }

                using var release = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
                var root = release.RootElement;
                var tagName = root.GetProperty("tag_name").GetString();
                var releaseUrl = root.GetProperty("html_url").GetString();
                if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(releaseUrl) ||
                    !Version.TryParse(tagName.TrimStart('v', 'V'), out var latestVersion))
                {
                    return;
                }

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0);
                if (latestVersion <= currentVersion || IsDisposed || Disposing)
                {
                    return;
                }

                var message = $"发现新版本 {tagName}。\r\n当前版本：v{currentVersion.Major}.{currentVersion.Minor}.{Math.Max(0, currentVersion.Build)}\r\n\r\n是否打开 GitHub Release 页面？";
                if (MessageBox.Show(message, "发现新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes &&
                    Uri.TryCreate(releaseUrl, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                {
                    Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
                }
            }
            catch (HttpRequestException)
            {
                // Update checks must never interfere with the image workflow.
            }
            catch (TaskCanceledException)
            {
                // A timeout or closing window is treated as an unavailable update service.
            }
            catch (JsonException)
            {
                // Ignore malformed release data rather than showing an unhelpful startup error.
            }
        }

        // ��ק����ʱ����
        private void PanelDropTarget_DragEnter(object sender, DragEventArgs e)
        {
            // �����ק�������Ƿ�����ļ�
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // �������Ʋ���
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // ��ק���ʱ����
        private void PanelDropTarget_DragDrop(object sender, DragEventArgs e)
        {
            // ��ȡ��ק���ļ�·��
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // ���˳�ͼƬ�ļ���������չ�����жϣ�
            var validImageFiles = files.Where(f => IsImageFile(f)).ToList();

            if (validImageFiles.Any())
            {
                // *** �޸ĵ㣺�Ƴ� .Clear() ***
                // droppedFilePaths.Clear(); // �Ƴ����У�ʵ��׷��Ч��

                // ���µ���ЧͼƬ�ļ����ӵ������б���
                foreach (var filePath in validImageFiles)
                {
                    // ����Ƿ��Ѵ��ڣ������ظ�����
                    if (!droppedFilePaths.Contains(filePath))
                    {
                        droppedFilePaths.Add(filePath);
                        listBoxFiles.Items.Add(filePath); // ֻ�����ļ���
                    }
                }


                LogMessage($"�ɹ�׷�� {validImageFiles.Count} ��ͼƬ��", LogLevel.Info);
                // ��ӡ׷�ӵ��ļ�����������־ˢ��������ֻ��ӡ�����ӵ�
                foreach (var filePath in validImageFiles)
                {
                    if (droppedFilePaths.Contains(filePath) && droppedFilePaths.Count(p => p == filePath) == 1) // ȷ����������ӵ�
                    {
                        LogMessage($"  + {Path.GetFileName(filePath)}", LogLevel.Info);
                    }
                }
                LogMessage($"��ǰ�б������� {droppedFilePaths.Count} ��ͼƬ��", LogLevel.Info);
            }
            else
            {
                LogMessage("û���ҵ���Ч��ͼƬ�ļ���", LogLevel.Warning);
            }
        }


        // �ж��Ƿ���ͼƬ�ļ�
        private bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();
            return extension != null && (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp" || extension == ".gif");
        }

        private void InitializeJdDetailsTab()
        {
            Controls.Remove(tableLayoutPanel1);

            mainTabs = new TabControl { Dock = DockStyle.Fill };
            var resizePage = new TabPage("图片裁切");
            resizePage.Controls.Add(tableLayoutPanel1);
            tableLayoutPanel1.Dock = DockStyle.Fill;

            var jdPage = new TabPage("京东详情页裁切");
            jdPage.Controls.Add(CreateJdDetailsLayout());

            mainTabs.TabPages.Add(resizePage);
            mainTabs.TabPages.Add(jdPage);
            Controls.Add(mainTabs);
        }

        private Control CreateJdDetailsLayout()
        {
            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

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

            layout.Controls.Add(imageGroup, 0, 0);
            layout.Controls.Add(actionGroup, 1, 0);
            layout.Controls.Add(previewGroup, 0, 1);
            layout.SetColumnSpan(previewGroup, 2);
            UpdateJdMoveButtons();
            return layout;
        }

        private void ApplyVisualStyle()
        {
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
            richTextBoxLog.BackColor = surface;
            richTextBoxLog.ForeColor = text;
            richTextBoxLog.BorderStyle = BorderStyle.FixedSingle;
            richTextBoxLog.Font = new System.Drawing.Font("Consolas", 9F);

            foreach (var radioButton in new[] { r1440f, r1440c, r800f, rspct })
            {
                radioButton.BackColor = surface;
                radioButton.ForeColor = text;
                radioButton.Font = new System.Drawing.Font("Microsoft YaHei UI", 9F, System.Drawing.FontStyle.Bold);
            }

            StyleButton(buttonProcess, System.Drawing.Color.FromArgb(22, 163, 74), System.Drawing.Color.White);
            StyleButton(buttonClearList, System.Drawing.Color.FromArgb(254, 226, 226), System.Drawing.Color.FromArgb(185, 28, 28));
            StyleButton(buttonAddFolder, System.Drawing.Color.FromArgb(224, 242, 254), System.Drawing.Color.FromArgb(3, 105, 161));

            ApplyJdVisualStyle(surface, text, canvas);
        }

        private void ApplyJdVisualStyle(System.Drawing.Color surface, System.Drawing.Color text, System.Drawing.Color canvas)
        {
            var jdPage = mainTabs.TabPages[1];
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
            var selectedIndices = GetSelectedJdIndices();
            var newFiles = files
                .Where(IsImageFile)
                .Where(path => !jdFilePaths.Contains(path, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            jdFilePaths.AddRange(newFiles);
            RefreshJdFileList(selectedIndices);
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
            }
            catch (Exception ex)
            {
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

            var files = jdFilePaths.ToArray();
            var outputDirectory = Path.GetDirectoryName(files[0]) ?? Environment.CurrentDirectory;
            if (Directory.EnumerateFiles(outputDirectory, "京东详情-*.jpg").Any() &&
                MessageBox.Show("输出目录已有京东详情图片，是否覆盖同名文件？", "京东详情页裁切", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            buttonJdPreview.Enabled = false;
            buttonJdExport.Enabled = false;
            try
            {
                var outputCount = await Task.Run(() => ExportJdDetails(files, outputDirectory));
                MessageBox.Show($"已输出 {outputCount} 张图片。\r\n保存位置：{outputDirectory}", "京东详情页裁切", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearJdFiles();
            }
            catch (Exception ex)
            {
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

        // ���������ť
        private async void buttonProcess_Click(object sender, EventArgs e)
        {
            if (!droppedFilePaths.Any())
            {
                LogMessage("������קͼƬ��ָ������", LogLevel.Error);
                return;
            }

            SetTargetSize(); // ����Ŀ��ߴ�

            // ���ð�ť����ֹ�ظ����
            buttonProcess.Enabled = false;
            LogMessage("��ʼ����ͼƬ...", LogLevel.Info);


            // ���д���ͼƬ
            var tasks = droppedFilePaths.Select(filePath => ProcessImageAsync(filePath));
            await Task.WhenAll(tasks);

            LogMessage("����ͼƬ������ϣ�", LogLevel.Success);
            
            droppedFilePaths.Clear();
            listBoxFiles.Items.Clear(); // ��� ListBox���Ա��������
            LogMessage("ͼƬ�б�����ա�", LogLevel.Info);
            // ���ð�ť
            buttonProcess.Enabled = true;
        }

        // ���� RadioButton ѡ�����Ŀ��ߴ�
        private void SetTargetSize()
        {
            if (r1440f.Checked)
            {
                targetSize = new Size(1440, 1440);
                sname = "1440f";
                LogMessage("Ŀ��ߴ�������Ϊ 1440x1440��", LogLevel.Info);
            }
            else if (r1440c.Checked)
            {
                targetSize = new Size(1440, 1920);
                sname = "1440c";
                LogMessage("Ŀ��ߴ�������Ϊ 1440x1920��", LogLevel.Info);
            }
            else if (r800f.Checked)
            {
                targetSize = new Size(800, 800);
                sname = "800f";
                LogMessage("Ŀ��ߴ�������Ϊ 800x800��", LogLevel.Info);
            }
            else if (rspct.Checked)
            {
                targetSize = new Size(1440, 2160);
                sname = "��ͼ";
                LogMessage("Ŀ��ߴ�������Ϊ 1440*2160��", LogLevel.Info);
            }
        }


        // ͼƬ�������߼�
        private async Task ProcessImageAsync(string inputFilePath)
        {
            try
            {
                using (var image = await SixLabors.ImageSharp.Image.LoadAsync<Rgba32>(inputFilePath))
                {
                    string originalFileName = Path.GetFileNameWithoutExtension(inputFilePath);
                    string outputFileName = $"{originalFileName}_{sname}.jpg";
                    string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), outputFileName);

                    LogMessage($"[*] ���ڴ���: {Path.GetFileName(inputFilePath)}", LogLevel.Processing);

                    // 1. �����ߴ粢����ɫ����
                    var resizedImage = ResizeImageWithPadding(image, targetSize);

                    // 2. ����Ϊ JPG
                    await resizedImage.SaveAsync(outputFilePath, new JpegEncoder { Quality = 90 }); // Quality: 0-100��90�ǽϺõ�ƽ��

                    LogMessage($"    -> �ѱ��浽: {outputFileName} (�ߴ�: {resizedImage.Width}x{resizedImage.Height})", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"    [!] �������� - {Path.GetFileName(inputFilePath)}: {ex.Message}", LogLevel.Error);
            }
        }

        // ����ͼƬ�ߴ粢���Ӱ�ɫ������ƥ��Ŀ��ߴ�
        private Image<Rgba32> ResizeImageWithPadding(Image<Rgba32> sourceImage, Size targetDimensions)
        {
            sourceImage = OrientImage(sourceImage); // �Ⱦ�������
            // �������ű���������ԭʼ����
            float widthRatio = (float)targetDimensions.Width / sourceImage.Width;
            float heightRatio = (float)targetDimensions.Height / sourceImage.Height;
            float ratio = Math.Min(widthRatio, heightRatio);

            // �������ź�ĳߴ�
            int newWidth = (int)(sourceImage.Width * ratio);
            int newHeight = (int)(sourceImage.Height * ratio);

            // ����ͼƬ�ߴ�
            sourceImage.Mutate(x => x.Resize(newWidth, newHeight));

            // ����һ����䱳������ͼƬ
            var paddedImage = new Image<Rgba32>(targetDimensions.Width, targetDimensions.Height, Color.White);

            // ���㽫���ź��ͼƬ��������䱳���ϵ�λ�ã�ʹ�����
            int xOffset = (targetDimensions.Width - newWidth) / 2;
            int yOffset = (targetDimensions.Height - newHeight) / 2;

            // �����ź��ͼƬ���Ƶ���䱳����
            paddedImage.Mutate(x => x.DrawImage(sourceImage, new SixLabors.ImageSharp.Point(xOffset, yOffset), 1.0f));

            return paddedImage;
        }
        
// ����һ����������������ͼƬ��ת
        private Image<Rgba32> OrientImage(Image<Rgba32> image)
        {
            // Check if EXIF information contains the Orientation tag  
            if (image.Metadata.ExifProfile != null &&
                image.Metadata.ExifProfile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? orientationValue) &&
                orientationValue?.Value is ushort orientation)
            {
                switch (orientation)
                {
                    case 2: // Horizontal flip  
                        image.Mutate(x => x.Flip(FlipMode.Horizontal));
                        break;
                    case 3: // Rotate 180 degrees  
                        image.Mutate(x => x.Rotate(RotateMode.Rotate180));
                        break;
                    case 4: // Vertical flip  
                        image.Mutate(x => x.Flip(FlipMode.Vertical));
                        break;
                    case 5: // Transpose (vertical flip followed by 90-degree clockwise rotation)  
                        image.Mutate(x => x.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90));
                        break;
                    case 6: // Rotate 90 degrees clockwise  
                        image.Mutate(x => x.Rotate(RotateMode.Rotate90));
                        break;
                    case 7: // Transverse (horizontal flip followed by 90-degree clockwise rotation)  
                        image.Mutate(x => x.Flip(FlipMode.Horizontal).Rotate(RotateMode.Rotate90));
                        break;
                    case 8: // Rotate 270 degrees clockwise (or 90 degrees counter-clockwise)  
                        image.Mutate(x => x.Rotate(RotateMode.Rotate270));
                        break;
                    default:
                        // Orientation is 1 (Normal) or unknown, no rotation needed  
                        break;
                }
                
        // EXIF Orientation ��ǩ�ھ��������Ͳ�����Ҫ�ˣ������Ƴ���������Ϊ 1
        // image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)1);
        // ����ֱ���Ƴ�
        // image.Metadata.ExifProfile.RemoveTag(ExifTag.Orientation);
            }
            return image;
        }

        // ��־�������
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
            // �� UI �߳��ϸ��� RichTextBox
            if (richTextBoxLog.InvokeRequired)
            {
                richTextBoxLog.Invoke(new Action(() => AppendLog(message, level)));
            }
            else
            {
                AppendLog(message, level);
            }
        }

        private void AppendLog(string message, LogLevel level)
        {
            string prefix = "";
            System.Drawing.Color color = System.Drawing.Color.Black; // Ĭ����ɫ

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

            // ��ʽ�����
            string formattedMessage = $"{DateTime.Now:HH:mm:ss} {prefix}{message}\r\n";

            // ��¼��ɫ
            int startIndex = richTextBoxLog.Text.Length;
            richTextBoxLog.AppendText(formattedMessage);
            int endIndex = richTextBoxLog.Text.Length;

            richTextBoxLog.Select(startIndex, endIndex - startIndex);
            richTextBoxLog.SelectionColor = color;
            richTextBoxLog.SelectionCharOffset = 0; // �ָ�Ĭ��ƫ��
            richTextBoxLog.DeselectAll(); // ȡ��ѡ��

            // �Զ��������ײ�
            richTextBoxLog.ScrollToCaret();
        }

        // �ڴ������ʱ�� RadioButton ״̬�ı�ʱ����Ŀ��ߴ磨��ѡ��
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
            listBoxFiles.Items.Clear(); // ��� ListBox���Ա��������
            LogMessage("ͼƬ�б�����ա�", LogLevel.Info);
        }
    }

}
