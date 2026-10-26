import kitPreset from "./src/ui-kit/tailwind.preset.cjs";

/** @type {import('tailwindcss').Config} */
export default {
  presets: [kitPreset],
  content: ["./index.html", "./src/**/*.{js,ts,jsx,tsx}"],
};
