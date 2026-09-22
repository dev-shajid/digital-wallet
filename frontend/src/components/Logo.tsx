import { Wallet } from 'lucide-react'
import Link from 'next/link'
import React from 'react'

export default function Logo() {
    return (
        <Link href="#" className="flex items-center gap-2 self-center font-medium">
            <div className="flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground">
                <Wallet className="size-4" />
            </div>
            Diggital Wallet
        </Link>
    )
}
