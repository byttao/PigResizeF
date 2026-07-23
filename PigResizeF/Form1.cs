using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System;
using System.Collections.Generic;
using System.Drawing; // 用于 Color.White
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using Color = SixLabors.ImageSharp.Color;
using Size = System.Drawing.Size; // ImageSharp 的 Color

namespace PigResizeF
{
    public partial class Form1 : Form
    {

        private List<string> droppedFilePaths = new List<string>();
        private Size targetSize; // 用于存储当前选择的目标尺寸
        private string sname = "";
        public Form1()
        {
            InitializeComponent();
            richTextBoxLog.Rtf = ""; // 初始化为空，但确保它被设置为 RTF 模式
            // 设置 RichTextBox 为只读，并启用滚动条
            richTextBoxLog.ReadOnly = true;
            richTextBoxLog.ScrollBars = RichTextBoxScrollBars.Vertical;

            // 注册拖放事件
            tableLayoutPanel1.DragEnter += PanelDropTarget_DragEnter;
            tableLayoutPanel1.DragDrop += PanelDropTarget_DragDrop;

        }

        // 拖拽进入时触发
        private void PanelDropTarget_DragEnter(object sender, DragEventArgs e)
        {
            // 检查拖拽的数据是否包含文件
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy; // 允许复制操作
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        // 拖拽完成时触发
        private void PanelDropTarget_DragDrop(object sender, DragEventArgs e)
        {
            // 获取拖拽的文件路径
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            // 过滤出图片文件（根据扩展名简单判断）
            var validImageFiles = files.Where(f => IsImageFile(f)).ToList();

            if (validImageFiles.Any())
            {
                // *** 修改点：移除 .Clear() ***
                // droppedFilePaths.Clear(); // 移除此行，实现追加效果

                // 将新的有效图片文件添加到现有列表中
                foreach (var filePath in validImageFiles)
                {
                    // 检查是否已存在，避免重复添加
                    if (!droppedFilePaths.Contains(filePath))
                    {
                        droppedFilePaths.Add(filePath);
                        listBoxFiles.Items.Add(filePath); // 只添加文件名
                    }
                }


                LogMessage($"成功追加 {validImageFiles.Count} 张图片。", LogLevel.Info);
                // 打印追加的文件名，避免日志刷屏，可以只打印新添加的
                foreach (var filePath in validImageFiles)
                {
                    if (droppedFilePaths.Contains(filePath) && droppedFilePaths.Count(p => p == filePath) == 1) // 确保是这次添加的
                    {
                        LogMessage($"  + {Path.GetFileName(filePath)}", LogLevel.Info);
                    }
                }
                LogMessage($"当前列表共包含 {droppedFilePaths.Count} 张图片。", LogLevel.Info);
            }
            else
            {
                LogMessage("没有找到有效的图片文件。", LogLevel.Warning);
            }
        }


        // 判断是否是图片文件
        private bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath)?.ToLower();
            return extension != null && (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp" || extension == ".gif");
        }

        // 点击处理按钮
        private async void buttonProcess_Click(object sender, EventArgs e)
        {
            if (!droppedFilePaths.Any())
            {
                LogMessage("请先拖拽图片到指定区域。", LogLevel.Error);
                return;
            }

            SetTargetSize(); // 更新目标尺寸

            // 禁用按钮，防止重复点击
            buttonProcess.Enabled = false;
            LogMessage("开始处理图片...", LogLevel.Info);


            // 并行处理图片
            var tasks = droppedFilePaths.Select(filePath => ProcessImageAsync(filePath));
            await Task.WhenAll(tasks);

            LogMessage("所有图片处理完毕！", LogLevel.Success);
            
            droppedFilePaths.Clear();
            listBoxFiles.Items.Clear(); // 清空 ListBox，以便重新填充
            LogMessage("图片列表已清空。", LogLevel.Info);
            // 启用按钮
            buttonProcess.Enabled = true;
        }

        // 根据 RadioButton 选择更新目标尺寸
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
                sname = "长图";
                LogMessage("目标尺寸已设置为 1440*2160。", LogLevel.Info);
            }
        }


        // 图片处理主逻辑
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

                    // 1. 调整尺寸并填充白色背景
                    var resizedImage = ResizeImageWithPadding(image, targetSize);

                    // 2. 保存为 JPG
                    await resizedImage.SaveAsync(outputFilePath, new JpegEncoder { Quality = 90 }); // Quality: 0-100，90是较好的平衡

                    LogMessage($"    -> 已保存到: {outputFileName} (尺寸: {resizedImage.Width}x{resizedImage.Height})", LogLevel.Success);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"    [!] 处理错误 - {Path.GetFileName(inputFilePath)}: {ex.Message}", LogLevel.Error);
            }
        }

        // 调整图片尺寸并添加白色背景以匹配目标尺寸
        private Image<Rgba32> ResizeImageWithPadding(Image<Rgba32> sourceImage, Size targetDimensions)
        {
            sourceImage = OrientImage(sourceImage); // 先纠正方向
            // 计算缩放比例，保持原始比例
            float widthRatio = (float)targetDimensions.Width / sourceImage.Width;
            float heightRatio = (float)targetDimensions.Height / sourceImage.Height;
            float ratio = Math.Min(widthRatio, heightRatio);

            // 计算缩放后的尺寸
            int newWidth = (int)(sourceImage.Width * ratio);
            int newHeight = (int)(sourceImage.Height * ratio);

            // 调整图片尺寸
            sourceImage.Mutate(x => x.Resize(newWidth, newHeight));

            // 创建一个填充背景的新图片
            var paddedImage = new Image<Rgba32>(targetDimensions.Width, targetDimensions.Height, Color.White);

            // 计算将缩放后的图片放置在填充背景上的位置，使其居中
            int xOffset = (targetDimensions.Width - newWidth) / 2;
            int yOffset = (targetDimensions.Height - newHeight) / 2;

            // 将缩放后的图片绘制到填充背景上
            paddedImage.Mutate(x => x.DrawImage(sourceImage, new SixLabors.ImageSharp.Point(xOffset, yOffset), 1.0f));

            return paddedImage;
        }
        
// 添加一个辅助方法来处理图片旋转
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
                
        // EXIF Orientation 标签在纠正方向后就不再需要了，可以移除或者重置为 1
        // image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)1);
        // 或者直接移除
        // image.Metadata.ExifProfile.RemoveTag(ExifTag.Orientation);
            }
            return image;
        }

        // 日志输出函数
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
            // 在 UI 线程上更新 RichTextBox
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
            System.Drawing.Color color = System.Drawing.Color.Black; // 默认颜色

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

            // 格式化输出
            string formattedMessage = $"{DateTime.Now:HH:mm:ss} {prefix}{message}\r\n";

            // 记录颜色
            int startIndex = richTextBoxLog.Text.Length;
            richTextBoxLog.AppendText(formattedMessage);
            int endIndex = richTextBoxLog.Text.Length;

            richTextBoxLog.Select(startIndex, endIndex - startIndex);
            richTextBoxLog.SelectionColor = color;
            richTextBoxLog.SelectionCharOffset = 0; // 恢复默认偏移
            richTextBoxLog.DeselectAll(); // 取消选择

            // 自动滚动到底部
            richTextBoxLog.ScrollToCaret();
        }

        // 在窗体加载时或 RadioButton 状态改变时更新目标尺寸（可选）
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
            listBoxFiles.Items.Clear(); // 清空 ListBox，以便重新填充
            LogMessage("图片列表已清空。", LogLevel.Info);
        }
    }

}
