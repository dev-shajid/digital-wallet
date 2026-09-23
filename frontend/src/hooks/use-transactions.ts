import { useQuery } from "@tanstack/react-query"
import { getTransactions } from "@/lib/api/transactions"

export function useTransactions() {
  return useQuery({
    queryKey: ["transactions"],
    queryFn: getTransactions,
  })
}
