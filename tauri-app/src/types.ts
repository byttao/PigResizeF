export type CropMode = 'auto' | 'randomcorners' | 'grid2' | 'grid3';

export type ResizeStyle = 'Square1440' | 'Long1440' | 'Square800' | 'ProductLong';

export interface ResizeResult {
  success: boolean;
  message: string;
  failed_files: string[];
}

export interface CropResult {
  success: boolean;
  message: string;
  output_count: number;
  failed_files: string[];
}

export interface JdExportResult {
  success: boolean;
  message: string;
  output_paths: string[];
}

export interface DetectResult {
  mode: CropMode;
}

export interface CropJob {
  path: string;
  mode: CropMode;
}

export interface ImageFile {
  path: string;
  name: string;
}

export interface CropFileItem extends ImageFile {
  mode: CropMode;
}

export type LogLevel = 'info' | 'success' | 'warning' | 'error' | 'processing';

export interface LogMessage {
  id: string;
  text: string;
  level: LogLevel;
  timestamp: Date;
}
