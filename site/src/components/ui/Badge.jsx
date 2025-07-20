import { forwardRef } from 'react'
import { cn } from '@/lib/utils'

/**
 * Enhanced Badge component with comprehensive design system integration
 * 
 * Features:
 * - Multiple variants (primary, secondary, success, warning, error, info)
 * - Multiple sizes (sm, md, lg)
 * - Icon support
 * - Dismissible badges
 * - Consistent with design tokens
 */

const badgeVariants = {
  variant: {
    primary: 'badge-primary',
    secondary: 'bg-gray-100 text-gray-800 dark:bg-gray-800 dark:text-gray-200',
    success: 'badge-success',
    warning: 'badge-warning',
    error: 'badge-error',
    info: 'bg-info-100 text-info-800 dark:bg-info-900/20 dark:text-info-400',
    outline: 'border border-gray-300 text-gray-700 dark:border-gray-600 dark:text-gray-300',
  },
  size: {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-0.5 text-xs', // Default
    lg: 'px-3 py-1 text-sm',
  },
}

const Badge = forwardRef(({
  className,
  variant = 'primary',
  size = 'md',
  children,
  leftIcon,
  rightIcon,
  dismissible = false,
  onDismiss,
  ...props
}, ref) => {
  return (
    <span
      ref={ref}
      className={cn(
        'badge inline-flex items-center gap-1 font-medium rounded-full',
        badgeVariants.variant[variant],
        badgeVariants.size[size],
        className
      )}
      {...props}
    >
      {leftIcon && (
        <span className="flex-shrink-0">
          {leftIcon}
        </span>
      )}
      
      <span>{children}</span>
      
      {rightIcon && !dismissible && (
        <span className="flex-shrink-0">
          {rightIcon}
        </span>
      )}
      
      {dismissible && (
        <button
          type="button"
          className="flex-shrink-0 ml-1 hover:bg-black/10 dark:hover:bg-white/10 rounded-full p-0.5 transition-colors"
          onClick={onDismiss}
          aria-label="Remove badge"
        >
          <svg className="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z" clipRule="evenodd" />
          </svg>
        </button>
      )}
    </span>
  )
})

Badge.displayName = 'Badge'

// Convenience components for different variants
export const PrimaryBadge = (props) => <Badge variant="primary" {...props} />
export const SecondaryBadge = (props) => <Badge variant="secondary" {...props} />
export const SuccessBadge = (props) => <Badge variant="success" {...props} />
export const WarningBadge = (props) => <Badge variant="warning" {...props} />
export const ErrorBadge = (props) => <Badge variant="error" {...props} />
export const InfoBadge = (props) => <Badge variant="info" {...props} />
export const OutlineBadge = (props) => <Badge variant="outline" {...props} />

// Status badge component
export const StatusBadge = forwardRef(({
  status,
  className,
  ...props
}, ref) => {
  const statusVariants = {
    online: { variant: 'success', children: 'Online' },
    offline: { variant: 'error', children: 'Offline' },
    away: { variant: 'warning', children: 'Away' },
    busy: { variant: 'error', children: 'Busy' },
    idle: { variant: 'secondary', children: 'Idle' },
  }

  const statusConfig = statusVariants[status] || statusVariants.offline

  return (
    <Badge
      ref={ref}
      className={cn('relative', className)}
      {...statusConfig}
      {...props}
    >
      <span className="flex items-center gap-1.5">
        <span className={cn(
          'w-2 h-2 rounded-full',
          {
            'bg-success-500': status === 'online',
            'bg-error-500': status === 'offline' || status === 'busy',
            'bg-warning-500': status === 'away',
            'bg-gray-400': status === 'idle',
          }
        )} />
        {statusConfig.children}
      </span>
    </Badge>
  )
})

StatusBadge.displayName = 'StatusBadge'

// Notification badge (dot badge)
export const NotificationBadge = forwardRef(({
  count,
  max = 99,
  showZero = false,
  className,
  children,
  ...props
}, ref) => {
  const displayCount = count > max ? `${max}+` : count

  if (!showZero && (!count || count === 0)) {
    return children ? <span className="relative">{children}</span> : null
  }

  if (!children) {
    // Standalone notification badge
    return (
      <Badge
        ref={ref}
        variant="error"
        size="sm"
        className={cn('min-w-[1.25rem] h-5 px-1', className)}
        {...props}
      >
        {displayCount}
      </Badge>
    )
  }

  // Badge with children (positioned absolutely)
  return (
    <span className="relative inline-block">
      {children}
      <Badge
        ref={ref}
        variant="error"
        size="sm"
        className={cn(
          'absolute -top-1 -right-1 min-w-[1.25rem] h-5 px-1',
          'transform translate-x-1/2 -translate-y-1/2',
          className
        )}
        {...props}
      >
        {displayCount}
      </Badge>
    </span>
  )
})

NotificationBadge.displayName = 'NotificationBadge'

// Badge group component
export const BadgeGroup = ({ 
  children, 
  className,
  spacing = 'normal',
  ...props 
}) => {
  const spacingClasses = {
    tight: 'gap-1',
    normal: 'gap-2',
    loose: 'gap-3',
  }

  return (
    <div
      className={cn(
        'flex flex-wrap items-center',
        spacingClasses[spacing],
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
}

export { Badge, badgeVariants }
