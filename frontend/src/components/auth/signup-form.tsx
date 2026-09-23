"use client"

import { zodResolver } from "@hookform/resolvers/zod"
import Link from "next/link"
import { useEffect, useRef, useState } from "react"
import { Controller, useForm } from "react-hook-form"
import * as z from "zod"

import { Button } from "@/components/ui/button"
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  useInitiateRegistration,
  useResendOtp,
  useVerifyEmail,
} from "@/hooks/use-auth"
import { getApiErrorMessage } from "@/lib/api/error"
import { cn } from "@/lib/utils"

// ─── Step 1 schema ────────────────────────────────────────────────────────────
const registrationSchema = z
  .object({
    name: z
      .string()
      .min(1, "Full name is required.")
      .max(100, "Full name must be at most 100 characters."),
    email: z
      .string()
      .min(1, "Email is required.")
      .email("Enter a valid email address."),
    password: z
      .string()
      .min(8, "Password must be at least 8 characters long."),
    confirmPassword: z.string().min(1, "Please confirm your password."),
  })
  .refine((data) => data.password === data.confirmPassword, {
    message: "Passwords do not match.",
    path: ["confirmPassword"],
  })

// ─── Step 2 schema ────────────────────────────────────────────────────────────
const otpSchema = z.object({
  otp: z
    .string()
    .length(6, "Verification code must be exactly 6 digits.")
    .regex(/^\d+$/, "Verification code must contain digits only."),
})

type RegistrationValues = z.infer<typeof registrationSchema>
type OtpValues = z.infer<typeof otpSchema>

// ─────────────────────────────────────────────────────────────────────────────

const RESEND_COOLDOWN_SECONDS = 60

export function SignupForm({
  className,
  ...props
}: React.ComponentProps<"div">) {
  // Which screen is showing: "register" | "otp"
  const [step, setStep] = useState<"register" | "otp">("register")

  // The email that was submitted in Step 1 — needed to call verify-email & resend-otp
  const [pendingEmail, setPendingEmail] = useState("")

  // Feedback message shown after a successful resend
  const [resendMessage, setResendMessage] = useState("")

  // Countdown timer for the resend button
  const [resendCooldown, setResendCooldown] = useState(0)
  const cooldownRef = useRef<ReturnType<typeof setInterval> | null>(null)

  // ── Step 1 form ─────────────────────────────────────────────────────────────
  const registrationForm = useForm<RegistrationValues>({
    resolver: zodResolver(registrationSchema),
    defaultValues: { name: "", email: "", password: "", confirmPassword: "" },
  })

  const initiateMutation = useInitiateRegistration(() => {
    // OTP sent successfully — store the email and switch screens
    setPendingEmail(registrationForm.getValues("email").trim().toLowerCase())
    setStep("otp")
    startResendCooldown()
  })

  function onRegistrationSubmit(data: RegistrationValues) {
    initiateMutation.mutate({
      name: data.name,
      email: data.email,
      password: data.password,
    })
  }

  // ── Step 2 form ─────────────────────────────────────────────────────────────
  const otpForm = useForm<OtpValues>({
    resolver: zodResolver(otpSchema),
    defaultValues: { otp: "" },
  })

  const verifyMutation = useVerifyEmail()

  function onOtpSubmit(data: OtpValues) {
    setResendMessage("") // clear any "resent" banner when attempting verify
    verifyMutation.mutate({ email: pendingEmail, otp: data.otp })
  }

  // ── Resend OTP ───────────────────────────────────────────────────────────────
  const resendMutation = useResendOtp((message) => {
    setResendMessage(message)
    otpForm.reset()
    startResendCooldown()
  })

  function startResendCooldown() {
    setResendCooldown(RESEND_COOLDOWN_SECONDS)
    if (cooldownRef.current) clearInterval(cooldownRef.current)
    cooldownRef.current = setInterval(() => {
      setResendCooldown((prev) => {
        if (prev <= 1) {
          clearInterval(cooldownRef.current!)
          return 0
        }
        return prev - 1
      })
    }, 1000)
  }

  // Clean up interval on unmount
  useEffect(() => () => { if (cooldownRef.current) clearInterval(cooldownRef.current) }, [])

  // ── Render ───────────────────────────────────────────────────────────────────
  return (
    <div className={cn("flex flex-col gap-6", className)} {...props}>
      <Card>

        {/* ── STEP 1: Registration form ── */}
        {step === "register" && (
          <>
            <CardHeader className="text-center">
              <CardTitle className="text-xl">Create your account</CardTitle>
              <CardDescription>
                Enter your details below to get started
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={registrationForm.handleSubmit(onRegistrationSubmit)} noValidate>
                <FieldGroup>
                  {initiateMutation.isError && (
                    <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                      {getApiErrorMessage(initiateMutation.error)}
                    </div>
                  )}

                  <Controller
                    name="name"
                    control={registrationForm.control}
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid}>
                        <FieldLabel htmlFor={field.name}>Full Name</FieldLabel>
                        <Input
                          {...field}
                          id={field.name}
                          type="text"
                          placeholder="John Doe"
                          aria-invalid={fieldState.invalid}
                          autoComplete="name"
                        />
                        {fieldState.invalid && (
                          <FieldError errors={[fieldState.error]} />
                        )}
                      </Field>
                    )}
                  />

                  <Controller
                    name="email"
                    control={registrationForm.control}
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid}>
                        <FieldLabel htmlFor={field.name}>Email</FieldLabel>
                        <Input
                          {...field}
                          id={field.name}
                          type="email"
                          placeholder="m@example.com"
                          aria-invalid={fieldState.invalid}
                          autoComplete="email"
                        />
                        {fieldState.invalid && (
                          <FieldError errors={[fieldState.error]} />
                        )}
                      </Field>
                    )}
                  />

                  <Field>
                    <Field className="grid grid-cols-2 gap-4">
                      <Controller
                        name="password"
                        control={registrationForm.control}
                        render={({ field, fieldState }) => (
                          <Field data-invalid={fieldState.invalid}>
                            <FieldLabel htmlFor={field.name}>Password</FieldLabel>
                            <Input
                              {...field}
                              id={field.name}
                              type="password"
                              placeholder="••••••••"
                              aria-invalid={fieldState.invalid}
                              autoComplete="new-password"
                            />
                            {fieldState.invalid && (
                              <FieldError errors={[fieldState.error]} />
                            )}
                          </Field>
                        )}
                      />
                      <Controller
                        name="confirmPassword"
                        control={registrationForm.control}
                        render={({ field, fieldState }) => (
                          <Field data-invalid={fieldState.invalid}>
                            <FieldLabel htmlFor={field.name}>
                              Confirm Password
                            </FieldLabel>
                            <Input
                              {...field}
                              id={field.name}
                              type="password"
                              placeholder="••••••••"
                              aria-invalid={fieldState.invalid}
                              autoComplete="new-password"
                            />
                            {fieldState.invalid && (
                              <FieldError errors={[fieldState.error]} />
                            )}
                          </Field>
                        )}
                      />
                    </Field>
                    <FieldDescription>
                      Must be at least 8 characters long.
                    </FieldDescription>
                  </Field>

                  <Field>
                    <Button
                      type="submit"
                      disabled={
                        registrationForm.formState.isSubmitting ||
                        initiateMutation.isPending
                      }
                    >
                      {initiateMutation.isPending
                        ? "Sending verification code..."
                        : "Continue"}
                    </Button>
                    <FieldDescription className="text-center">
                      Already have an account?{" "}
                      <Link href="/sign-in">Sign in</Link>
                    </FieldDescription>
                  </Field>
                </FieldGroup>
              </form>
            </CardContent>
          </>
        )}

        {/* ── STEP 2: OTP verification screen ── */}
        {step === "otp" && (
          <>
            <CardHeader className="text-center">
              <CardTitle className="text-xl">Check your email</CardTitle>
              <CardDescription>
                We sent a 6-digit verification code to{" "}
                <span className="font-medium text-foreground">{pendingEmail}</span>.
                Enter it below to confirm your email.
              </CardDescription>
            </CardHeader>
            <CardContent>
              <form onSubmit={otpForm.handleSubmit(onOtpSubmit)} noValidate>
                <FieldGroup>
                  {/* Success feedback after resend */}
                  {resendMessage && (
                    <div className="rounded-md border border-green-500/30 bg-green-500/10 px-3 py-2 text-sm text-green-700 dark:text-green-400">
                      {resendMessage}
                    </div>
                  )}

                  {/* Error from verify attempt */}
                  {verifyMutation.isError && (
                    <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                      {getApiErrorMessage(verifyMutation.error)}
                    </div>
                  )}

                  {/* Error from resend attempt */}
                  {resendMutation.isError && (
                    <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                      {getApiErrorMessage(resendMutation.error)}
                    </div>
                  )}

                  <Controller
                    name="otp"
                    control={otpForm.control}
                    render={({ field, fieldState }) => (
                      <Field data-invalid={fieldState.invalid}>
                        <FieldLabel htmlFor={field.name}>
                          Verification Code
                        </FieldLabel>
                        <Input
                          {...field}
                          id={field.name}
                          type="text"
                          inputMode="numeric"
                          maxLength={6}
                          placeholder="123456"
                          aria-invalid={fieldState.invalid}
                          autoComplete="one-time-code"
                          className="text-center tracking-[0.5em] text-lg"
                        />
                        {fieldState.invalid && (
                          <FieldError errors={[fieldState.error]} />
                        )}
                        <FieldDescription>
                          The code expires in 10 minutes.
                        </FieldDescription>
                      </Field>
                    )}
                  />

                  <Field>
                    <Button
                      type="submit"
                      disabled={verifyMutation.isPending}
                    >
                      {verifyMutation.isPending
                        ? "Verifying..."
                        : "Verify & Create Account"}
                    </Button>

                    {/* Resend link with cooldown */}
                    <FieldDescription className="text-center">
                      Didn&apos;t get a code?{" "}
                      {resendCooldown > 0 ? (
                        <span className="text-muted-foreground">
                          Resend in {resendCooldown}s
                        </span>
                      ) : (
                        <button
                          type="button"
                          className="underline underline-offset-4 hover:text-primary disabled:opacity-50"
                          disabled={resendMutation.isPending}
                          onClick={() => {
                            setResendMessage("")
                            resendMutation.mutate({ email: pendingEmail })
                          }}
                        >
                          {resendMutation.isPending ? "Sending..." : "Resend code"}
                        </button>
                      )}
                    </FieldDescription>

                    {/* Back link */}
                    <FieldDescription className="text-center">
                      <button
                        type="button"
                        className="underline underline-offset-4 hover:text-primary"
                        onClick={() => {
                          setStep("register")
                          otpForm.reset()
                          verifyMutation.reset()
                          setResendMessage("")
                        }}
                      >
                        ← Use a different email
                      </button>
                    </FieldDescription>
                  </Field>
                </FieldGroup>
              </form>
            </CardContent>
          </>
        )}

      </Card>
    </div>
  )
}

