export type CropMode = "auto" | "randomcorners" | "grid2" | "grid3";

export type ResizePreset = "Square1440" | "Tall1440" | "Square800" | "ProductTall";

export interface CropModeOption {
  value: CropMode;
  label: string;
}

export interface CropJob {
  path: string;
  mode: CropMode;
}

export interface ProgressEvent {
  current: number;
  total: number;
  file_name: string;
  /** 0~100，综合文件序号与单张内部阶段。 */
  percent: number;
  stage: string;
}

export interface BatchResult {
  success: boolean;
  message: string;
  output_count: number;
  failed_files: string[];
}

export type LogLevel = "info" | "success" | "warning" | "error" | "processing";

export interface LogEntry {
  id: string;
  time: string;
  level: LogLevel;
  message: string;
}
