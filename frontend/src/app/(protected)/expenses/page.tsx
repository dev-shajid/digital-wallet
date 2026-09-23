"use client"

import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import { LoaderCircleIcon, PlusIcon, ReceiptIcon } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import { useExpenseCategories } from "@/hooks/use-expense-categories"
import { useCreateExpense, useExpenses } from "@/hooks/use-expenses"
import { getApiErrorMessage } from "@/lib/api/error"

function today() {
  return new Date().toISOString().slice(0, 10)
}

const expenseFormSchema = z.object({
  categoryId: z.string().min(1, "Select a category."),
  amount: z.coerce
    .number()
    .min(0.01, "Amount must be greater than zero.")
    .max(99999999999999.9999, "Amount is too large."),
  expenseDate: z
    .string()
    .min(1, "Pick a date.")
    .refine((value) => value <= today(), "Expense date can't be in the future."),
  note: z.string().max(255, "Note cannot exceed 255 characters.").optional(),
})

type ExpenseFormValues = z.infer<typeof expenseFormSchema>

function formatAmount(amount: number) {
  return new Intl.NumberFormat(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 4,
  }).format(amount)
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString(undefined, {
    year: "numeric",
    month: "short",
    day: "numeric",
  })
}

export default function ExpensesPage() {
  const categoriesQuery = useExpenseCategories()
  const expensesQuery = useExpenses()
  const createExpenseMutation = useCreateExpense()

  const [isFormOpen, setIsFormOpen] = useState(false)

  const form = useForm<ExpenseFormValues>({
    resolver: zodResolver(expenseFormSchema),
    defaultValues: {
      categoryId: "",
      amount: 0,
      expenseDate: today(),
      note: "",
    },
  })

  const categories = (categoriesQuery.data?.data ?? []).filter(
    (category) => category.status === "ACTIVE"
  )
  const expenses = expensesQuery.data?.data ?? []

  function openForm() {
    form.reset({ categoryId: "", amount: 0, expenseDate: today(), note: "" })
    createExpenseMutation.reset()
    setIsFormOpen(true)
  }

  function closeForm(open: boolean) {
    setIsFormOpen(open)

    if (!open) {
      form.reset({ categoryId: "", amount: 0, expenseDate: today(), note: "" })
      createExpenseMutation.reset()
    }
  }

  function onSubmit(values: ExpenseFormValues) {
    createExpenseMutation.mutate(
      {
        categoryId: values.categoryId,
        amount: values.amount,
        expenseDate: values.expenseDate,
        note: values.note || undefined,
      },
      {
        onSuccess: (response) => {
          if (response.success) {
            setIsFormOpen(false)
          }
        },
      }
    )
  }

  return (
    <>
      <div className="flex flex-col gap-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold">Expenses</h1>
            <p className="text-muted-foreground">
              Record what you spend from your wallet.
            </p>
          </div>

          <Button
            type="button"
            onClick={openForm}
            disabled={categoriesQuery.isLoading || categories.length === 0}
          >
            <PlusIcon />
            Add expense
          </Button>
        </div>

        {categoriesQuery.data && categories.length === 0 && (
          <div className="rounded-md border border-input bg-muted/50 px-4 py-3 text-sm text-muted-foreground">
            There are no active expense categories yet. Ask an admin to add one
            before you can record an expense.
          </div>
        )}

        {expensesQuery.isLoading ? (
          <div className="flex flex-col gap-3">
            {[1, 2, 3].map((item) => (
              <div key={item} className="h-16 animate-pulse rounded-xl bg-muted" />
            ))}
          </div>
        ) : expensesQuery.isError ? (
          <div className="flex flex-col gap-4">
            <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
              {getApiErrorMessage(expensesQuery.error)}
            </div>
            <Button
              type="button"
              variant="outline"
              className="self-start"
              onClick={() => expensesQuery.refetch()}
            >
              Try again
            </Button>
          </div>
        ) : expenses.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
              <ReceiptIcon className="size-10 text-muted-foreground" />
              <div>
                <h2 className="font-medium">No expenses yet</h2>
                <p className="text-sm text-muted-foreground">
                  Expenses you record will show up here.
                </p>
              </div>
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="divide-y p-0">
              {expenses.map((expense) => (
                <div
                  key={expense.transactionId}
                  className="flex items-center justify-between gap-4 px-4 py-3"
                >
                  <div>
                    <p className="font-medium">{expense.categoryName}</p>
                    <p className="text-sm text-muted-foreground">
                      {formatDate(expense.expenseDate)}
                      {expense.note ? ` · ${expense.note}` : ""}
                    </p>
                  </div>
                  <p className="font-semibold text-destructive shrink-0">
                    -{formatAmount(expense.amount)}
                  </p>
                </div>
              ))}
            </CardContent>
          </Card>
        )}
      </div>

      <Sheet open={isFormOpen} onOpenChange={closeForm}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>Add expense</SheetTitle>
            <SheetDescription>
              Pick a category and amount to debit from your wallet.
            </SheetDescription>
          </SheetHeader>

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-1 flex-col"
            noValidate
          >
            <div className="flex-1 px-4">
              <FieldGroup>
                {createExpenseMutation.isError && (
                  <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    {getApiErrorMessage(createExpenseMutation.error)}
                  </div>
                )}

                <Controller
                  name="categoryId"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Category</FieldLabel>
                      <Select
                        value={field.value}
                        onValueChange={field.onChange}
                        disabled={createExpenseMutation.isPending}
                      >
                        <SelectTrigger
                          id={field.name}
                          aria-invalid={fieldState.invalid}
                        >
                          <SelectValue placeholder="Select a category" />
                        </SelectTrigger>
                        <SelectContent>
                          {categories.map((category) => (
                            <SelectItem key={category.id} value={category.id}>
                              {category.name}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="amount"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Amount</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        type="number"
                        min="0.01"
                        step="0.01"
                        placeholder="0.00"
                        aria-invalid={fieldState.invalid}
                        disabled={createExpenseMutation.isPending}
                        onChange={(event) => field.onChange(event.target.value)}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="expenseDate"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Date</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        type="date"
                        max={today()}
                        aria-invalid={fieldState.invalid}
                        disabled={createExpenseMutation.isPending}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="note"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>
                        Note (optional)
                      </FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        placeholder="e.g. Lunch with team"
                        aria-invalid={fieldState.invalid}
                        disabled={createExpenseMutation.isPending}
                      />
                      <FieldDescription>
                        A short description to help you remember this expense.
                      </FieldDescription>
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />
              </FieldGroup>
            </div>

            <SheetFooter>
              <Button type="submit" disabled={createExpenseMutation.isPending}>
                {createExpenseMutation.isPending && (
                  <LoaderCircleIcon className="animate-spin" />
                )}
                {createExpenseMutation.isPending ? "Saving..." : "Add expense"}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>
    </>
  )
}
