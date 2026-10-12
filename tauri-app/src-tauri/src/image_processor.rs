use anyhow::{Context, Result};
use image::{DynamicImage, GenericImageView, ImageBuffer, Rgba, RgbaImage};
use serde::{Deserialize, Serialize};
use std::path::{Path, PathBuf};

#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
#[serde(rename_all = "lowercase")]
pub enum CropMode {
    Auto,
    RandomCorners,
    Grid2,
    Grid3,
}

#[derive(Debug, Clone, Copy, Serialize, Deserialize)]
pub enum ResizeStyle {
    Square1440,
    Long1440,
    Square800,
    ProductLong,
}

/// 常规图片转换：调整尺寸并补白背景
pub fn resize_image(
    input_path: &Path,
    output_path: &Path,
    style: ResizeStyle,
) -> Result<()> {
    let img = image::open(input_path)
        .with_context(|| format!("无法打开图片: {:?}", input_path))?;

    let (target_width, target_height) = match style {
        ResizeStyle::Square1440 => (1440, 1440),
        ResizeStyle::Long1440 => (1440, 1920),
        ResizeStyle::Square800 => (800, 800),
        ResizeStyle::ProductLong => {
            let (w, h) = img.dimensions();
            if w >= h {
                (1440, 1440)
            } else {
                (1440, 1920)
            }
        }
    };

    let resized = resize_with_white_background(&img, target_width, target_height);

    resized
        .save_with_format(output_path, image::ImageFormat::Jpeg)
        .with_context(|| format!("无法保存图片: {:?}", output_path))?;

    Ok(())
}

/// 保持比例缩放并用白色背景补齐
fn resize_with_white_background(
    img: &DynamicImage,
    target_width: u32,
    target_height: u32,
) -> RgbaImage {
    let (orig_width, orig_height) = img.dimensions();
    let scale_w = target_width as f32 / orig_width as f32;
    let scale_h = target_height as f32 / orig_height as f32;
    let scale = scale_w.min(scale_h);

    let new_width = (orig_width as f32 * scale) as u32;
    let new_height = (orig_height as f32 * scale) as u32;

    let resized = img.resize_exact(
        new_width,
        new_height,
        image::imageops::FilterType::Lanczos3,
    );

    let mut canvas = RgbaImage::from_pixel(target_width, target_height, Rgba([255, 255, 255, 255]));
    let x_offset = (target_width - new_width) / 2;
    let y_offset = (target_height - new_height) / 2;

    image::imageops::overlay(&mut canvas, &resized.to_rgba8(), x_offset as i64, y_offset as i64);
    canvas
}

/// 自动检测裁切模式
pub fn detect_crop_mode(img: &DynamicImage) -> CropMode {
    let (width, height) = img.dimensions();

    // 检测 1/2 分界线
    if has_horizontal_split_at(img, height / 2, 0.02) {
        return CropMode::Grid2;
    }

    // 检测 1/3 和 2/3 分界线
    if has_horizontal_split_at(img, height / 3, 0.02)
        && has_horizontal_split_at(img, height * 2 / 3, 0.02) {
        return CropMode::Grid3;
    }

    CropMode::RandomCorners
}

/// 检测指定行是否为分界线
fn has_horizontal_split_at(img: &DynamicImage, y: u32, threshold: f32) -> bool {
    let (width, height) = img.dimensions();
    if y >= height {
        return false;
    }

    let check_range = (height as f32 * threshold) as u32;
    let y_start = y.saturating_sub(check_range);
    let y_end = (y + check_range).min(height);

    let mut white_count = 0u32;
    let mut total_count = 0u32;

    for cy in y_start..y_end {
        for x in 0..width {
            let pixel = img.get_pixel(x, cy);
            let r = pixel[0];
            let g = pixel[1];
            let b = pixel[2];

            if r > 250 && g > 250 && b > 250 {
                white_count += 1;
            }
            total_count += 1;
        }
    }

    total_count > 0 && (white_count as f32 / total_count as f32) > 0.95
}

/// 图片裁切：根据模式裁切图片
pub fn crop_image(
    input_path: &Path,
    output_dir: &Path,
    mode: CropMode,
) -> Result<Vec<PathBuf>> {
    let img = image::open(input_path)
        .with_context(|| format!("无法打开图片: {:?}", input_path))?;

    let file_stem = input_path
        .file_stem()
        .and_then(|s| s.to_str())
        .unwrap_or("output");

    let actual_mode = match mode {
        CropMode::Auto => detect_crop_mode(&img),
        other => other,
    };

    match actual_mode {
        CropMode::RandomCorners => crop_random_corners(&img, output_dir, file_stem),
        CropMode::Grid2 => crop_grid(&img, output_dir, file_stem, 2),
        CropMode::Grid3 => crop_grid(&img, output_dir, file_stem, 3),
        CropMode::Auto => unreachable!(),
    }
}

/// 四角乱切：输出 4 张固定尺寸图片
fn crop_random_corners(
    img: &DynamicImage,
    output_dir: &Path,
    file_stem: &str,
) -> Result<Vec<PathBuf>> {
    let (width, height) = img.dimensions();
    let target_w = 1440;
    let target_h = 1920;

    let corners = [
        (0, 0),                                    // 左上
        (width.saturating_sub(target_w), 0),      // 右上
        (0, height.saturating_sub(target_h)),     // 左下
        (width.saturating_sub(target_w), height.saturating_sub(target_h)), // 右下
    ];

    let mut outputs = Vec::new();

    for (i, (x, y)) in corners.iter().enumerate() {
        let crop_w = target_w.min(width - x);
        let crop_h = target_h.min(height - y);

        let cropped = img.crop_imm(*x, *y, crop_w, crop_h);
        let mut canvas = RgbaImage::from_pixel(target_w, target_h, Rgba([255, 255, 255, 255]));
        image::imageops::overlay(&mut canvas, &cropped.to_rgba8(), 0, 0);

        let output_path = output_dir.join(format!("{}-乱切{}.jpg", file_stem, i + 1));
        canvas.save_with_format(&output_path, image::ImageFormat::Jpeg)?;
        outputs.push(output_path);
    }

    Ok(outputs)
}

/// 宫格拆分：2x2 或 3x3
fn crop_grid(
    img: &DynamicImage,
    output_dir: &Path,
    file_stem: &str,
    grid_size: u32,
) -> Result<Vec<PathBuf>> {
    let (width, height) = img.dimensions();
    let cell_w = width / grid_size;
    let cell_h = height / grid_size;

    let mut outputs = Vec::new();
    let mut index = 1;

    for row in 0..grid_size {
        for col in 0..grid_size {
            let x = col * cell_w;
            let y = row * cell_h;

            let cropped = img.crop_imm(x, y, cell_w, cell_h);
            let output_path = output_dir.join(format!("{}-拆分{}.jpg", file_stem, index));
            cropped.save_with_format(&output_path, image::ImageFormat::Jpeg)?;
            outputs.push(output_path);
            index += 1;
        }
    }

    Ok(outputs)
}

/// 京东详情页拼接：返回拼接后的长图
pub fn stitch_jd_images(image_paths: &[PathBuf], output_width: u32) -> Result<DynamicImage> {
    if image_paths.is_empty() {
        anyhow::bail!("没有图片可拼接");
    }

    let mut images = Vec::new();
    let mut total_height = 0u32;

    for path in image_paths {
        let img = image::open(path)
            .with_context(|| format!("无法打开图片: {:?}", path))?;
        let (w, h) = img.dimensions();
        let scaled_h = (h as f32 * output_width as f32 / w as f32) as u32;
        total_height += scaled_h;
        images.push((img, scaled_h));
    }

    let mut canvas = RgbaImage::from_pixel(output_width, total_height, Rgba([255, 255, 255, 255]));
    let mut y_offset = 0u32;

    for (img, scaled_h) in images {
        let resized = img.resize_exact(output_width, scaled_h, image::imageops::FilterType::Lanczos3);
        image::imageops::overlay(&mut canvas, &resized.to_rgba8(), 0, y_offset as i64);
        y_offset += scaled_h;
    }

    Ok(DynamicImage::ImageRgba8(canvas))
}

/// 京东详情页导出：按高度分段保存
pub fn export_jd_segments(
    stitched: &DynamicImage,
    output_dir: &Path,
    segment_height: u32,
) -> Result<Vec<PathBuf>> {
    let (width, height) = stitched.dimensions();
    let mut outputs = Vec::new();
    let mut index = 1;
    let mut y = 0u32;

    while y < height {
        let h = segment_height.min(height - y);
        let segment = stitched.crop_imm(0, y, width, h);

        let output_path = output_dir.join(format!("京东详情-{:03}.jpg", index));
        segment.save_with_format(&output_path, image::ImageFormat::Jpeg)?;
        outputs.push(output_path);

        y += h;
        index += 1;
    }

    Ok(outputs)
}

