import { forwardRef } from 'react'
import { cn } from '@/lib/utils'
import { 
  InformationCircleIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  XCircleIcon,
  LightBulbIcon,
  BookOpenIcon,
  CodeBracketIcon,
  RocketLaunchIcon
} from '@heroicons/react/24/outline'

/**
 * Enhanced Callout component for documentation
 * 
 * Features:
 * - Multiple variants (info, warning, success, error, tip, note, code, example)
 * - Icons and custom styling
 * - Consistent with design tokens
 * - Animation support
 */

const calloutVariants = {
  variant: {
    info: {
      container: 'bg-info-50 border-info-200 dark:bg-info-900/20 dark:border-info-800',
      icon: 'text-info-600 dark:text-info-400',
      title: 'text-info-900 dark:text-info-100',
      content: 'text-info-800 dark:text-info-200',
      iconComponent: InformationCircleIcon,
    },
    warning: {
      container: 'bg-warning-50 border-warning-200 dark:bg-warning-900/20 dark:border-warning-800',
      icon: 'text-warning-600 dark:text-warning-400',
      title: 'text-warning-900 dark:text-warning-100',
      content: 'text-warning-800 dark:text-warning-200',
      iconComponent: ExclamationTriangleIcon,
    },
    success: {
      container: 'bg-success-50 border-success-200 dark:bg-success-900/20 dark:border-success-800',
      icon: 'text-success-600 dark:text-success-400',
      title: 'text-success-900 dark:text-success-100',
      content: 'text-success-800 dark:text-success-200',
      iconComponent: CheckCircleIcon,
    },
    error: {
      container: 'bg-error-50 border-error-200 dark:bg-error-900/20 dark:border-error-800',
      icon: 'text-error-600 dark:text-error-400',
      title: 'text-error-900 dark:text-error-100',
      content: 'text-error-800 dark:text-error-200',
      iconComponent: XCircleIcon,
    },
    tip: {
      container: 'bg-primary-50 border-primary-200 dark:bg-primary-900/20 dark:border-primary-800',
      icon: 'text-primary-600 dark:text-primary-400',
      title: 'text-primary-900 dark:text-primary-100',
      content: 'text-primary-800 dark:text-primary-200',
      iconComponent: LightBulbIcon,
    },
    note: {
      container: 'bg-gray-50 border-gray-200 dark:bg-gray-900/20 dark:border-gray-700',
      icon: 'text-gray-600 dark:text-gray-400',
      title: 'text-gray-900 dark:text-gray-100',
      content: 'text-gray-800 dark:text-gray-200',
      iconComponent: BookOpenIcon,
    },
    code: {
      container: 'bg-gray-900 border-gray-700 dark:bg-gray-800 dark:border-gray-600',
      icon: 'text-gray-400 dark:text-gray-300',
      title: 'text-white dark:text-gray-100',
      content: 'text-gray-300 dark:text-gray-200',
      iconComponent: CodeBracketIcon,
    },
    example: {
      container: 'bg-secondary-50 border-secondary-200 dark:bg-secondary-900/20 dark:border-secondary-800',
      icon: 'text-secondary-600 dark:text-secondary-400',
      title: 'text-secondary-900 dark:text-secondary-100',
      content: 'text-secondary-800 dark:text-secondary-200',
      iconComponent: RocketLaunchIcon,
    },
  }
}

const Callout = forwardRef(({
  variant = 'info',
  title,
  children,
  className,
  icon: CustomIcon,
  animated = true,
  ...props
}, ref) => {
  const variantConfig = calloutVariants.variant[variant]
  const IconComponent = CustomIcon || variantConfig.iconComponent

  return (
    <div
      ref={ref}
      className={cn(
        'rounded-xl border-l-4 p-6 my-6',
        'transition-all duration-200',
        variantConfig.container,
        {
          'animate-slide-up': animated,
          'hover:shadow-soft hover:-translate-y-0.5': animated,
        },
        className
      )}
      {...props}
    >
      <div className="flex items-start gap-4">
        <div className="flex-shrink-0">
          <IconComponent className={cn('w-6 h-6', variantConfig.icon)} />
        </div>
        
        <div className="flex-1 min-w-0">
          {title && (
            <h4 className={cn(
              'text-lg font-semibold mb-2',
              variantConfig.title
            )}>
              {title}
            </h4>
          )}
          
          <div className={cn(
            'prose prose-sm max-w-none',
            variantConfig.content,
            // Override prose styles to match callout colors
            variant === 'code' && 'prose-invert',
          )}>
            {children}
          </div>
        </div>
      </div>
    </div>
  )
})

Callout.displayName = 'Callout'

// Convenience components for different variants
export const InfoCallout = (props) => <Callout variant="info" {...props} />
export const WarningCallout = (props) => <Callout variant="warning" {...props} />
export const SuccessCallout = (props) => <Callout variant="success" {...props} />
export const ErrorCallout = (props) => <Callout variant="error" {...props} />
export const TipCallout = (props) => <Callout variant="tip" {...props} />
export const NoteCallout = (props) => <Callout variant="note" {...props} />
export const CodeCallout = (props) => <Callout variant="code" {...props} />
export const ExampleCallout = (props) => <Callout variant="example" {...props} />

// Steps component for tutorials
export const Steps = forwardRef(({
  children,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'space-y-6 my-8',
        '[counter-reset:step]',
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

Steps.displayName = 'Steps'

export const Step = forwardRef(({
  title,
  children,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'relative pl-12',
        '[counter-increment:step]',
        'before:absolute before:left-0 before:top-0',
        'before:flex before:items-center before:justify-center',
        'before:w-8 before:h-8 before:rounded-full',
        'before:bg-primary-600 before:text-white before:text-sm before:font-semibold',
        'before:content-[counter(step)]',
        'before:shadow-soft',
        className
      )}
      {...props}
    >
      {title && (
        <h4 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">
          {title}
        </h4>
      )}
      
      <div className="prose prose-gray max-w-none dark:prose-invert">
        {children}
      </div>
    </div>
  )
})

Step.displayName = 'Step'

// Tabs component for code examples
export const Tabs = forwardRef(({
  children,
  defaultValue,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn('my-6', className)}
      {...props}
    >
      {children}
    </div>
  )
})

Tabs.displayName = 'Tabs'

export const TabsList = forwardRef(({
  children,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'flex space-x-1 rounded-lg bg-gray-100 dark:bg-gray-800 p-1',
        'border border-gray-200 dark:border-gray-700',
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

TabsList.displayName = 'TabsList'

export const TabsTrigger = forwardRef(({
  children,
  active = false,
  className,
  ...props
}, ref) => {
  return (
    <button
      ref={ref}
      className={cn(
        'px-3 py-2 text-sm font-medium rounded-md transition-all duration-200',
        'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
        {
          'bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 shadow-sm': active,
          'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-100': !active,
        },
        className
      )}
      {...props}
    >
      {children}
    </button>
  )
})

TabsTrigger.displayName = 'TabsTrigger'

export const TabsContent = forwardRef(({
  children,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn('mt-4', className)}
      {...props}
    >
      {children}
    </div>
  )
})

TabsContent.displayName = 'TabsContent'

// Feature grid for showcasing features
export const FeatureGrid = forwardRef(({
  children,
  columns = 2,
  className,
  ...props
}, ref) => {
  const gridCols = {
    1: 'grid-cols-1',
    2: 'grid-cols-1 md:grid-cols-2',
    3: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3',
    4: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-4',
  }

  return (
    <div
      ref={ref}
      className={cn(
        'grid gap-6 my-8',
        gridCols[columns],
        className
      )}
      {...props}
    >
      {children}
    </div>
  )
})

FeatureGrid.displayName = 'FeatureGrid'

export const FeatureCard = forwardRef(({
  title,
  description,
  children,
  className,
  ...props
}, ref) => {
  return (
    <div
      ref={ref}
      className={cn(
        'p-6 rounded-lg border border-gray-200 dark:border-gray-700',
        'bg-white dark:bg-gray-800',
        'hover:shadow-lg transition-shadow duration-200',
        'group',
        className
      )}
      {...props}
    >
      {title && (
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-2 group-hover:text-primary-600 dark:group-hover:text-primary-400 transition-colors">
          {title}
        </h3>
      )}

      {description && (
        <p className="text-gray-600 dark:text-gray-400 mb-4">
          {description}
        </p>
      )}

      {children && (
        <div className="prose prose-gray max-w-none dark:prose-invert">
          {children}
        </div>
      )}
    </div>
  )
})

FeatureCard.displayName = 'FeatureCard'

export { Callout, calloutVariants }
