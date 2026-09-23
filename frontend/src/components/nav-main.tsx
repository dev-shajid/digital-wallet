"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  ArrowLeftRightIcon,
  LayoutDashboardIcon,
  ShapesIcon,
  UserRoundIcon,
  WalletCardsIcon,
} from "lucide-react"
import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
} from "@/components/ui/sidebar"
import { useAuthStore } from "@/store/auth-store"

// Every user gets these. "Transactions" covers every kind of money movement
// (expenses now, P2P transfers later) in one list - see the transactions page
// for how new transaction types get added to it.
const NAV_ITEMS = [
  { title: "Dashboard", url: "/", icon: LayoutDashboardIcon },
  { title: "Wallets", url: "/wallets", icon: WalletCardsIcon },
  { title: "Transactions", url: "/transactions", icon: ArrowLeftRightIcon },
  { title: "Profile", url: "/profile", icon: UserRoundIcon },
]

const ADMIN_NAV_ITEMS = [
  {
    title: "Expense Categories",
    url: "/admin/expense-categories",
    icon: ShapesIcon,
  },
]

export function NavMain() {
  const pathname = usePathname()
  const role = useAuthStore((state) => state.user?.role)

  const items = role === "ADMIN" ? [...NAV_ITEMS, ...ADMIN_NAV_ITEMS] : NAV_ITEMS

  return (
    <SidebarGroup>
      <SidebarGroupLabel>Menu</SidebarGroupLabel>
      <SidebarMenu>
        {items.map((item) => (
          <SidebarMenuItem key={item.url}>
            <SidebarMenuButton
              asChild
              tooltip={item.title}
              isActive={pathname === item.url}
            >
              <Link href={item.url}>
                <item.icon />
                <span>{item.title}</span>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        ))}
      </SidebarMenu>
    </SidebarGroup>
  )
}
