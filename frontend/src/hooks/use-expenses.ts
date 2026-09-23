import { useMutation, useQueryClient } from "@tanstack/react-query"
import { createExpense } from "@/lib/api/expenses"

export function useCreateExpense() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createExpense,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["transactions"] })
      // An expense debits the wallet it came from, so the balance shown on
      // /wallets is now stale too.
      queryClient.invalidateQueries({ queryKey: ["wallets"] })
    },
  })
}
