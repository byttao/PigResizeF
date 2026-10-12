using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using ImageSharpColor = SixLabors.ImageSharp.Color;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpPoint = SixLabors.ImageSharp.Point;
using ImageSharpRectangle = SixLabors.ImageSharp.Rectangle;
using Color = Avalonia.Media.Color;

namespace PigResizeF;

public partial class MainWindow : Window
{
    private readonly List<string> _convertFiles = [];
    private readonly HashSet<string> _convertSelection = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _cropFiles = [];
    private readonly Dictionary<string, CropMode> _cropModes = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _jdFiles = [];
    private readonly ObservableCollection<string> _jdDisplay = [];

    public MainWindow()
    {
        InitializeComponent();
        JdList.ItemsSource = _jdDisplay;
        Log(ConvertLog, "准备就绪，可拖入图片开始处理。", LogLevel.Info);
        Log(CropLog, "准备就绪，可拖入图片开始裁切。", LogLevel.Info);
        Log(JdLog, "准备就绪，可拖入详情图片。", LogLevel.Info);
    }

    private void ShowConvert(object? sender, RoutedEventArgs e) => ShowView(ConvertView, ConvertNav);
    private void ShowCrop(object? sender, RoutedEventArgs e) => ShowView(CropView, CropNav);
    private void ShowJd(object? sender, RoutedEventArgs e) => ShowView(JdView, JdNav);

    private void ShowView(Control target, Button selected)
    {
        ConvertView.IsVisible = target == ConvertView;
        CropView.IsVisible = target == CropView;
        JdView.IsVisible = target == JdView;
        ConvertNav.Classes.Set("active", selected == ConvertNav);
        CropNav.Classes.Set("active", selected == CropNav);
        JdNav.Classes.Set("active", selected == JdNav);
    }

    private static bool IsImageFile(string path) => new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".webp" }
        .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> GetDropFiles(DragEventArgs e) => e.Data.GetFiles()?
        .Select(file => file.TryGetLocalPath())
        .Where(path => !string.IsNullOrWhiteSpace(path))
        .Cast<string>() ?? Enumerable.Empty<string>();

    private static void SetDragEffect(DragEventArgs e) => e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
    private void ConvertDropZone_DragOver(object? sender, DragEventArgs e) => SetDragEffect(e);
    private void CropDropZone_DragOver(object? sender, DragEventArgs e) => SetDragEffect(e);
    private void JdDropZone_DragOver(object? sender, DragEventArgs e) => SetDragEffect(e);

    private void ConvertDropZone_Drop(object? sender, DragEventArgs e) => AddConvertFiles(GetDropFiles(e));
    private async void CropDropZone_Drop(object? sender, DragEventArgs e)
    {
        var added = AddCropFiles(GetDropFiles(e));
        await DetectCropModesAsync(added);
        if (QuickCrop.IsChecked == true && added.Count > 0)
            await CropAsync(added);
    }
    private void JdDropZone_Drop(object? sender, DragEventArgs e) => AddJdFiles(GetDropFiles(e));

    private void AddConvertFiles(IEnumerable<string> files)
    {
        var added = files.Where(IsImageFile).Where(file => !_convertFiles.Contains(file, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        _convertFiles.AddRange(added);
        RefreshConvertRows();
        if (added.Length > 0) Log(ConvertLog, $"成功添加 {added.Length} 张图片。", LogLevel.Info);
    }

    private void RefreshConvertRows()
    {
        ConvertRows.Children.Clear();
        foreach (var path in _convertFiles)
        {
            var selected = new CheckBox { IsChecked = _convertSelection.Contains(path), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Tag = path };
            selected.IsCheckedChanged += (_, _) =>
            {
                if (selected.IsChecked == true) _convertSelection.Add(path); else _convertSelection.Remove(path);
            };
            var row = new Border { Classes = { "list-row" }, Padding = new Thickness(10, 7) };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,28,*,Auto") };
            grid.Children.Add(selected);
            var swatch = new Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(6), Background = new SolidColorBrush(Color.Parse("#4285D7")), Margin = new Thickness(8, 0) };
            Grid.SetColumn(swatch, 1); grid.Children.Add(swatch);
            var name = new TextBlock { Text = Path.GetFileName(path), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, FontSize = 13 };
            Grid.SetColumn(name, 2); grid.Children.Add(name);
            var info = new TextBlock { Text = GetImageDimensions(path), Foreground = new SolidColorBrush(Color.Parse("#A0AAB5")), FontSize = 11, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center };
            Grid.SetColumn(info, 3); grid.Children.Add(info);
            row.Child = grid;
            ConvertRows.Children.Add(row);
        }
        ConvertCount.Text = $"{_convertFiles.Count} 张待处理";
        ConvertDropHint.IsVisible = _convertFiles.Count == 0;
    }

    private static string GetImageDimensions(string path)
    {
        try { var info = ImageSharpImage.Identify(path); return info == null ? string.Empty : $"{info.Width} × {info.Height}"; }
        catch { return string.Empty; }
    }

    private async void AddFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, Title = "选择图片文件夹" });
        var folder = folders.FirstOrDefault()?.TryGetLocalPath();
        if (folder == null) return;
        AddConvertFiles(Directory.EnumerateFiles(folder).Where(IsImageFile));
    }

    private void ClearConvert(object? sender, RoutedEventArgs e)
    {
        _convertFiles.Clear(); _convertSelection.Clear(); RefreshConvertRows(); Log(ConvertLog, "图片列表已清空。", LogLevel.Info);
    }

    private void RemoveSelectedConvert(object? sender, RoutedEventArgs e)
    {
        _convertFiles.RemoveAll(path => _convertSelection.Contains(path));
        _convertSelection.Clear(); RefreshConvertRows(); Log(ConvertLog, "已移除选中的图片。", LogLevel.Info);
    }

    private async void ConvertImages(object? sender, RoutedEventArgs e)
    {
        if (_convertFiles.Count == 0) { Log(ConvertLog, "请先拖拽图片或添加文件夹。", LogLevel.Error); return; }
        var (width, height, suffix) = GetTargetSize();
        var files = _convertFiles.ToArray();
        Log(ConvertLog, $"开始转换 {files.Length} 张图片，目标尺寸 {width} × {height}。", LogLevel.Processing);
        await Task.WhenAll(files.Select(path => Task.Run(() => ConvertImage(path, width, height, suffix))));
        foreach (var path in files) Log(ConvertLog, $"已保存：{Path.GetFileNameWithoutExtension(path)}_{suffix}.jpg", LogLevel.Success);
        _convertFiles.Clear(); _convertSelection.Clear(); RefreshConvertRows();
    }

    private (int Width, int Height, string Suffix) GetTargetSize() => Size1440Tall.IsChecked == true ? (1440, 1920, "1440c")
        : Size800Square.IsChecked == true ? (800, 800, "800f")
        : SizeProduct.IsChecked == true ? (1440, 2160, "商品长图") : (1440, 1440, "1440f");

    private static void ConvertImage(string inputPath, int targetWidth, int targetHeight, string suffix)
    {
        using var image = ImageSharpImage.Load<Rgba32>(inputPath);
        OrientImage(image);
        var ratio = Math.Min((double)targetWidth / image.Width, (double)targetHeight / image.Height);
        var width = Math.Max(1, (int)(image.Width * ratio));
        var height = Math.Max(1, (int)(image.Height * ratio));
        image.Mutate(context => context.Resize(width, height));
        using var output = new Image<Rgba32>(targetWidth, targetHeight, ImageSharpColor.White);
        output.Mutate(context => context.DrawImage(image, new ImageSharpPoint((targetWidth - width) / 2, (targetHeight - height) / 2), 1));
        output.Save(Path.Combine(Path.GetDirectoryName(inputPath) ?? Environment.CurrentDirectory, $"{Path.GetFileNameWithoutExtension(inputPath)}_{suffix}.jpg"), new JpegEncoder { Quality = 90 });
    }

    private List<string> AddCropFiles(IEnumerable<string> files)
    {
        var mode = SelectedCropMode();
        var added = files.Where(IsImageFile).Where(file => !_cropFiles.Contains(file, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        _cropFiles.AddRange(added);
        foreach (var path in added) _cropModes[path] = mode;
        RefreshCropRows();
        if (added.Count > 0) Log(CropLog, $"图片裁切：已添加 {added.Count} 张图片。", LogLevel.Info);
        return added;
    }

    private void RefreshCropRows()
    {
        CropRows.Children.Clear();
        foreach (var path in _cropFiles)
        {
            var row = new Border { Classes = { "list-row" }, Padding = new Thickness(8, 5) };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("28,*,150") };
            grid.Children.Add(new Border { Width = 22, Height = 22, CornerRadius = new CornerRadius(6), Background = new SolidColorBrush(Color.Parse("#49AFC3")), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            var title = new TextBlock { Text = Path.GetFileName(path), VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, FontSize = 13 };
            Grid.SetColumn(title, 1); grid.Children.Add(title);
            var box = new ComboBox { ItemsSource = Enum.GetValues<CropMode>().Select(DisplayCropMode).ToArray(), SelectedIndex = (int)(_cropModes.TryGetValue(path, out var mode) ? mode : CropMode.Auto), Tag = path, Height = 28 };
            box.SelectionChanged += CropRowModeChanged;
            Grid.SetColumn(box, 2); grid.Children.Add(box);
            row.Child = grid; CropRows.Children.Add(row);
        }
        CropCount.Text = $"{_cropFiles.Count} 张待处理";
    }

    private void CropRowModeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { Tag: string path, SelectedIndex: >= 0 } box)
        {
            _cropModes[path] = (CropMode)box.SelectedIndex;
            Log(CropLog, $"{Path.GetFileName(path)} 模式已设置为 {DisplayCropMode(_cropModes[path])}。", LogLevel.Info);
        }
    }

    private async Task DetectCropModesAsync(IEnumerable<string> files)
    {
        var candidates = files.Where(path => _cropModes.GetValueOrDefault(path) == CropMode.Auto).ToArray();
        if (candidates.Length == 0) return;
        var result = await Task.Run(() => candidates.Select(path => (path, mode: TryDetectCropMode(path))).ToArray());
        foreach (var (path, mode) in result) if (mode.HasValue && _cropModes.GetValueOrDefault(path) == CropMode.Auto) _cropModes[path] = mode.Value;
        RefreshCropRows();
        Log(CropLog, $"图片裁切：已完成 {result.Count(x => x.mode.HasValue)} 张图片的自动识别。", LogLevel.Info);
    }

    private static CropMode? TryDetectCropMode(string path)
    {
        try { using var image = ImageSharpImage.Load<Rgba32>(path); OrientImage(image); return DetectCropMode(image); }
        catch { return null; }
    }

    private void CropModeChanged(object? sender, RoutedEventArgs e)
    {
        if (_cropFiles.Count == 0) return;
        var mode = SelectedCropMode();
        foreach (var path in _cropFiles) _cropModes[path] = mode;
        RefreshCropRows();
        Log(CropLog, $"批量模式已切换为 {DisplayCropMode(mode)}。", LogLevel.Info);
    }

    private CropMode SelectedCropMode() => CropGrid2.IsChecked == true ? CropMode.Grid2 : CropGrid3.IsChecked == true ? CropMode.Grid3 : CropRandom.IsChecked == true ? CropMode.RandomCorners : CropMode.Auto;
    private static string DisplayCropMode(CropMode mode) => mode switch { CropMode.Auto => "自动识别", CropMode.RandomCorners => "四角乱切", CropMode.Grid2 => "四宫格拆分", CropMode.Grid3 => "九宫格拆分", _ => "自动识别" };

    private void CustomOutputChanged(object? sender, RoutedEventArgs e)
    {
        var enabled = UseCustomOutput.IsChecked == true;
        OutputDirectory.IsEnabled = enabled; BrowseOutput.IsEnabled = enabled;
    }

    private async void BrowseOutputFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, Title = "选择裁切图片保存位置" });
        OutputDirectory.Text = folders.FirstOrDefault()?.TryGetLocalPath() ?? OutputDirectory.Text;
    }

    private async void CropImages(object? sender, RoutedEventArgs e) => await CropAsync(_cropFiles.ToArray());

    private async Task CropAsync(IEnumerable<string> source)
    {
        var files = source.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (files.Length == 0) { Log(CropLog, "请先拖拽图片到列表。", LogLevel.Error); return; }
        var output = UseCustomOutput.IsChecked == true ? OutputDirectory.Text?.Trim() : null;
        if (UseCustomOutput.IsChecked == true && string.IsNullOrWhiteSpace(output)) { Log(CropLog, "请选择自定义保存路径，或关闭自定义保存路径。", LogLevel.Error); return; }
        var jobs = files.Select(path => new CropJob(path, _cropModes.GetValueOrDefault(path, CropMode.Auto))).ToArray();
        Log(CropLog, $"图片裁切：开始处理 {jobs.Length} 张图片。", LogLevel.Processing);
        var result = await Task.Run(() => CropImages(jobs, output));
        foreach (var done in files.Except(result.FailedFiles, StringComparer.OrdinalIgnoreCase)) { _cropFiles.Remove(done); _cropModes.Remove(done); }
        RefreshCropRows();
        Log(CropLog, result.FailedFiles.Count == 0 ? $"图片裁切：已输出 {result.OutputCount} 张图片。" : $"已输出 {result.OutputCount} 张图片，{result.FailedFiles.Count} 张未完成。", result.FailedFiles.Count == 0 ? LogLevel.Success : LogLevel.Warning);
    }

    private static CropBatchResult CropImages(IEnumerable<CropJob> jobs, string? customOutput)
    {
        var count = 0; var failures = new List<string>();
        foreach (var job in jobs) try { var output = customOutput ?? Path.GetDirectoryName(job.Path) ?? Environment.CurrentDirectory; Directory.CreateDirectory(output); count += CropImage(job.Path, output, job.Mode); } catch { failures.Add(job.Path); }
        return new CropBatchResult(count, failures);
    }

    private static int CropImage(string path, string output, CropMode mode)
    {
        using var image = ImageSharpImage.Load<Rgba32>(path); OrientImage(image); var name = Path.GetFileNameWithoutExtension(path); var encoder = new JpegEncoder { Quality = 100 };
        return (mode == CropMode.Auto ? DetectCropMode(image) : mode) switch { CropMode.Grid2 => CropGrid(image, name, output, encoder, 2), CropMode.Grid3 => CropGrid(image, name, output, encoder, 3), _ => CropRandomCorners(image, name, output, encoder) };
    }

    private static int CropRandomCorners(Image<Rgba32> image, string name, string output, JpegEncoder encoder)
    {
        var maxWidth = Math.Max(1, (int)Math.Floor(image.Width * .85)); var maxHeight = Math.Max(1, (int)Math.Floor(image.Height * .85));
        var width = Math.Max(1, Math.Min(maxWidth, (int)Math.Floor(maxHeight * .75))); var height = Math.Max(1, Math.Min(maxHeight, (int)Math.Round(width / .75)));
        var points = new[] { new ImageSharpPoint(0, 0), new ImageSharpPoint(image.Width - width, 0), new ImageSharpPoint(0, image.Height - height), new ImageSharpPoint(image.Width - width, image.Height - height) };
        for (var index = 0; index < points.Length; index++) { using var crop = image.Clone(x => x.Crop(new ImageSharpRectangle(points[index].X, points[index].Y, width, height)).Resize(1440, 1920)); crop.Save(Path.Combine(output, $"{name}-乱切{index + 1}.jpg"), encoder); }
        return points.Length;
    }

    private static int CropGrid(Image<Rgba32> image, string name, string output, JpegEncoder encoder, int size)
    {
        var index = 1;
        for (var row = 0; row < size; row++) for (var column = 0; column < size; column++) { var left = column * image.Width / size; var top = row * image.Height / size; var right = (column + 1) * image.Width / size; var bottom = (row + 1) * image.Height / size; using var crop = image.Clone(x => x.Crop(new ImageSharpRectangle(left, top, right - left, bottom - top))); crop.Save(Path.Combine(output, $"{name}-拆分{index++}.jpg"), encoder); }
        return size * size;
    }

    private static CropMode DetectCropMode(Image<Rgba32> image)
    {
        var thirds = new[] { SeamScore(image, true, image.Width / 3), SeamScore(image, true, image.Width * 2 / 3), SeamScore(image, false, image.Height / 3), SeamScore(image, false, image.Height * 2 / 3) };
        if (thirds.Min() >= 8 && thirds.Sum() >= 70) return CropMode.Grid3;
        var halves = new[] { SeamScore(image, true, image.Width / 2), SeamScore(image, false, image.Height / 2) };
        return halves.Min() >= 12 && halves.Sum() >= 35 ? CropMode.Grid2 : CropMode.RandomCorners;
    }

    private static double SeamScore(Image<Rgba32> image, bool vertical, int center)
    {
        var dimension = vertical ? image.Width : image.Height; var radius = Math.Max(12, (int)Math.Round(dimension * .012)); var step = Math.Max(1, (vertical ? image.Height : image.Width) / 600); var best = 0d;
        for (var position = Math.Max(1, center - radius); position < Math.Min(dimension, center + radius + 1); position++) { var sum = 0d; var count = 0; if (vertical) for (var y = 0; y < image.Height; y += step) { var a = image[position - 1, y]; var b = image[position, y]; sum += (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B)) / 3d; count++; } else for (var x = 0; x < image.Width; x += step) { var a = image[x, position - 1]; var b = image[x, position]; sum += (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B)) / 3d; count++; } best = Math.Max(best, sum / count); }
        return best;
    }

    private void AddJdFiles(IEnumerable<string> files)
    {
        var added = files.Where(IsImageFile).Where(file => !_jdFiles.Contains(file, StringComparer.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(Path.GetFileName, StringComparer.CurrentCultureIgnoreCase).ToArray();
        _jdFiles.AddRange(added); RefreshJdList(); if (added.Length > 0) Log(JdLog, $"京东详情页：已添加 {added.Length} 张图片。", LogLevel.Info);
    }

    private void RefreshJdList()
    {
        _jdDisplay.Clear(); foreach (var path in _jdFiles) _jdDisplay.Add(Path.GetFileName(path)); JdCount.Text = $"{_jdFiles.Count} 张待拼接";
    }

    private void JdSelectionChanged(object? sender, SelectionChangedEventArgs e) { }
    private void MoveJdUp(object? sender, RoutedEventArgs e) => MoveJd(-1);
    private void MoveJdDown(object? sender, RoutedEventArgs e) => MoveJd(1);
    private void MoveJd(int direction)
    {
        var index = JdList.SelectedIndex; var next = index + direction; if (index < 0 || next < 0 || next >= _jdFiles.Count) return;
        (_jdFiles[index], _jdFiles[next]) = (_jdFiles[next], _jdFiles[index]); RefreshJdList(); JdList.SelectedIndex = next;
    }

    private void PreviewJd(object? sender, RoutedEventArgs e)
    {
        if (_jdFiles.Count == 0) { Log(JdLog, "请先拖拽详情图片到列表。", LogLevel.Error); return; }
        JdPreview.Children.Clear();
        foreach (var path in _jdFiles)
        {
            try { JdPreview.Children.Add(new Avalonia.Controls.Image { Source = new Bitmap(path), Width = 220, Stretch = Stretch.Uniform }); }
            catch { }
        }
        JdPreviewHint.IsVisible = false; Log(JdLog, $"京东详情页：已加载 {_jdFiles.Count} 张图片预览。", LogLevel.Info);
    }

    private async void ExportJd(object? sender, RoutedEventArgs e)
    {
        if (_jdFiles.Count == 0) { Log(JdLog, "请先拖拽详情图片到列表。", LogLevel.Error); return; }
        var files = _jdFiles.ToArray(); Log(JdLog, $"京东详情页：开始输出 {files.Length} 张图片。", LogLevel.Processing);
        try { var count = await Task.Run(() => ExportJdDetails(files, Path.GetDirectoryName(files[0]) ?? Environment.CurrentDirectory)); Log(JdLog, $"京东详情页：已输出 {count} 张图片。", LogLevel.Success); _jdFiles.Clear(); RefreshJdList(); }
        catch (Exception ex) { Log(JdLog, $"京东详情页输出失败：{ex.Message}", LogLevel.Error); }
    }

    private static int ExportJdDetails(IEnumerable<string> files, string output)
    {
        const int width = 790, segmentHeight = 1500; var encoder = new JpegEncoder { Quality = 100 }; Image<Rgba32>? segment = null; var used = 0; var index = 1;
        try { foreach (var path in files) { using var image = ImageSharpImage.Load<Rgba32>(path); OrientImage(image); var scaled = Math.Max(1, (int)Math.Round(image.Height * (double)width / image.Width)); image.Mutate(x => x.Resize(width, scaled)); for (var y = 0; y < scaled;) { segment ??= new Image<Rgba32>(width, segmentHeight, ImageSharpColor.White); var copy = Math.Min(segmentHeight - used, scaled - y); segment.Mutate(x => x.DrawImage(image, new ImageSharpPoint(0, used), new ImageSharpRectangle(0, y, width, copy), 1)); y += copy; used += copy; if (used == segmentHeight) { segment.Save(Path.Combine(output, $"京东详情-{index++:D3}.jpg"), encoder); segment.Dispose(); segment = null; used = 0; } } } if (segment != null && used > 0) { using var crop = segment.Clone(x => x.Crop(new ImageSharpRectangle(0, 0, width, used))); crop.Save(Path.Combine(output, $"京东详情-{index++:D3}.jpg"), encoder); } return index - 1; } finally { segment?.Dispose(); }
    }

    private async void CheckUpdate(object? sender, RoutedEventArgs e)
    {
        Log(ConvertLog, "检查更新功能将在后续版本中接入。", LogLevel.Info);
        await Task.CompletedTask;
    }

    private static void OrientImage(Image<Rgba32> image)
    {
        if (image.Metadata.ExifProfile?.TryGetValue(ExifTag.Orientation, out IExifValue<ushort>? value) != true || value?.Value is not ushort orientation) return;
        image.Mutate(x => { switch (orientation) { case 2: x.Flip(FlipMode.Horizontal); break; case 3: x.Rotate(RotateMode.Rotate180); break; case 4: x.Flip(FlipMode.Vertical); break; case 5: x.Flip(FlipMode.Vertical).Rotate(RotateMode.Rotate90); break; case 6: x.Rotate(RotateMode.Rotate90); break; case 7: x.Flip(FlipMode.Horizontal).Rotate(RotateMode.Rotate90); break; case 8: x.Rotate(RotateMode.Rotate270); break; } });
    }

    private static void Log(TextBox target, string message, LogLevel level)
    {
        var prefix = level switch { LogLevel.Info => "INFO", LogLevel.Warning => "WARN", LogLevel.Error => "ERROR", LogLevel.Success => "SUCCESS", _ => "PROCESS" };
        target.Text += $"{DateTime.Now:HH:mm:ss}  {prefix}  {message}{Environment.NewLine}";
        target.CaretIndex = target.Text.Length;
    }

    private enum CropMode { Auto, RandomCorners, Grid2, Grid3 }
    private enum LogLevel { Info, Warning, Error, Success, Processing }
    private sealed record CropJob(string Path, CropMode Mode);
    private sealed record CropBatchResult(int OutputCount, IReadOnlyList<string> FailedFiles);
}
