"use client"

import { Avatar, AvatarFallback } from "@/components/ui/avatar"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Separator } from "@/components/ui/separator"
import { Skeleton } from "@/components/ui/skeleton"
import { useCurrentUser } from "@/hooks/use-current-user"
import { getApiErrorMessage } from "@/lib/api/error"
import { initials } from "@/lib/utils"

function ProfileField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm text-muted-foreground">{label}</span>
      <span className="font-medium">{value}</span>
    </div>
  )
}

export default function ProfilePage() {
  const { data: response, isPending, isError, error } = useCurrentUser()
  const user = response?.data

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Profile</h1>
        <p className="text-muted-foreground">Your account details, as stored on the server.</p>
      </div>

      <Card className="max-w-2xl">
        {isPending && (
          <CardHeader>
            <div className="flex items-center gap-4">
              <Skeleton className="h-16 w-16 rounded-full" />
              <div className="flex flex-col gap-2">
                <Skeleton className="h-4 w-40" />
                <Skeleton className="h-3 w-56" />
              </div>
            </div>
          </CardHeader>
        )}

        {isError && (
          <CardHeader>
            <CardTitle className="text-destructive">Couldn&apos;t load your profile</CardTitle>
            <CardDescription>{getApiErrorMessage(error)}</CardDescription>
          </CardHeader>
        )}

        {user && (
          <>
            <CardHeader>
              <div className="flex items-center gap-4">
                <Avatar className="h-16 w-16">
                  <AvatarFallback className="text-lg">{initials(user.name)}</AvatarFallback>
                </Avatar>
                <div>
                  <CardTitle className="text-xl">{user.name}</CardTitle>
                  <CardDescription>{user.email}</CardDescription>
                </div>
              </div>
            </CardHeader>
            <Separator />
            <CardContent className="grid gap-6 pt-6 sm:grid-cols-2">
              <ProfileField label="Account Number" value={user.accountNo} />
              <ProfileField label="Role" value={user.role} />
              <ProfileField
                label="Member Since"
                value={new Date(user.createdAt).toLocaleDateString(undefined, {
                  year: "numeric",
                  month: "long",
                  day: "numeric",
                })}
              />
            </CardContent>
          </>
        )}
      </Card>
    </div>
  )
}
