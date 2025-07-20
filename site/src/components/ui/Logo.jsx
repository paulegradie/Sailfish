import clsx from 'clsx'

// Sailfish Logo Component - Modern, scalable SVG logo
function SailfishIcon({ className, ...props }) {
  return (
    <svg
      viewBox="0 0 40 40"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      className={className}
      {...props}
    >
      {/* Sailfish silhouette - streamlined and modern */}
      <path
        d="M8 20c0-6.627 5.373-12 12-12s12 5.373 12 12-5.373 12-12 12S8 26.627 8 20z"
        fill="currentColor"
        className="text-primary-500"
      />
      {/* Sail fin - distinctive feature */}
      <path
        d="M20 8l8 4-8 8V8z"
        fill="currentColor"
        className="text-primary-600"
      />
      {/* Speed lines - representing performance */}
      <path
        d="M4 18h4M4 20h6M4 22h4"
        stroke="currentColor"
        strokeWidth="2"
        strokeLinecap="round"
        className="text-primary-400"
      />
    </svg>
  )
}

// Logo component with text
export function Logo({ 
  className, 
  showText = true, 
  size = 'md',
  variant = 'default',
  ...props 
}) {
  const sizeClasses = {
    sm: 'h-6 w-6',
    md: 'h-8 w-8',
    lg: 'h-10 w-10',
    xl: 'h-12 w-12',
  }

  const textSizeClasses = {
    sm: 'text-lg',
    md: 'text-xl',
    lg: 'text-2xl',
    xl: 'text-3xl',
  }

  const variantClasses = {
    default: 'text-slate-900 dark:text-white',
    light: 'text-white',
    dark: 'text-slate-900',
    primary: 'text-primary-600 dark:text-primary-400',
  }

  return (
    <div 
      className={clsx(
        'flex items-center gap-3',
        variantClasses[variant],
        className
      )}
      {...props}
    >
      <SailfishIcon className={sizeClasses[size]} />
      {showText && (
        <span 
          className={clsx(
            'font-display font-bold tracking-tight',
            textSizeClasses[size]
          )}
        >
          Sailfish
        </span>
      )}
    </div>
  )
}

// Icon-only version for compact spaces
export function LogoIcon({ className, size = 'md', ...props }) {
  const sizeClasses = {
    sm: 'h-6 w-6',
    md: 'h-8 w-8',
    lg: 'h-10 w-10',
    xl: 'h-12 w-12',
  }

  return (
    <SailfishIcon 
      className={clsx(sizeClasses[size], className)} 
      {...props} 
    />
  )
}

// Logo mark for use in headers, favicons, etc.
export function LogoMark({ className, ...props }) {
  return (
    <div 
      className={clsx(
        'flex items-center justify-center rounded-lg bg-primary-500 p-2',
        className
      )}
      {...props}
    >
      <SailfishIcon className="h-6 w-6 text-white" />
    </div>
  )
}
