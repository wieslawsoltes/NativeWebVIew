import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './specs',
  timeout: 30_000,
  expect: {
    timeout: 10_000,
  },
  use: {
    baseURL: 'http://127.0.0.1:8080',
    trace: 'on-first-retry',
  },
  webServer: {
    command: 'python3 -m http.server 8080 --directory ../../site',
    cwd: '.',
    url: 'http://127.0.0.1:8080/index.html',
    reuseExistingServer: !process.env.CI,
  },
});
