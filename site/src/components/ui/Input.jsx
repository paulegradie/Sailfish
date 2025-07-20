import { forwardRef, useState } from 'react'
import { cn } from '@/lib/utils'

/**
 * Enhanced Input component with comprehensive design system integration
 * 
 * Features:
 * - Multiple sizes (sm, md, lg)
 * - Icon support (left and right)
 * - Error states
 * - Loading states
 * - Password visibility toggle
 * - Consistent with design tokens
 */

const inputVariants = {
  size: {
    sm: 'px-3 py-1.5 text-sm',
    md: 'px-3 py-2 text-base', // Default
    lg: 'px-4 py-3 text-lg',
  },
}

const Input = forwardRef(({
  className,
  type = 'text',
  size = 'md',
  error = false,
  loading = false,
  leftIcon,
  rightIcon,
  showPasswordToggle = false,
  ...props
}, ref) => {
  const [showPassword, setShowPassword] = useState(false)
  const [inputType, setInputType] = useState(type)

  const handlePasswordToggle = () => {
    setShowPassword(!showPassword)
    setInputType(showPassword ? 'password' : 'text')
  }

  const hasLeftIcon = leftIcon || loading
  const hasRightIcon = rightIcon || (showPasswordToggle && type === 'password')

  return (
    <div className="relative">
      {hasLeftIcon && (
        <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          {loading ? (
            <div className="loading-spinner w-4 h-4" />
          ) : (
            leftIcon
          )}
        </div>
      )}
      
      <input
        type={showPasswordToggle && type === 'password' ? inputType : type}
        className={cn(
          // Base form input styles from our design system
          'form-input',
          inputVariants.size[size],
          {
            'pl-10': hasLeftIcon,
            'pr-10': hasRightIcon,
            'border-error-300 focus:border-error-500 focus:ring-error-500': error,
            'opacity-50 cursor-not-allowed': props.disabled,
          },
          className
        )}
        ref={ref}
        {...props}
      />
      
      {hasRightIcon && (
        <div className="absolute inset-y-0 right-0 pr-3 flex items-center">
          {showPasswordToggle && type === 'password' ? (
            <button
              type="button"
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 focus:outline-none"
              onClick={handlePasswordToggle}
              tabIndex={-1}
            >
              {showPassword ? (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.878 9.878L3 3m6.878 6.878L21 21" />
                </svg>
              ) : (
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
              )}
            </button>
          ) : (
            rightIcon
          )}
        </div>
      )}
    </div>
  )
})

Input.displayName = 'Input'

// Label component
export const Label = forwardRef(({
  className,
  required = false,
  children,
  ...props
}, ref) => {
  return (
    <label
      ref={ref}
      className={cn('form-label', className)}
      {...props}
    >
      {children}
      {required && (
        <span className="text-error-500 ml-1">*</span>
      )}
    </label>
  )
})

Label.displayName = 'Label'

// Error message component
export const ErrorMessage = forwardRef(({
  className,
  children,
  ...props
}, ref) => {
  if (!children) return null

  return (
    <p
      ref={ref}
      className={cn('form-error', className)}
      {...props}
    >
      {children}
    </p>
  )
})

ErrorMessage.displayName = 'ErrorMessage'

// Helper text component
export const HelperText = forwardRef(({
  className,
  children,
  ...props
}, ref) => {
  if (!children) return null

  return (
    <p
      ref={ref}
      className={cn(
        'text-sm text-gray-500 dark:text-gray-400 mt-1',
        className
      )}
      {...props}
    >
      {children}
    </p>
  )
})

HelperText.displayName = 'HelperText'

// Form field wrapper component
export const FormField = ({
  label,
  error,
  helperText,
  required = false,
  className,
  children,
  ...props
}) => {
  return (
    <div className={cn('space-y-2', className)} {...props}>
      {label && (
        <Label required={required}>
          {label}
        </Label>
      )}
      
      {children}
      
      <ErrorMessage>{error}</ErrorMessage>
      <HelperText>{helperText}</HelperText>
    </div>
  )
}

// Textarea component
export const Textarea = forwardRef(({
  className,
  size = 'md',
  error = false,
  ...props
}, ref) => {
  return (
    <textarea
      className={cn(
        'form-input resize-none',
        inputVariants.size[size],
        {
          'border-error-300 focus:border-error-500 focus:ring-error-500': error,
          'opacity-50 cursor-not-allowed': props.disabled,
        },
        className
      )}
      ref={ref}
      {...props}
    />
  )
})

Textarea.displayName = 'Textarea'

// Select component
export const Select = forwardRef(({
  className,
  size = 'md',
  error = false,
  children,
  ...props
}, ref) => {
  return (
    <select
      className={cn(
        'form-input pr-8 bg-no-repeat bg-right',
        'bg-[url("data:image/svg+xml,%3csvg xmlns=\'http://www.w3.org/2000/svg\' fill=\'none\' viewBox=\'0 0 20 20\'%3e%3cpath stroke=\'%236b7280\' stroke-linecap=\'round\' stroke-linejoin=\'round\' stroke-width=\'1.5\' d=\'m6 8 4 4 4-4\'/%3e%3c/svg%3e")]',
        inputVariants.size[size],
        {
          'border-error-300 focus:border-error-500 focus:ring-error-500': error,
          'opacity-50 cursor-not-allowed': props.disabled,
        },
        className
      )}
      ref={ref}
      {...props}
    >
      {children}
    </select>
  )
})

Select.displayName = 'Select'

// Search input component
export const SearchInput = forwardRef(({
  className,
  onClear,
  value,
  ...props
}, ref) => {
  const searchIcon = (
    <svg className="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
    </svg>
  )

  const clearIcon = value && onClear && (
    <button
      type="button"
      className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 focus:outline-none"
      onClick={onClear}
      tabIndex={-1}
    >
      <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
      </svg>
    </button>
  )

  return (
    <Input
      ref={ref}
      type="search"
      leftIcon={searchIcon}
      rightIcon={clearIcon}
      value={value}
      className={className}
      {...props}
    />
  )
})

SearchInput.displayName = 'SearchInput'

export { Input, inputVariants }
