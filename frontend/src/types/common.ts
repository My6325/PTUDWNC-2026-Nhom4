/**
 * Định nghĩa các kiểu dữ liệu chung (Common DTOs) cho toàn bộ Frontend Culinary Blog.
 * Khớp 100% với cấu trúc Common Domain & Problem Details RFC 7807 của Backend .NET 10.
 */

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Cấu trúc lỗi chuẩn RFC 7807 Problem Details từ ASP.NET Core Minimal APIs
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
