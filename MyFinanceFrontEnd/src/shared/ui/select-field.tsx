import { useEffect, useId, useRef, useState, type KeyboardEvent, type ReactNode } from "react"
import { Check, ChevronDown } from "lucide-react"
import { cn } from "@/shared/lib/utils"

export type SelectOption<T extends string> = {
  value: T
  label: string
  /** Icone opcional exibido antes do rotulo. */
  icon?: ReactNode
}

type SelectFieldProps<T extends string> = {
  label: string
  options: ReadonlyArray<SelectOption<T>>
  value: T | null
  onChange: (value: T) => void
  placeholder?: string
  disabled?: boolean
  className?: string
}

/**
 * Campo de selecao com lista customizada (listbox). Teclado: Enter/Espaco/
 * setas abrem; setas/Home/End navegam; Enter escolhe; Esc e clique fora
 * fecham. Apresentacao pura.
 */
export function SelectField<T extends string>({
  label,
  options,
  value,
  onChange,
  placeholder = "Selecione",
  disabled,
  className,
}: SelectFieldProps<T>) {
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)
  const rootRef = useRef<HTMLDivElement>(null)
  const buttonRef = useRef<HTMLButtonElement>(null)
  const baseId = useId()
  const listId = `${baseId}-list`
  const selectedIndex = options.findIndex((o) => o.value === value)
  const selected = selectedIndex >= 0 ? options[selectedIndex] : null

  useEffect(() => {
    if (!open) return
    function handlePointerDown(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener("mousedown", handlePointerDown)
    return () => document.removeEventListener("mousedown", handlePointerDown)
  }, [open])

  function openList() {
    setActive(selectedIndex >= 0 ? selectedIndex : 0)
    setOpen(true)
  }

  function choose(index: number) {
    onChange(options[index].value)
    setOpen(false)
    buttonRef.current?.focus()
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (!open) {
      if (["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) {
        event.preventDefault()
        openList()
      }
      return
    }
    const last = options.length - 1
    switch (event.key) {
      case "ArrowDown":
        event.preventDefault()
        setActive((i) => (i >= last ? 0 : i + 1))
        break
      case "ArrowUp":
        event.preventDefault()
        setActive((i) => (i <= 0 ? last : i - 1))
        break
      case "Home":
        event.preventDefault()
        setActive(0)
        break
      case "End":
        event.preventDefault()
        setActive(last)
        break
      case "Enter":
      case " ":
        event.preventDefault()
        if (options.length > 0) choose(active)
        break
      case "Escape":
        // Fecha so a lista; nao propaga para um dialog que a contenha.
        event.preventDefault()
        event.nativeEvent.stopImmediatePropagation()
        setOpen(false)
        break
      case "Tab":
        setOpen(false)
        break
    }
  }

  return (
    <div ref={rootRef} className={cn("relative flex flex-col gap-1.5", className)}>
      <span id={`${baseId}-label`} className="text-[13px] text-text-muted">
        {label}
      </span>
      <button
        ref={buttonRef}
        type="button"
        role="combobox"
        aria-haspopup="listbox"
        aria-expanded={open}
        aria-controls={listId}
        aria-labelledby={`${baseId}-label`}
        aria-activedescendant={open ? `${baseId}-opt-${active}` : undefined}
        disabled={disabled}
        onClick={() => (open ? setOpen(false) : openList())}
        onKeyDown={handleKeyDown}
        className={cn(
          "flex w-full items-center justify-between gap-2 rounded-[10px] border border-border bg-accent px-3.5 py-3 text-left text-sm outline-none",
          "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 disabled:cursor-not-allowed disabled:opacity-50",
        )}
      >
        <span
          className={cn("flex min-w-0 items-center gap-2", selected ? "text-text-body" : "text-text-faint")}
        >
          {selected?.icon}
          <span className="truncate">{selected ? selected.label : placeholder}</span>
        </span>
        <ChevronDown
          className={cn("size-3.5 shrink-0 text-text-muted transition-transform", open && "rotate-180")}
          aria-hidden="true"
        />
      </button>
      {open ? (
        <ul
          id={listId}
          role="listbox"
          aria-labelledby={`${baseId}-label`}
          className="absolute top-full z-20 mt-1 max-h-60 w-full overflow-y-auto rounded-xl border border-border bg-popover p-1"
        >
          {options.map((option, index) => (
            <li
              key={option.value}
              id={`${baseId}-opt-${index}`}
              role="option"
              aria-selected={option.value === value}
              onMouseEnter={() => setActive(index)}
              onClick={() => choose(index)}
              className={cn(
                "flex cursor-pointer items-center justify-between gap-2 rounded-lg px-3 py-2.5 text-sm text-text-body",
                index === active && "bg-accent",
              )}
            >
              <span className="flex min-w-0 items-center gap-2">
                {option.icon}
                <span className="truncate">{option.label}</span>
              </span>
              {option.value === value ? (
                <Check className="size-4 shrink-0 text-primary" aria-hidden="true" />
              ) : null}
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  )
}
