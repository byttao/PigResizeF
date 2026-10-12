mod image_processor;

use image_processor::{
    crop_image, detect_crop_mode, export_jd_segments, resize_image, stitch_jd_images, CropMode,
    ResizeStyle,
};
use rayon::prelude::*;
use serde::{Deserialize, Serialize};
use std::path::PathBuf;
use tauri::Manager;

#[derive(Debug, Serialize, Deserialize)]
struct ResizeResult {
    success: bool,
    message: String,
    failed_files: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
struct CropResult {
    success: bool,
    message: String,
    output_count: usize,
    failed_files: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
struct JdExportResult {
    success: bool,
    message: String,
    output_paths: Vec<String>,
}

#[derive(Debug, Serialize, Deserialize)]
struct DetectResult {
    mode: CropMode,
}

/// 批量转换图片
#[tauri::command]
async fn batch_resize(
    file_paths: Vec<String>,
    style: ResizeStyle,
) -> Result<ResizeResult, String> {
    let results: Vec<_> = file_paths
        .par_iter()
        .map(|path| {
            let input_path = PathBuf::from(path);
            let parent = input_path.parent().unwrap_or_else(|| std::path::Path::new("."));
            let file_stem = input_path
                .file_stem()
                .and_then(|s| s.to_str())
                .unwrap_or("output");

            let suffix = match style {
                ResizeStyle::Square1440 => "1440方图",
                ResizeStyle::Long1440 => "1440长图",
                ResizeStyle::Square800 => "800方图",
                ResizeStyle::ProductLong => "商品长图",
            };

            let output_path = parent.join(format!("{}-{}.jpg", file_stem, suffix));

            match resize_image(&input_path, &output_path, style) {
                Ok(_) => Ok(()),
                Err(_) => Err(path.clone()),
            }
        })
        .collect();

    let failed_files: Vec<String> = results.into_iter().filter_map(|r| r.err()).collect();

    let success = failed_files.is_empty();
    let message = if success {
        format!("成功转换 {} 张图片", file_paths.len())
    } else {
        format!(
            "转换完成，{} 张成功，{} 张失败",
            file_paths.len() - failed_files.len(),
            failed_files.len()
        )
    };

    Ok(ResizeResult {
        success,
        message,
        failed_files,
    })
}

/// 检测图片裁切模式
#[tauri::command]
async fn detect_image_crop_mode(file_path: String) -> Result<DetectResult, String> {
    let img = image::open(&file_path).map_err(|e| e.to_string())?;
    let mode = detect_crop_mode(&img);
    Ok(DetectResult { mode })
}

/// 批量裁切图片
#[tauri::command]
async fn batch_crop(
    jobs: Vec<(String, CropMode)>,
    custom_output_dir: Option<String>,
) -> Result<CropResult, String> {
    let results: Vec<_> = jobs
        .into_iter()
        .map(|(path, mode)| {
            let input_path = PathBuf::from(&path);
            let output_dir = if let Some(ref custom_dir) = custom_output_dir {
                PathBuf::from(custom_dir)
            } else {
                input_path
                    .parent()
                    .unwrap_or_else(|| std::path::Path::new("."))
                    .to_path_buf()
            };

            std::fs::create_dir_all(&output_dir).ok();

            match crop_image(&input_path, &output_dir, mode) {
                Ok(outputs) => Ok(outputs.len()),
                Err(_) => Err(path),
            }
        })
        .collect();

    let mut output_count = 0;
    let mut failed_files = Vec::new();

    for result in results {
        match result {
            Ok(count) => output_count += count,
            Err(path) => failed_files.push(path),
        }
    }

    let success = failed_files.is_empty();
    let message = if success {
        format!("成功输出 {} 张图片", output_count)
    } else {
        format!(
            "裁切完成，输出 {} 张图片，{} 个文件失败",
            output_count,
            failed_files.len()
        )
    };

    Ok(CropResult {
        success,
        message,
        output_count,
        failed_files,
    })
}

/// 京东详情页拼接与导出
#[tauri::command]
async fn jd_export(file_paths: Vec<String>, output_dir: String) -> Result<JdExportResult, String> {
    if file_paths.is_empty() {
        return Err("没有图片可导出".to_string());
    }

    let paths: Vec<PathBuf> = file_paths.iter().map(PathBuf::from).collect();
    let output_dir_path = PathBuf::from(&output_dir);

    std::fs::create_dir_all(&output_dir_path).map_err(|e| e.to_string())?;

    // 拼接图片
    let stitched = stitch_jd_images(&paths, 790).map_err(|e| e.to_string())?;

    // 分段导出
    let outputs = export_jd_segments(&stitched, &output_dir_path, 1500).map_err(|e| e.to_string())?;

    let output_paths: Vec<String> = outputs
        .iter()
        .filter_map(|p| p.to_str().map(String::from))
        .collect();

    Ok(JdExportResult {
        success: true,
        message: format!("成功导出 {} 张图片", output_paths.len()),
        output_paths,
    })
}

/// 获取图片信息（用于预览）
#[tauri::command]
async fn get_image_info(file_path: String) -> Result<(u32, u32), String> {
    let img = image::open(&file_path).map_err(|e| e.to_string())?;
    let (width, height) = img.dimensions();
    Ok((width, height))
}

/// 检查更新
#[tauri::command]
async fn check_update() -> Result<Option<String>, String> {
    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(5))
        .user_agent("PigResizeF/1.5")
        .build()
        .map_err(|e| e.to_string())?;

    let response = client
        .get("https://api.github.com/repos/byttao/PigResizeF/releases/latest")
        .send()
        .await
        .map_err(|e| e.to_string())?;

    if !response.status().is_success() {
        return Ok(None);
    }

    let release: serde_json::Value = response.json().await.map_err(|e| e.to_string())?;

    let tag_name = release["tag_name"].as_str().unwrap_or("");
    let html_url = release["html_url"].as_str().unwrap_or("");

    if tag_name.is_empty() || html_url.is_empty() {
        return Ok(None);
    }

    // 简单版本比较
    let current_version = "1.5.0";
    if tag_name.trim_start_matches('v') > current_version {
        Ok(Some(html_url.to_string()))
    } else {
        Ok(None)
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .plugin(tauri_plugin_opener::init())
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_fs::init())
        .invoke_handler(tauri::generate_handler![
            batch_resize,
            detect_image_crop_mode,
            batch_crop,
            jd_export,
            get_image_info,
            check_update,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
