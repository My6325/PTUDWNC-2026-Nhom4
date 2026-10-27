"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { categoryService } from "@/services/categoryService";
import type { CategoryDto } from "@/types/category";
import { getErrorMessage } from "@/utils/apiError";

export function useRecipeCategories() {
  const [categories, setCategories] = useState<CategoryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const request = useRef<AbortController | null>(null);

  const retry = useCallback(async () => {
    request.current?.abort();
    const controller = new AbortController();
    request.current = controller;
    setIsLoading(true);
    setError(null);
    try {
      const result = await categoryService.getCategories(controller.signal);
      if (!controller.signal.aborted) setCategories(result);
    } catch (error) {
      if (!controller.signal.aborted) setError(getErrorMessage(error));
    } finally {
      if (!controller.signal.aborted) setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void retry();
    return () => request.current?.abort();
  }, [retry]);

  return { categories, isLoading, error, retry };
}
