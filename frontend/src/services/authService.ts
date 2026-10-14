import { apiClient } from "./apiClient";
import {
  LoginRequest,
  RegisterRequest,
  AuthResponseDto,
  UserDto,
  UpdateProfileRequest,
} from "@/types/auth";

export const AUTH_CHANGED_EVENT = "culinary_blog_auth_changed";

export interface StoredUser {
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
}

/**
 * Service xác thực & quản lý phiên người dùng (FR-AUTH).
 * Tuân thủ quy chuẩn an ninh nghiêm ngặt:
 * - KHÔNG lưu RefreshToken vào localStorage hoặc sessionStorage.
 * - Chỉ lưu AccessToken ngắn hạn và thông tin hồ sơ hiển thị cơ bản.
 * - Phát sự kiện AUTH_CHANGED_EVENT để Header và các trang tự động đồng bộ trạng thái.
 */
export const authService = {
  /**
   * Đăng nhập bằng Email và Mật khẩu (FR-AUTH-001)
   */
  async login(request: LoginRequest): Promise<AuthResponseDto> {
    const response = await apiClient.post<AuthResponseDto>("/auth/login", request);
    const data = response.data;

    if (typeof window !== "undefined") {
      localStorage.setItem("accessToken", data.accessToken);
      localStorage.setItem("userId", data.userId);
      localStorage.setItem("userEmail", data.email);
      localStorage.setItem("userDisplayName", data.displayName);
      localStorage.setItem("userRoles", JSON.stringify(data.roles || []));

      // Thông báo cho toàn bộ ứng dụng cập nhật giao diện
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    }

    return data;
  },

  /**
   * Đăng ký tài khoản mới (FR-AUTH-002)
   */
  async register(request: RegisterRequest): Promise<AuthResponseDto> {
    const response = await apiClient.post<AuthResponseDto>("/auth/register", request);
    const data = response.data;

    if (typeof window !== "undefined") {
      localStorage.setItem("accessToken", data.accessToken);
      localStorage.setItem("userId", data.userId);
      localStorage.setItem("userEmail", data.email);
      localStorage.setItem("userDisplayName", data.displayName);
      localStorage.setItem("userRoles", JSON.stringify(data.roles || []));

      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    }

    return data;
  },

  /**
   * Đăng xuất và xóa phiên làm việc (FR-AUTH-003)
   */
  async logout(): Promise<void> {
    try {
      // Backend /auth/logout hỗ trợ đọc cookie HttpOnly refreshToken hoặc body
      await apiClient.post("/auth/logout");
    } catch {
      // Dù API có lỗi mạng, client vẫn xóa thông tin phiên để bảo vệ người dùng
    } finally {
      if (typeof window !== "undefined") {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("userId");
        localStorage.removeItem("userEmail");
        localStorage.removeItem("userDisplayName");
        localStorage.removeItem("userRoles");

        window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
      }
    }
  },

  /**
   * Lấy thông tin chi tiết người dùng hiện tại từ máy chủ (GET /api/v1/auth/me)
   */
  async getCurrentUser(): Promise<UserDto> {
    const response = await apiClient.get<UserDto>("/auth/me");
    const data = response.data;

    if (typeof window !== "undefined" && data) {
      if (data.displayName) {
        localStorage.setItem("userDisplayName", data.displayName);
      }
      if (data.email) {
        localStorage.setItem("userEmail", data.email);
      }
      if (data.roles) {
        localStorage.setItem("userRoles", JSON.stringify(data.roles));
      }
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    }

    return data;
  },

  /**
   * Cập nhật thông tin hồ sơ cá nhân (PATCH /api/v1/auth/profile)
   */
  async updateProfile(request: UpdateProfileRequest): Promise<UserDto> {
    const response = await apiClient.patch<UserDto>("/auth/profile", request);
    const data = response.data;

    if (typeof window !== "undefined" && data.displayName) {
      localStorage.setItem("userDisplayName", data.displayName);
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
    }

    return data;
  },

  /**
   * Lấy thông tin tóm tắt người dùng đã lưu trữ ở Client
   */
  getStoredUser(): StoredUser | null {
    if (typeof window === "undefined") return null;

    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    try {
      const rolesStr = localStorage.getItem("userRoles");
      const roles = rolesStr ? JSON.parse(rolesStr) : [];
      return {
        userId: localStorage.getItem("userId") || "",
        email: localStorage.getItem("userEmail") || "",
        displayName: localStorage.getItem("userDisplayName") || "Người dùng",
        roles: Array.isArray(roles) ? roles : [],
      };
    } catch {
      return null;
    }
  },

  /**
   * Kiểm tra nhanh trạng thái đăng nhập
   */
  isAuthenticated(): boolean {
    if (typeof window === "undefined") return false;
    return !!localStorage.getItem("accessToken");
  },

  /**
   * Lấy AccessToken hiện tại
   */
  getAccessToken(): string | null {
    if (typeof window === "undefined") return null;
    return localStorage.getItem("accessToken");
  },
};
