// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use image::{
    DynamicImage, ImageDecoder, Rgb, RgbImage, imageops::FilterType, metadata::Orientation,
};
use rayon::prelude::*;
use serde::{Deserialize, Serialize};
use std::path::{Path, PathBuf};
use std::sync::{Mutex, OnceLock};
use tauri::{Emitter, Manager};

/// 裁切模式，与原 C# 版本的 CropMode 一一对应。
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum CropMode {
    Auto,
    RandomCorners,
    Grid2,
    Grid3,
}

/// 常规转换的目标尺寸预设，与原 C# 的四个单选按钮对应。
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
pub enum ResizePreset {
    /// 1440x1440 填充
    Square1440,
    /// 1440x1920 长图
    Tall1440,
    /// 800x800 填充
    Square800,
    /// 1440x2160 商品长图
    ProductTall,
}

impl ResizePreset {
    fn target_size(self) -> (u32, u32) {
        match self {
            ResizePreset::Square1440 => (1440, 1440),
            ResizePreset::Tall1440 => (1440, 1920),
            ResizePreset::Square800 => (800, 800),
            ResizePreset::ProductTall => (1440, 2160),
        }
    }

    /// 输出文件名后缀，与原 C# 的 sname 字段一致。
    fn suffix(self) -> &'static str {
        match self {
            ResizePreset::Square1440 => "1440f",
            ResizePreset::Tall1440 => "1440c",
            ResizePreset::Square800 => "800f",
            ResizePreset::ProductTall => "商品长图",
        }
    }
}

/// 裁切任务的输入。
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CropJob {
    pub path: String,
    pub mode: CropMode,
}

/// 处理进度事件。percent 综合了文件序号与单张图片内部阶段，用于驱动实时进度条。
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ProgressEvent {
    pub current: usize,
    pub total: usize,
    pub file_name: String,
    pub percent: f64,
    pub stage: String,
}

/// 向前端推送某一批任务的进度，支持多张图片并发上报。
struct ProgressReporter {
    app: tauri::AppHandle,
    event: &'static str,
    total: usize,
    state: Mutex<ProgressState>,
}

struct ProgressState {
    /// 每张图片的完成度，None 表示尚未开始。
    fractions: Vec<Option<f64>>,
    /// 已开始处理的张数，只增不减，前端据此判断“新文件开始”。
    started: usize,
}

impl ProgressReporter {
    fn new(app: tauri::AppHandle, event: &'static str, total: usize) -> Self {
        let state = ProgressState { fractions: vec![None; total], started: 0 };
        Self { app, event, total, state: Mutex::new(state) }
    }

    /// index 为文件序号（从 0 开始），fraction 为单张图片内部完成度（0~1）。
    /// 总进度取所有图片完成度之和，并发时也不会回退。
    fn report(&self, index: usize, file_name: &str, fraction: f64, stage: &str) {
        let total = self.total.max(1);
        let mut state = self.state.lock().unwrap_or_else(|e| e.into_inner());
        let Some(slot) = state.fractions.get_mut(index) else { return };
        let is_new = slot.is_none();
        *slot = Some(slot.unwrap_or(0.0).max(fraction.clamp(0.0, 1.0)));
        if is_new {
            state.started += 1;
        }

        let done: f64 = state.fractions.iter().flatten().sum();
        // 持锁发送，保证事件顺序与进度一致。
        let _ = self.app.emit(self.event, ProgressEvent {
            current: state.started.min(total),
            total,
            file_name: file_name.to_string(),
            percent: (done / total as f64 * 100.0).min(100.0),
            stage: stage.to_string(),
        });
    }
}

/// 批处理同时处理的图片数上限：大图解码后单张占用数十 MB，限制并发以控制内存峰值。
const MAX_BATCH_CONCURRENCY: usize = 4;

fn batch_pool() -> &'static rayon::ThreadPool {
    static POOL: OnceLock<rayon::ThreadPool> = OnceLock::new();
    POOL.get_or_init(|| {
        let threads = std::thread::available_parallelism()
            .map_or(2, |n| n.get())
            .min(MAX_BATCH_CONCURRENCY);
        rayon::ThreadPoolBuilder::new()
            .num_threads(threads)
            .thread_name(|i| format!("pig-batch-{i}"))
            .build()
            .expect("无法创建批处理线程池")
    })
}

/// 在受限线程池中并发处理每个文件，结果按输入顺序返回。
/// process 收到 (序号, 文件名, 进度回调)，结束后自动上报“完成/失败”。
fn run_batch<J, F>(jobs: &[J], path_of: fn(&J) -> &str, reporter: &ProgressReporter, process: F) -> Vec<Result<usize, String>>
where
    J: Sync,
    F: Fn(&J, &Path, StageProgress) -> Result<usize, String> + Sync,
{
    batch_pool().install(|| {
        jobs.par_iter()
            .enumerate()
            .map(|(index, job)| {
                let path = PathBuf::from(path_of(job));
                let file_name = file_name_of(&path);
                let progress = |fraction: f64, stage: &str| reporter.report(index, &file_name, fraction, stage);
                let result = process(job, &path, &progress);
                progress(1.0, if result.is_ok() { "完成" } else { "失败" });
                result
            })
            .collect()
    })
}

/// 单张图片内部的进度回调：(完成度 0~1, 阶段说明)。
type StageProgress<'a> = &'a dyn Fn(f64, &str);

/// 把耗时任务放到阻塞线程池执行，避免占用主线程导致界面和进度条卡死。
async fn run_blocking<T, F>(task: F) -> Result<T, String>
where
    T: Send + 'static,
    F: FnOnce() -> Result<T, String> + Send + 'static,
{
    tauri::async_runtime::spawn_blocking(task)
        .await
        .map_err(|e| format!("后台任务异常: {e}"))?
}

fn file_name_of(path: &Path) -> String {
    path.file_name()
        .and_then(|n| n.to_str())
        .unwrap_or("unknown")
        .to_string()
}

/// 批量处理的统一返回结构。
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct BatchResult {
    pub success: bool,
    pub message: String,
    pub output_count: usize,
    pub failed_files: Vec<String>,
}

/// 裁切模式下拉的选项文本，与原 C# 的 GetCropModeDisplay 一致。
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CropModeOption {
    pub value: CropMode,
    pub label: String,
}

const CROP_MODE_OPTIONS: [(&str, &str); 4] = [
    ("auto", "自动识别"),
    ("randomcorners", "四角乱切"),
    ("grid2", "四宫格拆分"),
    ("grid3", "九宫格拆分"),
];

// ---------------------------------------------------------------------------
// 常规转换
// ---------------------------------------------------------------------------

/// 批量常规转换：等比缩放、白底补齐，输出到原图所在目录。
#[tauri::command]
async fn batch_resize(
    app: tauri::AppHandle,
    files: Vec<String>,
    preset: ResizePreset,
) -> Result<BatchResult, String> {
    run_blocking(move || batch_resize_blocking(app, files, preset)).await
}

fn batch_resize_blocking(
    app: tauri::AppHandle,
    files: Vec<String>,
    preset: ResizePreset,
) -> Result<BatchResult, String> {
    if files.is_empty() {
        return Err("没有待处理的图片".to_string());
    }

    let reporter = ProgressReporter::new(app, "resize-progress", files.len());
    let results = run_batch(&files, |f| f.as_str(), &reporter, |_, path, progress| {
        resize_single_image(path, preset, progress)
    });

    let mut output_count = 0usize;
    let mut failed_files = Vec::new();
    for (file_path, result) in files.iter().zip(results) {
        match result {
            Ok(count) => output_count += count,
            Err(_) => failed_files.push(file_path.clone()),
        }
    }

    Ok(BatchResult {
        success: failed_files.is_empty(),
        message: if failed_files.is_empty() {
            format!("所有图片处理完成！共输出 {output_count} 张图片。")
        } else {
            format!(
                "处理完成，输出 {output_count} 张图片，{} 张失败。",
                failed_files.len()
            )
        },
        output_count,
        failed_files,
    })
}

/// 单张常规转换，输出到原图目录，文件名形如 `原名_1440f.jpg`。
fn resize_single_image(
    input_path: &Path,
    preset: ResizePreset,
    progress: StageProgress,
) -> Result<usize, String> {
    progress(0.0, "读取");
    let image = load_oriented_image(input_path)?;
    let (target_width, target_height) = preset.target_size();

    progress(0.35, "缩放");
    let padded = resize_with_padding(&image, target_width, target_height)?;

    progress(0.8, "保存");

    let parent = input_path.parent().ok_or("无法获取父目录")?;
    let stem = input_path
        .file_stem()
        .and_then(|s| s.to_str())
        .ok_or("文件名无效")?;
    let output_path = parent.join(format!("{}_{}.jpg", stem, preset.suffix()));

    save_jpeg(&padded, &output_path, 90)?;
    Ok(1)
}

/// 等比缩放原图，将不足部分用白色背景补齐到指定尺寸。
fn resize_with_padding(
    source: &RgbImage,
    target_width: u32,
    target_height: u32,
) -> Result<RgbImage, String> {
    let (source_width, source_height) = source.dimensions();
    let width_ratio = target_width as f32 / source_width as f32;
    let height_ratio = target_height as f32 / source_height as f32;
    let ratio = width_ratio.min(height_ratio);

    let new_width = ((source_width as f32 * ratio) as u32).max(1);
    let new_height = ((source_height as f32 * ratio) as u32).max(1);

    let resized = resize_lanczos(source, None, new_width, new_height)?;

    let mut canvas = RgbImage::from_pixel(target_width, target_height, Rgb([255, 255, 255]));
    let x_offset = (target_width - new_width) / 2;
    let y_offset = (target_height - new_height) / 2;

    image::imageops::replace(&mut canvas, &resized, x_offset.into(), y_offset.into());
    Ok(canvas)
}

// ---------------------------------------------------------------------------
// 图片裁切
// ---------------------------------------------------------------------------

/// 返回裁切模式的显示文本，供前端下拉框使用。
#[tauri::command]
fn crop_mode_options() -> Vec<CropModeOption> {
    CROP_MODE_OPTIONS
        .iter()
        .map(|(value, label)| CropModeOption {
            value: match *value {
                "auto" => CropMode::Auto,
                "randomcorners" => CropMode::RandomCorners,
                "grid2" => CropMode::Grid2,
                _ => CropMode::Grid3,
            },
            label: (*label).to_string(),
        })
        .collect()
}

/// 批量裁切：顺序处理，输出到原图目录。
#[tauri::command]
async fn batch_crop(
    app: tauri::AppHandle,
    jobs: Vec<CropJob>,
    output_dir: Option<String>,
) -> Result<BatchResult, String> {
    run_blocking(move || batch_crop_blocking(app, jobs, output_dir)).await
}

fn batch_crop_blocking(
    app: tauri::AppHandle,
    jobs: Vec<CropJob>,
    output_dir: Option<String>,
) -> Result<BatchResult, String> {
    if jobs.is_empty() {
        return Err("没有待裁切的图片".to_string());
    }

    let reporter = ProgressReporter::new(app, "crop-progress", jobs.len());
    let results = run_batch(&jobs, |job| job.path.as_str(), &reporter, |job, path, progress| {
        crop_single_image(path, job.mode, output_dir.as_deref(), progress)
    });

    let mut output_count = 0usize;
    let mut failed_files = Vec::new();
    for (job, result) in jobs.iter().zip(results) {
        match result {
            Ok(count) => output_count += count,
            Err(_) => failed_files.push(job.path.clone()),
        }
    }

    Ok(BatchResult {
        success: failed_files.is_empty(),
        message: if failed_files.is_empty() {
            format!("图片裁切：已输出 {output_count} 张图片。")
        } else {
            format!(
                "图片裁切：已输出 {output_count} 张图片，{} 张未完成。",
                failed_files.len()
            )
        },
        output_count,
        failed_files,
    })
}

/// 单张裁切。输出目录优先使用自定义路径，否则为原图所在目录。
fn crop_single_image(
    input_path: &Path,
    mode: CropMode,
    custom_output_dir: Option<&str>,
    progress: StageProgress,
) -> Result<usize, String> {
    progress(0.0, "读取");
    let image = load_oriented_image(input_path)?;

    let output_dir = match custom_output_dir {
        Some(dir) if !dir.trim().is_empty() => PathBuf::from(dir),
        _ => input_path
            .parent()
            .map(Path::to_path_buf)
            .ok_or("无法获取父目录")?,
    };
    std::fs::create_dir_all(&output_dir).map_err(|e| format!("无法创建输出目录: {e}"))?;

    let stem = input_path
        .file_stem()
        .and_then(|s| s.to_str())
        .ok_or("文件名无效")?;

    // 自动模式在实际输出前再识别一次，防止拖入后文件被替换。
    let resolved_mode = match mode {
        CropMode::Auto => {
            progress(0.15, "识别");
            detect_crop_mode(&image)
        }
        other => other,
    };

    // 读取与识别约占 20%，其余按输出张数均分。
    let piece_progress = |done: usize, count: usize| {
        progress(
            0.2 + 0.8 * done as f64 / count as f64,
            &format!("输出 {}/{}", done + 1, count),
        )
    };

    match resolved_mode {
        CropMode::RandomCorners => crop_random_corners(&image, stem, &output_dir, &piece_progress),
        CropMode::Grid2 => crop_grid(&image, stem, &output_dir, 2, &piece_progress),
        CropMode::Grid3 => crop_grid(&image, stem, &output_dir, 3, &piece_progress),
        CropMode::Auto => unreachable!("自动模式已在上面解析"),
    }
}

/// 四角乱切：在原图 85% 宽高范围内取最大的 3:4 区域，锚定到四个角并缩放到 1440x1920。
fn crop_random_corners(
    image: &RgbImage,
    original_name: &str,
    output_dir: &Path,
    piece_progress: &dyn Fn(usize, usize),
) -> Result<usize, String> {
    let (width, height) = image.dimensions();

    let max_width = ((width as f64 * 0.85).floor() as u32).max(1);
    let max_height = ((height as f64 * 0.85).floor() as u32).max(1);
    let crop_width = max_width.min((max_height as f64 * 3.0 / 4.0).floor() as u32).max(1);
    let crop_height = max_height.min((crop_width as f64 * 4.0 / 3.0).round() as u32).max(1);

    let positions = [
        (0, 0),
        (width.saturating_sub(crop_width), 0),
        (0, height.saturating_sub(crop_height)),
        (
            width.saturating_sub(crop_width),
            height.saturating_sub(crop_height),
        ),
    ];

    for (index, (x, y)) in positions.iter().enumerate() {
        piece_progress(index, positions.len());
        let resized = resize_lanczos(image, Some((*x, *y, crop_width, crop_height)), 1440, 1920)?;

        let output_path = output_dir.join(format!("{}-乱切{}.jpg", original_name, index + 1));
        save_jpeg(&resized, &output_path, 100)?;
    }

    Ok(positions.len())
}

/// 宫格拆分：2x2 或 3x3，使用整数边界分割以免丢失画面。
fn crop_grid(
    image: &RgbImage,
    original_name: &str,
    output_dir: &Path,
    grid_size: u32,
    piece_progress: &dyn Fn(usize, usize),
) -> Result<usize, String> {
    let (width, height) = image.dimensions();
    if width < grid_size || height < grid_size {
        return Err("图片尺寸不足以进行宫格拆分。".to_string());
    }

    let piece_count = (grid_size * grid_size) as usize;
    let mut index = 1;
    for row in 0..grid_size {
        let top = row * height / grid_size;
        let bottom = (row + 1) * height / grid_size;
        for column in 0..grid_size {
            let left = column * width / grid_size;
            let right = (column + 1) * width / grid_size;

            piece_progress(index - 1, piece_count);
            let cropped =
                image::imageops::crop_imm(image, left, top, right - left, bottom - top).to_image();
            let output_path = output_dir.join(format!("{}-拆分{}.jpg", original_name, index));
            save_jpeg(&cropped, &output_path, 100)?;
            index += 1;
        }
    }

    Ok(piece_count)
}

/// 自动识别裁切模式：九宫格需四条理论分界线同时明显，再检测四宫格。
fn detect_crop_mode(image: &RgbImage) -> CropMode {
    let (width, height) = image.dimensions();

    let grid3_scores = [
        get_peak_seam_score(image, true, width / 3),
        get_peak_seam_score(image, true, width * 2 / 3),
        get_peak_seam_score(image, false, height / 3),
        get_peak_seam_score(image, false, height * 2 / 3),
    ];
    let grid3_min = grid3_scores.iter().cloned().fold(f64::MAX, f64::min);
    let grid3_sum: f64 = grid3_scores.iter().sum();
    if grid3_min >= 8.0 && grid3_sum >= 70.0 {
        return CropMode::Grid3;
    }

    let grid2_scores = [
        get_peak_seam_score(image, true, width / 2),
        get_peak_seam_score(image, false, height / 2),
    ];
    let grid2_min = grid2_scores.iter().cloned().fold(f64::MAX, f64::min);
    let grid2_sum: f64 = grid2_scores.iter().sum();
    if grid2_min >= 12.0 && grid2_sum >= 35.0 {
        return CropMode::Grid2;
    }

    CropMode::RandomCorners
}

/// 在理论分界线附近搜索最大接缝得分，兼容带细分隔线或轻微偏移的拼图。
fn get_peak_seam_score(image: &RgbImage, vertical: bool, center: u32) -> f64 {
    let (width, height) = image.dimensions();

    let dimension = if vertical { width } else { height };
    let search_radius = (dimension as f64 * 0.012).round() as u32;
    let search_radius = search_radius.max(12);
    let sample_step = ((if vertical { height } else { width }) / 600).max(1);

    let start = center.saturating_sub(search_radius).max(1);
    let end = dimension.min(center.saturating_add(search_radius) + 1);

    let mut max_score = 0.0f64;

    for position in start..end {
        let mut total_difference = 0.0f64;
        let mut sample_count = 0u64;

        if vertical {
            let mut y = 0;
            while y < height {
                let left = image.get_pixel(position - 1, y);
                let right = image.get_pixel(position, y);
                total_difference += channel_difference(left, right);
                sample_count += 1;
                y += sample_step;
            }
        } else {
            let mut x = 0;
            while x < width {
                let top = image.get_pixel(x, position - 1);
                let bottom = image.get_pixel(x, position);
                total_difference += channel_difference(top, bottom);
                sample_count += 1;
                x += sample_step;
            }
        }

        if sample_count > 0 {
            let score = total_difference / sample_count as f64;
            if score > max_score {
                max_score = score;
            }
        }
    }

    max_score
}

/// 两个像素的 RGB 平均通道差。
fn channel_difference(a: &Rgb<u8>, b: &Rgb<u8>) -> f64 {
    (a[0].abs_diff(b[0]) as f64 + a[1].abs_diff(b[1]) as f64 + a[2].abs_diff(b[2]) as f64) / 3.0
}

/// 识别单张图片的裁切模式，供前端在拖入后即时显示。
#[tauri::command]
async fn detect_crop_mode_command(file_path: String) -> Result<CropMode, String> {
    run_blocking(move || {
        let image = load_oriented_image(Path::new(&file_path))?;
        Ok(detect_crop_mode(&image))
    })
    .await
}

// ---------------------------------------------------------------------------
// 京东详情页裁切
// ---------------------------------------------------------------------------

/// 京东详情页输出：以 790 像素宽拼接，按 1500 像素高度逐张写出。
#[tauri::command]
async fn jd_export(
    app: tauri::AppHandle,
    file_paths: Vec<String>,
    output_dir: String,
) -> Result<BatchResult, String> {
    run_blocking(move || jd_export_blocking(app, file_paths, output_dir)).await
}

fn jd_export_blocking(
    app: tauri::AppHandle,
    file_paths: Vec<String>,
    output_dir: String,
) -> Result<BatchResult, String> {
    if file_paths.is_empty() {
        return Err("没有待输出的图片".to_string());
    }

    let output_directory = PathBuf::from(&output_dir);
    std::fs::create_dir_all(&output_directory)
        .map_err(|e| format!("无法创建输出目录: {e}"))?;

    let reporter = ProgressReporter::new(app, "jd-progress", file_paths.len());
    let output_count = export_jd_details(&file_paths, &output_directory, &reporter)?;

    Ok(BatchResult {
        success: true,
        message: format!("京东详情页：已输出 {output_count} 张图片。"),
        output_count,
        failed_files: Vec::new(),
    })
}

/// 逐张读取并按 790x1500 分段写出，避免创建超长整图。
fn export_jd_details(
    files: &[String],
    output_directory: &Path,
    reporter: &ProgressReporter,
) -> Result<usize, String> {
    const OUTPUT_WIDTH: u32 = 790;
    const SEGMENT_HEIGHT: u32 = 1500;

    let mut segment: Option<RgbImage> = None;
    let mut used_height: u32 = 0;
    let mut output_index = 1usize;

    for (index, file_path) in files.iter().enumerate() {
        let path = PathBuf::from(file_path);
        let file_name = file_name_of(&path);
        let progress = |fraction: f64, stage: &str| reporter.report(index, &file_name, fraction, stage);

        progress(0.0, "读取");
        let image = load_oriented_image(&path)?;
        let scaled_height =
            ((image.height() as f64 * OUTPUT_WIDTH as f64 / image.width() as f64).round() as u32).max(1);
        progress(0.3, "缩放");
        let scaled = resize_lanczos(&image, None, OUTPUT_WIDTH, scaled_height)?;

        let mut source_y = 0u32;
        while source_y < scaled_height {
            // 拼接与分段写出占单张的后 40%，按已消耗的源图高度推进。
            progress(
                0.6 + 0.4 * source_y as f64 / scaled_height as f64,
                &format!("拼接第 {output_index} 段"),
            );
            if segment.is_none() {
                segment = Some(RgbImage::from_pixel(
                    OUTPUT_WIDTH,
                    SEGMENT_HEIGHT,
                    Rgb([255, 255, 255]),
                ));
            }

            let current = segment.as_mut().expect("segment 已初始化");
            let copy_height = (SEGMENT_HEIGHT - used_height).min(scaled_height - source_y);

            // 把源图的一段拷贝到当前分段的 used_height 位置。
            let strip = image::imageops::crop_imm(
                &scaled,
                0,
                source_y,
                OUTPUT_WIDTH,
                copy_height,
            )
            .to_image();
            image::imageops::replace(current, &strip, 0, used_height.into());

            source_y += copy_height;
            used_height += copy_height;

            if used_height == SEGMENT_HEIGHT {
                let full = segment.take().expect("segment 已初始化");
                save_jd_segment(&full, SEGMENT_HEIGHT, output_directory, output_index)?;
                output_index += 1;
                used_height = 0;
            }
        }

        progress(1.0, "完成");
    }

    if let Some(remaining) = segment {
        if used_height > 0 {
            reporter.report(files.len() - 1, "末段", 1.0, &format!("写出第 {output_index} 段"));
            save_jd_segment(&remaining, used_height, output_directory, output_index)?;
            output_index += 1;
        }
    }

    Ok(output_index - 1)
}

/// 写出一张京东详情分段，文件名为 `京东详情-001.jpg`。
fn save_jd_segment(
    segment: &RgbImage,
    height: u32,
    output_directory: &Path,
    output_index: usize,
) -> Result<(), String> {
    let output_path = output_directory.join(format!("京东详情-{output_index:03}.jpg"));

    if height == segment.height() {
        save_jpeg(segment, &output_path, 100)?;
        return Ok(());
    }

    // 末段高度不足，裁剪到实际高度再写出。
    let cropped = image::imageops::crop_imm(segment, 0, 0, segment.width(), height).to_image();
    save_jpeg(&cropped, &output_path, 100)?;
    Ok(())
}

// ---------------------------------------------------------------------------
// 公共辅助
// ---------------------------------------------------------------------------

/// 读取图片、按 EXIF Orientation 修正方向，并转为 RGB（透明部分铺白底，与输出 JPG 一致）。
fn load_oriented_image(path: &Path) -> Result<RgbImage, String> {
    let mut image = image::open(path).map_err(|e| format!("无法打开图片: {e}"))?;

    if let Some(orientation) = read_exif_orientation(path) {
        image.apply_orientation(orientation);
    }

    Ok(flatten_to_rgb(image))
}

/// 带透明通道的图片按 alpha 混合到白底上，其余直接转 RGB。
fn flatten_to_rgb(image: DynamicImage) -> RgbImage {
    if !image.color().has_alpha() {
        return image.into_rgb8();
    }

    let rgba = image.into_rgba8();
    let (width, height) = rgba.dimensions();
    let pixels = rgba
        .as_raw()
        .as_chunks::<4>()
        .0
        .iter()
        .flat_map(|p| {
            let alpha = p[3] as u32;
            [0, 1, 2].map(|i| ((p[i] as u32 * alpha + 255 * (255 - alpha) + 127) / 255) as u8)
        })
        .collect();
    RgbImage::from_raw(width, height, pixels).expect("像素数量与尺寸一致")
}

/// Lanczos3 缩放（fast_image_resize，SIMD + 多线程）。crop 为源图上的 (x, y, 宽, 高)，None 表示整张。
fn resize_lanczos(
    source: &RgbImage,
    crop: Option<(u32, u32, u32, u32)>,
    width: u32,
    height: u32,
) -> Result<RgbImage, String> {
    use fast_image_resize::{
        FilterType, PixelType, ResizeAlg, ResizeOptions, Resizer,
        images::{Image, ImageRef},
    };

    let src = ImageRef::new(source.width(), source.height(), source.as_raw(), PixelType::U8x3)
        .map_err(|e| format!("缩放失败: {e}"))?;
    let mut dst = Image::new(width, height, PixelType::U8x3);
    let mut options = ResizeOptions::new().resize_alg(ResizeAlg::Convolution(FilterType::Lanczos3));
    if let Some((x, y, w, h)) = crop {
        options = options.crop(x as f64, y as f64, w as f64, h as f64);
    }

    Resizer::new()
        .resize(&src, &mut dst, &options)
        .map_err(|e| format!("缩放失败: {e}"))?;
    RgbImage::from_raw(width, height, dst.into_vec()).ok_or_else(|| "缩放失败".to_string())
}

/// 从 JPEG 的 EXIF 中读取拍摄方向，非 JPEG 或缺失时返回 None。
fn read_exif_orientation(path: &Path) -> Option<Orientation> {
    use image::ImageReader;

    let reader = ImageReader::open(path).ok()?.with_guessed_format().ok()?;
    let mut decoder = reader.into_decoder().ok()?;
    let orientation = decoder.orientation().ok()?;

    if orientation == Orientation::NoTransforms {
        None
    } else {
        Some(orientation)
    }
}

/// 以指定质量写出 JPG。
/// 先写临时文件再改名，并发任务输出到同一路径时不会互相写坏。
fn save_jpeg(rgb: &RgbImage, path: &Path, quality: u8) -> Result<(), String> {
    static TEMP_COUNTER: std::sync::atomic::AtomicU64 = std::sync::atomic::AtomicU64::new(0);
    let sequence = TEMP_COUNTER.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
    let temp = path.with_extension(format!("{}-{sequence}.tmp", std::process::id()));

    let result = write_jpeg(rgb, &temp, quality)
        .and_then(|()| std::fs::rename(&temp, path).map_err(|e| format!("无法写入文件: {e}")));
    if result.is_err() {
        let _ = std::fs::remove_file(&temp);
    }
    result
}

fn write_jpeg(rgb: &RgbImage, path: &Path, quality: u8) -> Result<(), String> {
    use std::io::Write;

    // 编码器按字节小块写出，必须带缓冲，否则每次写入都是一次系统调用，速度慢数十倍。
    let file = std::fs::File::create(path).map_err(|e| format!("无法创建文件: {e}"))?;
    let mut writer = std::io::BufWriter::new(file);
    image::codecs::jpeg::JpegEncoder::new_with_quality(&mut writer, quality)
        .encode(
            rgb.as_raw(),
            rgb.width(),
            rgb.height(),
            image::ExtendedColorType::Rgb8,
        )
        .map_err(|e| format!("编码失败: {e}"))?;
    writer.flush().map_err(|e| format!("写入失败: {e}"))?;
    Ok(())
}

/// 并行读取图片尺寸，供前端预览排版使用。
#[tauri::command]
async fn read_image_sizes(file_paths: Vec<String>) -> Result<Vec<(String, u32, u32)>, String> {
    run_blocking(move || {
        Ok(file_paths
            .par_iter()
            .filter_map(|path| {
                let (width, height) = read_display_size(Path::new(path))?;
                Some((path.clone(), width, height))
            })
            .collect())
    })
    .await
}

/// 只解析文件头获取尺寸，并按 EXIF 方向换算为实际显示的宽高。
fn read_display_size(path: &Path) -> Option<(u32, u32)> {
    use image::ImageReader;

    let reader = ImageReader::open(path).ok()?.with_guessed_format().ok()?;
    let mut decoder = reader.into_decoder().ok()?;
    let (width, height) = decoder.dimensions();
    let rotated = matches!(
        decoder.orientation().unwrap_or(Orientation::NoTransforms),
        Orientation::Rotate90
            | Orientation::Rotate270
            | Orientation::Rotate90FlipH
            | Orientation::Rotate270FlipH
    );

    Some(if rotated { (height, width) } else { (width, height) })
}

// ---------------------------------------------------------------------------
// 京东详情页预览缩略图
// ---------------------------------------------------------------------------

/// 预览缩略图宽度，与原 C# 版本保持一致。
const PREVIEW_THUMBNAIL_WIDTH: u32 = 1024;
/// 生成超过该天数的缩略图在启动时清理，需要时会重新生成。
const PREVIEW_CACHE_MAX_AGE_DAYS: u64 = 7;

fn preview_cache_dir(app: &tauri::AppHandle) -> Result<PathBuf, String> {
    let dir = app
        .path()
        .app_cache_dir()
        .map_err(|e| format!("无法获取缓存目录: {e}"))?
        .join("jd-preview");
    std::fs::create_dir_all(&dir).map_err(|e| format!("无法创建缓存目录: {e}"))?;
    Ok(dir)
}

/// 生成（或复用）京东详情页预览缩略图，返回缩略图文件路径。
/// 缓存按“路径 + 修改时间 + 文件大小”区分，原图被替换后会重新生成。
#[tauri::command]
async fn jd_preview_thumbnail(app: tauri::AppHandle, file_path: String) -> Result<String, String> {
    let cache_dir = preview_cache_dir(&app)?;
    run_blocking(move || {
        ensure_preview_thumbnail(&cache_dir, &file_path)
            .map(|path| path.to_string_lossy().into_owned())
    })
    .await
}

fn ensure_preview_thumbnail(cache_dir: &Path, file_path: &str) -> Result<PathBuf, String> {
    use std::hash::{Hash, Hasher};

    let source = Path::new(&file_path);
    let metadata = std::fs::metadata(source).map_err(|e| format!("无法读取文件信息: {e}"))?;
    let modified = metadata
        .modified()
        .ok()
        .and_then(|time| time.duration_since(std::time::UNIX_EPOCH).ok())
        .map(|duration| duration.as_nanos())
        .unwrap_or_default();

    let mut hasher = std::collections::hash_map::DefaultHasher::new();
    (&file_path, modified, metadata.len(), PREVIEW_THUMBNAIL_WIDTH).hash(&mut hasher);
    let cached = cache_dir.join(format!("{:016x}.jpg", hasher.finish()));

    if cached.exists() {
        return Ok(cached);
    }

    let image = load_oriented_image(source)?;
    let thumbnail = if image.width() > PREVIEW_THUMBNAIL_WIDTH {
        let height = ((image.height() as f64 * PREVIEW_THUMBNAIL_WIDTH as f64
            / image.width() as f64)
            .round() as u32)
            .max(1);
        image::imageops::resize(&image, PREVIEW_THUMBNAIL_WIDTH, height, FilterType::Triangle)
    } else {
        image
    };

    // save_jpeg 先写临时文件再改名，并发请求不会读到写了一半的缩略图。
    if let Err(error) = save_jpeg(&thumbnail, &cached, 85) {
        // 改名失败但目标已存在，说明另一个请求已经写好了缩略图。
        if !cached.exists() {
            return Err(error);
        }
    }
    Ok(cached)
}

/// 清理过期的预览缩略图。
fn prune_preview_cache(app: &tauri::AppHandle) {
    let Ok(dir) = preview_cache_dir(app) else { return };
    let Ok(entries) = std::fs::read_dir(dir) else { return };
    let max_age = std::time::Duration::from_secs(PREVIEW_CACHE_MAX_AGE_DAYS * 24 * 60 * 60);

    for entry in entries.flatten() {
        let expired = entry
            .metadata()
            .and_then(|metadata| metadata.modified())
            .ok()
            .and_then(|modified| modified.elapsed().ok())
            .is_some_and(|age| age > max_age);
        if expired {
            let _ = std::fs::remove_file(entry.path());
        }
    }
}

// ---------------------------------------------------------------------------
// 更新检测
// ---------------------------------------------------------------------------

const LATEST_RELEASE_API: &str = "https://api.github.com/repos/byttao/PigResizeF/releases/latest";
const RELEASE_PAGE_PREFIX: &str = "https://github.com/byttao/PigResizeF/";

/// 有新版本时返回给前端的提示信息。
#[derive(Debug, Clone, PartialEq, Serialize)]
pub struct UpdateInfo {
    pub latest: String,
    pub current: String,
    pub url: String,
}

#[derive(Deserialize)]
struct LatestRelease {
    tag_name: String,
    html_url: String,
}

/// 查询 GitHub 最新 Release，有更高版本时返回提示信息。
/// 网络不可用、超时或数据异常都静默返回 None，不影响本地图片处理。
#[tauri::command]
async fn check_update() -> Option<UpdateInfo> {
    fetch_latest_release().await.ok()?
}

async fn fetch_latest_release() -> Result<Option<UpdateInfo>, reqwest::Error> {
    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(5))
        .user_agent(concat!("PigResize/", env!("CARGO_PKG_VERSION")))
        .build()?;
    let response = client
        .get(LATEST_RELEASE_API)
        .header("Accept", "application/vnd.github+json")
        .send()
        .await?;
    if !response.status().is_success() {
        return Ok(None);
    }

    let release: LatestRelease = response.json().await?;
    Ok(newer_release(&release, env!("CARGO_PKG_VERSION")))
}

/// 仅当 Release 版本高于当前版本且链接指向本仓库时返回提示。
fn newer_release(release: &LatestRelease, current: &str) -> Option<UpdateInfo> {
    let latest = parse_version(&release.tag_name)?;
    if latest <= parse_version(current)? || !release.html_url.starts_with(RELEASE_PAGE_PREFIX) {
        return None;
    }

    Some(UpdateInfo {
        latest: release.tag_name.clone(),
        current: format!("v{current}"),
        url: release.html_url.clone(),
    })
}

/// 解析 `v1.6.3` 这类版本号，与 C# 的 Version.TryParse 一样接受 2~4 段，缺少的段按 0 处理。
fn parse_version(text: &str) -> Option<[u64; 4]> {
    let parts = text
        .trim()
        .trim_start_matches(['v', 'V'])
        .split('.')
        .map(|part| part.parse::<u64>().ok())
        .collect::<Option<Vec<_>>>()?;
    if !(2..=4).contains(&parts.len()) {
        return None;
    }

    let mut version = [0; 4];
    version[..parts.len()].copy_from_slice(&parts);
    Some(version)
}

/// 用系统浏览器打开 Release 页面，只允许本仓库的 GitHub 链接。
#[tauri::command]
fn open_release_page(app: tauri::AppHandle, url: String) -> Result<(), String> {
    use tauri_plugin_opener::OpenerExt;

    if !url.starts_with(RELEASE_PAGE_PREFIX) {
        return Err("链接无效".to_string());
    }
    app.opener()
        .open_url(url, None::<&str>)
        .map_err(|e| format!("无法打开链接: {e}"))
}

fn main() {
    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .plugin(tauri_plugin_opener::init())
        .setup(|app| {
            let handle = app.handle().clone();
            std::thread::spawn(move || prune_preview_cache(&handle));
            Ok(())
        })
        .invoke_handler(tauri::generate_handler![
            batch_resize,
            batch_crop,
            crop_mode_options,
            detect_crop_mode_command,
            jd_export,
            read_image_sizes,
            jd_preview_thumbnail,
            check_update,
            open_release_page,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn update_check_compares_versions() {
        let release = |tag: &str| LatestRelease {
            tag_name: tag.to_string(),
            html_url: format!("{RELEASE_PAGE_PREFIX}releases/tag/{tag}"),
        };

        assert!(newer_release(&release("v1.6.4"), "1.6.3").is_some());
        assert!(newer_release(&release("V1.7"), "1.6.3").is_some());
        assert!(newer_release(&release("v1.6.3"), "1.6.3").is_none());
        assert!(newer_release(&release("v1.6.2"), "1.6.3").is_none());
        assert!(newer_release(&release("v1.10.0"), "1.9.9").is_some());
        assert!(newer_release(&release("nightly"), "1.6.3").is_none());

        let foreign = LatestRelease {
            tag_name: "v9.0.0".to_string(),
            html_url: "https://example.com/v9".to_string(),
        };
        assert!(newer_release(&foreign, "1.6.3").is_none());
    }

    #[test]
    fn preview_thumbnail_is_scaled_and_reused() {
        let dir = std::env::temp_dir().join(format!("pig-resize-test-{}", std::process::id()));
        let cache_dir = dir.join("cache");
        std::fs::create_dir_all(&cache_dir).unwrap();

        let source = dir.join("wide.jpg");
        let image = RgbImage::from_pixel(3000, 1500, Rgb([200, 80, 40]));
        save_jpeg(&image, &source, 90).unwrap();
        let source = source.to_string_lossy().into_owned();

        let first = ensure_preview_thumbnail(&cache_dir, &source).unwrap();
        let thumbnail = image::open(&first).unwrap();
        assert_eq!((thumbnail.width(), thumbnail.height()), (PREVIEW_THUMBNAIL_WIDTH, 512));
        assert_eq!(read_display_size(Path::new(&source)), Some((3000, 1500)));

        // 第二次请求直接命中缓存，不会重新写文件。
        let written_at = std::fs::metadata(&first).unwrap().modified().unwrap();
        let second = ensure_preview_thumbnail(&cache_dir, &source).unwrap();
        assert_eq!(first, second);
        assert_eq!(std::fs::metadata(&second).unwrap().modified().unwrap(), written_at);

        // 缓存目录里不应残留临时文件。
        let files = std::fs::read_dir(&cache_dir).unwrap().count();
        assert_eq!(files, 1);

        std::fs::remove_dir_all(&dir).unwrap();
    }
}
