"use client"

import { useState } from "react"
import { Controller, useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import * as z from "zod"
import {
  LoaderCircleIcon,
  PencilIcon,
  PlusIcon,
  ShapesIcon,
  Trash2Icon,
} from "lucide-react"

import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { RequireRole } from "@/components/guards/require-role"
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field"
import { Input } from "@/components/ui/input"
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet"
import {
  useCreateExpenseCategory,
  useDeleteExpenseCategory,
  useExpenseCategories,
  useUpdateExpenseCategory,
} from "@/hooks/use-expense-categories"
import { getApiErrorMessage } from "@/lib/api/error"
import type { ExpenseCategory } from "@/types/expense"

const categoryFormSchema = z.object({
  name: z
    .string()
    .min(2, "Name must be at least 2 characters.")
    .max(100, "Name cannot exceed 100 characters."),
  description: z
    .string()
    .max(500, "Description cannot exceed 500 characters.")
    .optional(),
})

type CategoryFormValues = z.infer<typeof categoryFormSchema>

export default function ExpenseCategoriesPage() {
  const categoriesQuery = useExpenseCategories()
  const createMutation = useCreateExpenseCategory()
  const updateMutation = useUpdateExpenseCategory()
  const toggleMutation = useUpdateExpenseCategory()
  const deleteMutation = useDeleteExpenseCategory()

  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingCategory, setEditingCategory] = useState<ExpenseCategory | null>(
    null
  )
  const [pendingDeleteId, setPendingDeleteId] = useState<string | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const form = useForm<CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: { name: "", description: "" },
  })

  const categories = categoriesQuery.data?.data ?? []
  const saveMutation = editingCategory ? updateMutation : createMutation

  function openCreateForm() {
    setEditingCategory(null)
    form.reset({ name: "", description: "" })
    createMutation.reset()
    setIsFormOpen(true)
  }

  function openEditForm(category: ExpenseCategory) {
    setEditingCategory(category)
    form.reset({ name: category.name, description: category.description })
    updateMutation.reset()
    setIsFormOpen(true)
  }

  function closeForm(open: boolean) {
    setIsFormOpen(open)

    if (!open) {
      setEditingCategory(null)
      form.reset({ name: "", description: "" })
      createMutation.reset()
      updateMutation.reset()
    }
  }

  function onSubmit(values: CategoryFormValues) {
    const payload = {
      name: values.name,
      description: values.description || undefined,
    }

    if (editingCategory) {
      updateMutation.mutate(
        { id: editingCategory.id, payload },
        { onSuccess: (response) => response.success && setIsFormOpen(false) }
      )
    } else {
      createMutation.mutate(payload, {
        onSuccess: (response) => response.success && setIsFormOpen(false),
      })
    }
  }

  function toggleStatus(category: ExpenseCategory) {
    toggleMutation.mutate({
      id: category.id,
      payload: {
        status: category.status === "ACTIVE" ? "INACTIVE" : "ACTIVE",
      },
    })
  }

  function handleDelete(category: ExpenseCategory) {
    setDeleteError(null)
    setPendingDeleteId(category.id)

    deleteMutation.mutate(category.id, {
      onError: (error) => setDeleteError(getApiErrorMessage(error)),
      onSettled: () => setPendingDeleteId(null),
    })
  }

  return (
    <RequireRole role="ADMIN">
      <div className="flex flex-col gap-6">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h1 className="text-2xl font-semibold">Expense Categories</h1>
            <p className="text-muted-foreground">
              Manage the categories users can pick when recording an expense.
            </p>
          </div>

          <Button type="button" onClick={openCreateForm}>
            <PlusIcon />
            New category
          </Button>
        </div>

        {deleteError && (
          <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
            {deleteError}
          </div>
        )}

        {categoriesQuery.isLoading ? (
          <div className="flex flex-col gap-3">
            {[1, 2, 3].map((item) => (
              <div key={item} className="h-16 animate-pulse rounded-xl bg-muted" />
            ))}
          </div>
        ) : categoriesQuery.isError ? (
          <div className="flex flex-col gap-4">
            <div className="rounded-md border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
              {getApiErrorMessage(categoriesQuery.error)}
            </div>
            <Button
              type="button"
              variant="outline"
              className="self-start"
              onClick={() => categoriesQuery.refetch()}
            >
              Try again
            </Button>
          </div>
        ) : categories.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
              <ShapesIcon className="size-10 text-muted-foreground" />
              <div>
                <h2 className="font-medium">No categories yet</h2>
                <p className="text-sm text-muted-foreground">
                  Create one so users can start recording expenses.
                </p>
              </div>
            </CardContent>
          </Card>
        ) : (
          <Card>
            <CardContent className="divide-y p-0">
              {categories.map((category) => (
                <div
                  key={category.id}
                  className="flex items-center justify-between gap-4 px-4 py-3"
                >
                  <div className="min-w-0">
                    <div className="flex items-center gap-2">
                      <p className="font-medium">{category.name}</p>
                      <button
                        type="button"
                        onClick={() => toggleStatus(category)}
                        disabled={toggleMutation.isPending}
                        className="rounded-full bg-muted px-2.5 py-0.5 text-xs font-medium transition-colors hover:bg-muted/70 disabled:pointer-events-none disabled:opacity-50"
                      >
                        {category.status === "ACTIVE" ? "Active" : "Inactive"}
                      </button>
                    </div>
                    {category.description && (
                      <p className="truncate text-sm text-muted-foreground">
                        {category.description}
                      </p>
                    )}
                  </div>

                  <div className="flex shrink-0 items-center gap-1">
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => openEditForm(category)}
                    >
                      <PencilIcon />
                      <span className="sr-only">Edit</span>
                    </Button>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon-sm"
                      onClick={() => handleDelete(category)}
                      disabled={pendingDeleteId === category.id}
                    >
                      {pendingDeleteId === category.id ? (
                        <LoaderCircleIcon className="animate-spin" />
                      ) : (
                        <Trash2Icon />
                      )}
                      <span className="sr-only">Delete</span>
                    </Button>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>
        )}
      </div>

      <Sheet open={isFormOpen} onOpenChange={closeForm}>
        <SheetContent side="right">
          <SheetHeader>
            <SheetTitle>
              {editingCategory ? "Edit category" : "New category"}
            </SheetTitle>
            <SheetDescription>
              {editingCategory
                ? "Update the name or description. Toggle the status from the list instead."
                : "New categories start out Active."}
            </SheetDescription>
          </SheetHeader>

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="flex flex-1 flex-col"
            noValidate
          >
            <div className="flex-1 px-4">
              <FieldGroup>
                {saveMutation.isError && (
                  <div className="rounded-md border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive">
                    {getApiErrorMessage(saveMutation.error)}
                  </div>
                )}

                <Controller
                  name="name"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Name</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        placeholder="e.g. Food & Dining"
                        aria-invalid={fieldState.invalid}
                        disabled={saveMutation.isPending}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />

                <Controller
                  name="description"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>
                        Description (optional)
                      </FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        placeholder="e.g. Meals, groceries, restaurants"
                        aria-invalid={fieldState.invalid}
                        disabled={saveMutation.isPending}
                      />
                      <FieldDescription>
                        Shown to users when they pick a category.
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
              <Button type="submit" disabled={saveMutation.isPending}>
                {saveMutation.isPending && (
                  <LoaderCircleIcon className="animate-spin" />
                )}
                {saveMutation.isPending
                  ? "Saving..."
                  : editingCategory
                    ? "Save changes"
                    : "Create category"}
              </Button>
            </SheetFooter>
          </form>
        </SheetContent>
      </Sheet>
    </RequireRole>
  )
}
