"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import {
  ArrowLeftRightIcon,
  HistoryIcon,
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

// Shown to everyone, regardless of role.
const COMMON_NAV_ITEMS = [
  { title: "Dashboard", url: "/", icon: LayoutDashboardIcon },
]

// Admins don't have a personal wallet to manage - they manage users and
// system settings instead - so these money-movement pages are user-only.
const USER_NAV_ITEMS = [
  { title: "Wallets", url: "/wallets", icon: WalletCardsIcon },
  { title: "Transactions", url: "/transactions", icon: HistoryIcon },
  { title: "Expenses", url: "/expenses", icon: ReceiptIcon },
  { title: "Transfers", url: "/transfers", icon: ArrowLeftRightIcon },
]

// TODO: add a "Users" item here (list/manage users, view a user's
// transactions) once that admin feature has a backend endpoint - it doesn't
// exist yet.
const ADMIN_NAV_ITEMS = [
  {
    title: "Expense Categories",
    url: "/admin/expense-categories",
    icon: ShapesIcon,
  },
]

const TRAILING_NAV_ITEMS = [
  { title: "Profile", url: "/profile", icon: UserRoundIcon },
]

export function NavMain() {
  const pathname = usePathname()
  const role = useAuthStore((state) => state.user?.role)

  const items = [
    ...COMMON_NAV_ITEMS,
    ...(role === "ADMIN" ? ADMIN_NAV_ITEMS : USER_NAV_ITEMS),
    ...TRAILING_NAV_ITEMS,
  ]

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
