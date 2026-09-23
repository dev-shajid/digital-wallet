import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { createExpense, getExpenses } from "@/lib/api/expenses"

export function useExpenses() {
  return useQuery({
    queryKey: ["expenses"],
    queryFn: getExpenses,
  })
}

export function useCreateExpense() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: createExpense,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["expenses"] })
      // An expense debits the wallet it came from, so the balance shown on
      // /wallets is now stale too.
      queryClient.invalidateQueries({ queryKey: ["wallets"] })
    },
  })
}
