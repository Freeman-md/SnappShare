import { defineVitestConfig } from '@nuxt/test-utils/config'
import path from 'path';

export default defineVitestConfig({
  resolve: {
    alias: {
      '~': path.resolve(__dirname, './'),
      '@': path.resolve(__dirname, './'),
    },
  },
  test: {
    environment: 'nuxt'
  },
});
