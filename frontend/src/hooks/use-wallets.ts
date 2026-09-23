import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query"
import {
  cashInWallet,
  getWallets,
} from "@/lib/api/wallets"

export function useWallets() {
  return useQuery({
    queryKey: ["wallets"],
    queryFn: getWallets,
  })
}

export function useCashIn() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({
      walletId,
      amount,
    }: {
      walletId: string
      amount: number
    }) => cashInWallet(walletId, { amount }),

    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["wallets"],
      })
    },
  })
}