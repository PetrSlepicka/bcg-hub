import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  base: "/bcg-hub/",
  plugins: [react()],
  server: { proxy: { "/api": { target: "http://localhost:5090", changeOrigin: true } } }
});
