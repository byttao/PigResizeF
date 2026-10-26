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

export interface BatchResult {
  success: boolean;
  message: string;
  output_count: number;
  failed_files: string[];
}
