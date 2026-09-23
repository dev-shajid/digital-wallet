"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  LayoutDashboardIcon,
  ReceiptIcon,
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

// Every user gets these. Money-movement features (expenses now, P2P transfers
// later) each get their own top-level entry here rather than being nested
// under one generic "Transactions" item.
const NAV_ITEMS = [
  { title: "Dashboard", url: "/", icon: LayoutDashboardIcon },
  { title: "Wallets", url: "/wallets", icon: WalletCardsIcon },
  { title: "Expenses", url: "/expenses", icon: ReceiptIcon },
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
