import axios, { type AxiosRequestConfig, type AxiosInstance } from 'axios';

const setupInterceptors = (instance: AxiosInstance) => {
  instance.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });
  return instance;
};

export const createAxiosInstance = (baseURL: string) => {
  const instance = setupInterceptors(axios.create({ baseURL }));

  return <T>(config: AxiosRequestConfig): Promise<T> => {
    return instance(config).then((response) => response.data);
  };
};

// 默认实例（API 网关）
export const axiosInstance = createAxiosInstance(
  import.meta.env.VITE_API_BASE_URL,
);

export default axiosInstance;
