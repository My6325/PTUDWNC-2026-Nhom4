import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";

/**
 * Base Axios Client kết nối trực tiếp đến Backend .NET 10 Minimal APIs.
 * Tuân thủ quy chuẩn Tiêu chí 1 Lab 6:
 * - Tự động gắn JWT Bearer Token vào Request Header.
 * - withCredentials: true để bảo mật cookie RefreshToken.
 * - Response Interceptor xử lý lỗi 401 và làm sạch phiên khi hết hạn.
 */
export const apiClient = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || "http://localhost:5000/api/v1",
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
  withCredentials: true,
});

// Request Interceptor: Tự động đính kèm Access Token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    if (typeof window !== "undefined") {
      const token = localStorage.getItem("accessToken");
      if (token && config.headers) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Xử lý tập trung lỗi HTTP toàn cục
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    if (error.response?.status === 401 && typeof window !== "undefined") {
      // Khi gặp lỗi 401 Unauthorized, kiểm tra xem có phải đang ở trang login không
      const pathname = window.location.pathname;
      if (pathname !== "/login" && pathname !== "/register") {
        // Reset Zustand store và dọn dẹp localStorage
        try {
          const { useAuthStore } = await import("@/stores/auth-store");
          useAuthStore.getState().logout();
        } catch {
          // Fallback dọn dẹp thủ công
          localStorage.removeItem("accessToken");
          localStorage.removeItem("userId");
          localStorage.removeItem("userEmail");
          localStorage.removeItem("userDisplayName");
          localStorage.removeItem("userRoles");
        }
      }
    }
    return Promise.reject(error);
  }
);
