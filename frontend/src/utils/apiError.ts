import axios, { AxiosError } from "axios";
import { ProblemDetails } from "@/types/common";

/**
 * Trích xuất thông tin lỗi chuẩn RFC 7807 Problem Details từ AxiosError.
 */
export function extractProblemDetails(error: unknown): ProblemDetails | null {
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as ProblemDetails;
    return {
      type: data.type,
      title: data.title || "Lỗi yêu cầu",
      status: error.response.status,
      detail: data.detail || error.message,
      errors: data.errors,
    };
  }
  return null;
}

/**
 * Phân rã lỗi validation theo từng trường (Field-level validation errors) từ ProblemDetails
 * Phục vụ gán trực tiếp vào React Hook Form hoặc hiển thị inline dưới input.
 */
export function parseValidationErrors(error: unknown): Record<string, string> {
  const result: Record<string, string> = {};
  if (axios.isAxiosError(error) && error.response?.data) {
    const data = error.response.data as ProblemDetails;
    if (data.errors) {
      for (const [key, messages] of Object.entries(data.errors)) {
        if (messages && messages.length > 0) {
          result[key.toLowerCase()] = messages[0];
        }
      }
    }
  }
  return result;
}

/**
 * Trả về thông điệp lỗi ngắn gọn, thân thiện bằng tiếng Việt để hiển thị lên giao diện.
 */
export function getErrorMessage(error: unknown, fallbackMessage = "Đã xảy ra lỗi không xác định. Vui lòng thử lại sau."): string {
  if (axios.isAxiosError(error)) {
    const status = error.response?.status;
    const data = error.response?.data as ProblemDetails | undefined;

    // Ưu tiên detail cụ thể từ Backend
    if (data?.detail) {
      return data.detail;
    }

    // Nếu có lỗi validation theo trường
    if (data?.errors) {
      const firstField = Object.keys(data.errors)[0];
      if (firstField && data.errors[firstField]?.length > 0) {
        return data.errors[firstField][0];
      }
    }

    // Map mã HTTP chuẩn sang tiếng Việt thân thiện
    switch (status) {
      case 400:
        return "Yêu cầu không hợp lệ. Vui lòng kiểm tra lại thông tin đã nhập.";
      case 401:
        return "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.";
      case 403:
        return "Bạn không có quyền thực hiện thao tác này.";
      case 404:
        return "Không tìm thấy dữ liệu hoặc trang yêu cầu.";
      case 409:
        return "Dữ liệu bị xung đột hoặc đã tồn tại trong hệ thống.";
      case 422:
        return "Dữ liệu không đáp ứng quy chuẩn thẩm định của công thức/tài khoản.";
      case 423:
        return "Tài khoản tạm thời bị khóa 15 phút do nhập sai mật khẩu nhiều lần.";
      case 429:
        return "Bạn đã gửi quá nhiều yêu cầu. Vui lòng chờ 1 phút rồi thử lại.";
      case 500:
      case 502:
      case 503:
        return "Máy chủ đang bảo trì hoặc gặp sự cố kết nối. Vui lòng thử lại sau.";
      default:
        if (error.message === "Network Error") {
          return "Không thể kết nối đến máy chủ. Vui lòng kiểm tra kết nối mạng hoặc Backend API.";
        }
        return data?.title || error.message || fallbackMessage;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallbackMessage;
}

/**
 * Bí danh cho hàm getErrorMessage để tương thích với các component
 */
export const extractApiErrorMessage = getErrorMessage;
