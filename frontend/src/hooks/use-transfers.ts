import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query"
import { getMyWallet } from "@/lib/api/wallets"
import { getTransferHistory, lookupAccount, sendTransfer } from "@/lib/api/transfers"

export function useMyWallet() {
  return useQuery({
    queryKey: ["wallet", "me"],
    queryFn: async () => {
      const res = await getMyWallet()
      return res.data
    },
  })
}

export function useTransferHistory() {
  return useQuery({
    queryKey: ["transfers", "history"],
    queryFn: async () => {
      const res = await getTransferHistory()
      return res.data ?? []
    },
  })
}

export function useAccountLookup(accountNo: string) {
  const trimmed = accountNo.trim()
  return useQuery({
    queryKey: ["users", "lookup", trimmed],
    queryFn: async () => {
      const res = await lookupAccount(trimmed)
      return res.data
    },
    enabled: trimmed.length === 10 && /^AC\d{8}$/i.test(trimmed),
    retry: false,
  })
}

export function useSendTransfer() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: sendTransfer,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["wallet", "me"] })
      queryClient.invalidateQueries({ queryKey: ["transfers", "history"] })
    },
  })
}
