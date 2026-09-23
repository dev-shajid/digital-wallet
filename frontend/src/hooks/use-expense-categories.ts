import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import {
  createExpenseCategory,
  deleteExpenseCategory,
  getExpenseCategories,
  updateExpenseCategory,
} from "@/lib/api/expense-categories"
import type { UpdateExpenseCategoryPayload } from "@/types/expense"

export function useExpenseCategories() {
  return useQuery({
    queryKey: ["expense-categories"],
    queryFn: getExpenseCategories,
  })
}

export function useCreateExpenseCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createExpenseCategory,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expense-categories"] })
    },
  })
}

export function useUpdateExpenseCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      id,
      payload,
    }: {
      id: string
      payload: UpdateExpenseCategoryPayload
    }) => updateExpenseCategory(id, payload),

    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expense-categories"] })
    },
  })
}

export function useDeleteExpenseCategory() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deleteExpenseCategory(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expense-categories"] })
    },
  })
}
