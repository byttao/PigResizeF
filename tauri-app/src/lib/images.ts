/**
 * 后端支持的图片格式，与原 C# 版本的 IsImageFile 保持一致。
 * 拖拽（useFileDrop）与文件选择框共用；ui-kit 的默认列表还包含 webp / tif，这里不用。
 */
export const IMAGE_EXTENSIONS = ["jpg", "jpeg", "png", "bmp", "gif"];
