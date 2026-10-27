/**
 * Định nghĩa các kiểu dữ liệu TypeScript cho Phân hệ Xác thực & Người dùng (FR-AUTH).
 * Khớp 100% với C# Backend .NET 10 DTOs: AuthResponseDto, UserDto, RegisterRequest, LoginRequest.
 */

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
  userName: string;
}

export interface AuthResponseDto {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  userId: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  displayName: string;
  avatarUrl?: string | null;
  bio?: string | null;
  roles: string[];
  createdAt: string;
}

export interface UpdateProfileRequest {
  displayName?: string;
  avatarUrl?: string | null;
  bio?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
  resetToken?: string | null;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmNewPassword: string;
}
