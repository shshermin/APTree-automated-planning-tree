import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],

  server: {
    watch: {
      // If you hit ENOSPC (inotify watcher limit), enable polling:
      //   VITE_USE_POLLING=true npm run dev
      usePolling: process.env.VITE_USE_POLLING === "true",
      interval: 250,
      ignored: [
        "**/node_modules/**",
        "**/.git/**",
        "**/dist/**",
      ],
    },
    proxy: {
      "/api": {
        target: "http://localhost:5254",
        changeOrigin: true,
      },
      "/health": {
        target: "http://localhost:5254",
        changeOrigin: true,
      },
      "/swagger": {
        target: "http://localhost:5254",
        changeOrigin: true,
      },
      "/ws": {
        target: "ws://localhost:5254",
        ws: true,
      },
    },
  },
});
