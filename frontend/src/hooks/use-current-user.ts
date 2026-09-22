import { useQuery } from "@tanstack/react-query"
import { getCurrentUser } from "@/lib/api/users"

export function useCurrentUser() {
  return useQuery({
    queryKey: ["users", "me"],
    queryFn: getCurrentUser,
  })
}
