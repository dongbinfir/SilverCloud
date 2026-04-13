import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react()
  ],
  resolve: {
    alias: {
      "@identity-api": path.resolve(__dirname, "src/api/identity_api.ts"),
      "@components": path.resolve(__dirname, "src/components"),
      "@layouts": path.resolve(__dirname, "src/layouts"),
      "@router": path.resolve(__dirname, "src/router"),
      "@store": path.resolve(__dirname, "src/store"),
      "@pages": path.resolve(__dirname, "src/pages"),
    },
  },
  server: {
    port: 7041,
    proxy: {
      "/identity": {
        target: "https://localhost:7060",
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
