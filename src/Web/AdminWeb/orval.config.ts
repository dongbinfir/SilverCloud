import { defineConfig } from 'orval';

export default defineConfig({
  user: {
    input: {
      target: 'https://localhost:7060/openapi/v1.json',
    },
    output: {
      target: './src/api/user_api.ts',
      client: 'axios',
      override: {
        mutator: {
          path: './src/api/axios-instance.ts',
          name: 'axiosInstance',
        },
      },
    },
  },
});