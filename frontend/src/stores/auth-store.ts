import { create } from "zustand";
import { UserDto, AuthResponseDto } from "@/types/auth";

export interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;

  // Actions
  login: (authData: AuthResponseDto) => void;
  logout: () => void;
  setUser: (user: UserDto) => void;
  initAuth: () => void;
  setLoading: (loading: boolean) => void;
}

const STORAGE_KEYS = {
  TOKEN: "accessToken",
  USER_ID: "userId",
  EMAIL: "userEmail",
  DISPLAY_NAME: "userDisplayName",
  ROLES: "userRoles",
};

/**
 * Zustand Store quản lý trạng thái xác thực toàn cục (Tiêu chí 3 Lab 6).
 * Hỗ trợ Reactive State cập nhật Header, Profile và các trang tức thì không cần F5.
 */
export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isLoading: true,

  /**
   * Lưu thông tin đăng nhập và cập nhật Reactive State
   */
  login: (authData: AuthResponseDto) => {
    if (typeof window !== "undefined") {
      localStorage.setItem(STORAGE_KEYS.TOKEN, authData.accessToken);
      localStorage.setItem(STORAGE_KEYS.USER_ID, authData.userId);
      localStorage.setItem(STORAGE_KEYS.EMAIL, authData.email);
      localStorage.setItem(STORAGE_KEYS.DISPLAY_NAME, authData.displayName);
      localStorage.setItem(STORAGE_KEYS.ROLES, JSON.stringify(authData.roles || []));
    }

    const initialUser: UserDto = {
      id: authData.userId,
      email: authData.email,
      displayName: authData.displayName,
      userName: authData.displayName,
      roles: authData.roles || [],
      createdAt: new Date().toISOString(),
      avatarUrl: null,
      bio: null,
    };

    set({
      user: initialUser,
      accessToken: authData.accessToken,
      isAuthenticated: true,
      isLoading: false,
    });
  },

  /**
   * Đăng xuất và dọn dẹp an toàn toàn bộ phiên làm việc
   */
  logout: () => {
    if (typeof window !== "undefined") {
      localStorage.removeItem(STORAGE_KEYS.TOKEN);
      localStorage.removeItem(STORAGE_KEYS.USER_ID);
      localStorage.removeItem(STORAGE_KEYS.EMAIL);
      localStorage.removeItem(STORAGE_KEYS.DISPLAY_NAME);
      localStorage.removeItem(STORAGE_KEYS.ROLES);
    }

    set({
      user: null,
      accessToken: null,
      isAuthenticated: false,
      isLoading: false,
    });
  },

  /**
   * Cập nhật thông tin hồ sơ người dùng (sau khi PATCH profile)
   */
  setUser: (user: UserDto) => {
    if (typeof window !== "undefined") {
      if (user.displayName) {
        localStorage.setItem(STORAGE_KEYS.DISPLAY_NAME, user.displayName);
      }
      if (user.email) {
        localStorage.setItem(STORAGE_KEYS.EMAIL, user.email);
      }
      if (user.roles) {
        localStorage.setItem(STORAGE_KEYS.ROLES, JSON.stringify(user.roles));
      }
    }

    set({ user, isAuthenticated: true, isLoading: false });
  },

  /**
   * Khởi tạo và khôi phục trạng thái xác thực từ localStorage khi tải lại trang
   */
  initAuth: () => {
    if (typeof window === "undefined") {
      set({ isLoading: false });
      return;
    }

    try {
      const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
      const userId = localStorage.getItem(STORAGE_KEYS.USER_ID);
      const email = localStorage.getItem(STORAGE_KEYS.EMAIL);
      const displayName = localStorage.getItem(STORAGE_KEYS.DISPLAY_NAME);
      const rolesStr = localStorage.getItem(STORAGE_KEYS.ROLES);

      if (token && userId && email) {
        let roles: string[] = [];
        try {
          roles = rolesStr ? JSON.parse(rolesStr) : [];
        } catch {
          roles = [];
        }

        const restoredUser: UserDto = {
          id: userId,
          email,
          displayName: displayName || "Người dùng",
          userName: displayName || "User",
          roles: Array.isArray(roles) ? roles : [],
          createdAt: new Date().toISOString(),
          avatarUrl: null,
          bio: null,
        };

        set({
          user: restoredUser,
          accessToken: token,
          isAuthenticated: true,
          isLoading: false,
        });
      } else {
        set({
          user: null,
          accessToken: null,
          isAuthenticated: false,
          isLoading: false,
        });
      }
    } catch {
      set({
        user: null,
        accessToken: null,
        isAuthenticated: false,
        isLoading: false,
      });
    }
  },

  setLoading: (loading: boolean) => set({ isLoading: loading }),
}));
