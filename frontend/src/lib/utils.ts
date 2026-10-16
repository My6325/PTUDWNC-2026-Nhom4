import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

/**
 * Tiện ích chuẩn gộp class names cho shadcn/ui với Tailwind CSS và giải quyết xung đột classes.
 */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
