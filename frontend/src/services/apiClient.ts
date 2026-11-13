import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import { AuthResponseDto } from "@/types/auth";

/**
 * Base Axios Client kết nối trực tiếp đến Backend .NET 10 Minimal APIs.
 * Tuân thủ quy chuẩn Tiêu chí 1 Lab 6 & SPEC Tích Hợp:
 * - Tự động gắn JWT Bearer Token vào Request Header.
 * - withCredentials: true để bảo mật cookie RefreshToken.
 * - Response Interceptor: Mutex / Request Queue tự động Refresh Token khi gặp lỗi 401.
 * - Tránh triệt để vòng lặp 401 vô hạn và đá người dùng về /login khi Refresh Token hết hạn.
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

// Quản lý trạng thái Mutex Lock & Request Queue cho quá trình Refresh Token
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else if (token) {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Response Interceptor: Xử lý tập trung lỗi HTTP & Tự động Silent Refresh Token
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as (InternalAxiosRequestConfig & { _retry?: boolean }) | undefined;

    // Bỏ qua nếu không phải mã lỗi 401 hoặc thiếu config
    if (!error.response || error.response.status !== 401 || !originalRequest) {
      return Promise.reject(error);
    }

    // Các endpoint xác thực (auth) không được refresh khi trả về 401 để tránh infinite loop
    const requestUrl = originalRequest.url || "";
    const isAuthEndpoint =
      requestUrl.includes("/auth/login") ||
      requestUrl.includes("/auth/register") ||
      requestUrl.includes("/auth/refresh") ||
      requestUrl.includes("/auth/google") ||
      requestUrl.includes("/auth/logout");

    if (isAuthEndpoint || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Không refresh nếu người dùng đang ở trang đăng nhập hoặc đăng ký
    if (typeof window !== "undefined") {
      const pathname = window.location.pathname;
      if (pathname === "/login" || pathname === "/register") {
        return Promise.reject(error);
      }
    }

    // Nếu đang có một tiến trình refresh token đang diễn ra -> Đưa request này vào hàng đợi (Queue)
    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        failedQueue.push({ resolve, reject });
      })
        .then((token) => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`;
          }
          return apiClient(originalRequest);
        })
        .catch((err) => Promise.reject(err));
    }

    // Đánh dấu đã retry và kích hoạt Mutex Lock
    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const currentRefreshToken = typeof window !== "undefined" ? localStorage.getItem("refreshToken") : null;

      // Gọi endpoint /auth/refresh trực tiếp qua axios gốc để tránh interceptor lặp lại
      const refreshResponse = await axios.post<AuthResponseDto>(
        `${apiClient.defaults.baseURL || "http://localhost:5000/api/v1"}/auth/refresh`,
        currentRefreshToken ? { refreshToken: currentRefreshToken } : {},
        { withCredentials: true }
      );

      const authData = refreshResponse.data;
      const newAccessToken = authData.accessToken;

      // Cập nhật localStorage
      if (typeof window !== "undefined") {
        localStorage.setItem("accessToken", newAccessToken);
        if (authData.refreshToken) {
          localStorage.setItem("refreshToken", authData.refreshToken);
        }
        if (authData.userId) localStorage.setItem("userId", authData.userId);
        if (authData.email) localStorage.setItem("userEmail", authData.email);
        if (authData.displayName) localStorage.setItem("userDisplayName", authData.displayName);
        if (authData.roles) localStorage.setItem("userRoles", JSON.stringify(authData.roles));

        // Phát sự kiện đồng bộ toàn bộ ứng dụng
        const { AUTH_CHANGED_EVENT } = await import("./authService");
        window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
      }

      // Cập nhật Zustand Reactive Store
      try {
        const { useAuthStore } = await import("@/stores/auth-store");
        useAuthStore.getState().login(authData);
      } catch {
        // Fallback nếu Zustand chưa sẵn sàng
      }

      // Giải phóng hàng đợi với Access Token mới
      processQueue(null, newAccessToken);

      // Thử lại request ban đầu với Access Token mới
      if (originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;
      }
      return apiClient(originalRequest);
    } catch (refreshError) {
      // Refresh thất bại (Refresh Token hết hạn hẳn, bị thu hồi hoặc phiên bị hủy)
      processQueue(refreshError, null);

      if (typeof window !== "undefined") {
        // Dọn dẹp phiên làm việc
        try {
          const { useAuthStore } = await import("@/stores/auth-store");
          useAuthStore.getState().logout();
        } catch {
          localStorage.removeItem("accessToken");
          localStorage.removeItem("refreshToken");
          localStorage.removeItem("userId");
          localStorage.removeItem("userEmail");
          localStorage.removeItem("userDisplayName");
          localStorage.removeItem("userRoles");
        }

        const currentPath = window.location.pathname;
        if (currentPath !== "/login" && currentPath !== "/register") {
          const redirect = encodeURIComponent(currentPath + window.location.search);
          window.location.href = `/login?redirect=${redirect}`;
        }
      }

      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);
