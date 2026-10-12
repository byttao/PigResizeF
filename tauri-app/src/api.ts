import { invoke } from '@tauri-apps/api/core';
import type {
  ResizeResult,
  CropResult,
  JdExportResult,
  DetectResult,
  ResizeStyle,
  CropMode,
} from './types';

export async function batchResize(
  filePaths: string[],
  style: ResizeStyle
): Promise<ResizeResult> {
  return await invoke('batch_resize', { filePaths, style });
}

export async function detectImageCropMode(filePath: string): Promise<DetectResult> {
  return await invoke('detect_image_crop_mode', { filePath });
}

export async function batchCrop(
  jobs: Array<[string, CropMode]>,
  customOutputDir?: string
): Promise<CropResult> {
  return await invoke('batch_crop', {
    jobs,
    customOutputDir: customOutputDir || null,
  });
}

export async function jdExport(
  filePaths: string[],
  outputDir: string
): Promise<JdExportResult> {
  return await invoke('jd_export', { filePaths, outputDir });
}

export async function getImageInfo(filePath: string): Promise<[number, number]> {
  return await invoke('get_image_info', { filePath });
}

export async function checkUpdate(): Promise<string | null> {
  return await invoke('check_update');
}
