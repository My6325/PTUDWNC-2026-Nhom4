/**
 * Định nghĩa các kiểu dữ liệu TypeScript (TS Types) cho Phân hệ Giám sát Hệ thống (FR-OBS).
 */

export interface HealthEntry {
  name: string;
  status: "Healthy" | "Degraded" | "Unhealthy";
  duration: string;
  description?: string;
  exception?: string | null;
}

export interface OverallHealthReport {
  status: "Healthy" | "Degraded" | "Unhealthy";
  totalDuration: string;
  timestamp: string;
  entries: HealthEntry[];
}
