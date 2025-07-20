import { forwardRef } from 'react'
import { cn } from '@/lib/utils'

const buttonVariants = {
  variant: {
    primary: 'btn-primary',
    secondary: 'btn-secondary',
    outline: 'btn-outline',
    ghost: 'btn-ghost',
    success: 'bg-success-600 text-white hover:bg-success-700 focus-visible:ring-success-500 hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0',
    warning: 'bg-warning-600 text-white hover:bg-warning-700 focus-visible:ring-warning-500 hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0',
    error: 'bg-error-600 text-white hover:bg-error-700 focus-visible:ring-error-500 hover:shadow-lg hover:-translate-y-0.5 active:translate-y-0',
  },
  size: {
    sm: 'btn-sm',
    md: 'px-4 py-2 text-base', // Default size
    lg: 'btn-lg',
    xl: 'btn-xl',
  },
}

const Button = forwardRef(({
  className,
  variant = 'primary',
  size = 'md',
  loading = false,
  leftIcon,
  rightIcon,
  children,
  ...props
}, ref) => {
  return (
    <button
      className={cn(
        // Base button styles from our design system
        'btn focus-ring',
        buttonVariants.variant[variant],
        buttonVariants.size[size],
        {
          'opacity-50 cursor-not-allowed': loading || props.disabled,
        },
        className
      )}
      ref={ref}
      disabled={loading || props.disabled}
      {...props}
    >
      {loading && (
        <div className="loading-spinner w-4 h-4 mr-2" />
      )}

      {leftIcon && !loading && (
        <span className="mr-2 flex-shrink-0">
          {leftIcon}
        </span>
      )}

      <span className={cn({ 'opacity-0': loading })}>
        {children}
      </span>

      {rightIcon && !loading && (
        <span className="ml-2 flex-shrink-0">
          {rightIcon}
        </span>
      )}
    </button>
  )
})

Button.displayName = 'Button'

export { Button, buttonVariants }