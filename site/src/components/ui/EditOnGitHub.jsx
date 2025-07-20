import { PencilIcon } from '@heroicons/react/24/outline'
import clsx from 'clsx'

// Edit on GitHub component for documentation pages
export function EditOnGitHub({ 
  filePath, 
  className,
  variant = 'default',
  size = 'md'
}) {
  // Base GitHub repository URL
  const baseUrl = 'https://github.com/paulegradie/Sailfish'
  
  // Construct the edit URL
  const editUrl = `${baseUrl}/edit/main/site/src/pages/${filePath}`

  const variants = {
    default: 'text-slate-600 hover:text-slate-900 dark:text-slate-400 dark:hover:text-slate-200',
    primary: 'text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300',
    subtle: 'text-slate-500 hover:text-slate-600 dark:text-slate-500 dark:hover:text-slate-400'
  }

  const sizes = {
    sm: 'text-xs gap-1',
    md: 'text-sm gap-1.5',
    lg: 'text-base gap-2'
  }

  const iconSizes = {
    sm: 'h-3 w-3',
    md: 'h-4 w-4',
    lg: 'h-5 w-5'
  }

  return (
    <a
      href={editUrl}
      target="_blank"
      rel="noopener noreferrer"
      className={clsx(
        'inline-flex items-center font-medium transition-colors duration-200',
        variants[variant],
        sizes[size],
        className
      )}
    >
      <PencilIcon className={iconSizes[size]} />
      <span>Edit this page on GitHub</span>
    </a>
  )
}

// Compact version for mobile or tight spaces
export function EditOnGitHubCompact({ filePath, className }) {
  const baseUrl = 'https://github.com/paulegradie/Sailfish'
  const editUrl = `${baseUrl}/edit/main/site/src/pages/${filePath}`

  return (
    <a
      href={editUrl}
      target="_blank"
      rel="noopener noreferrer"
      className={clsx(
        'inline-flex items-center justify-center rounded-md p-2 text-slate-500 transition-colors duration-200',
        'hover:bg-slate-100 hover:text-slate-700 dark:hover:bg-slate-800 dark:hover:text-slate-300',
        className
      )}
      aria-label="Edit this page on GitHub"
    >
      <PencilIcon className="h-4 w-4" />
    </a>
  )
}

// Banner version for prominent placement
export function EditOnGitHubBanner({ filePath, className }) {
  const baseUrl = 'https://github.com/paulegradie/Sailfish'
  const editUrl = `${baseUrl}/edit/main/site/src/pages/${filePath}`

  return (
    <div className={clsx(
      'rounded-lg border border-slate-200 bg-slate-50 p-4 dark:border-slate-700 dark:bg-slate-800',
      className
    )}>
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-sm font-medium text-slate-900 dark:text-white">
            Help improve this page
          </h3>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400">
            Found an error or want to add more information? Edit this page on GitHub.
          </p>
        </div>
        <a
          href={editUrl}
          target="_blank"
          rel="noopener noreferrer"
          className={clsx(
            'inline-flex items-center gap-2 rounded-md bg-white px-3 py-2 text-sm font-medium text-slate-700 shadow-sm ring-1 ring-slate-300 transition-colors',
            'hover:bg-slate-50 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
            'dark:bg-slate-700 dark:text-slate-200 dark:ring-slate-600 dark:hover:bg-slate-600'
          )}
        >
          <PencilIcon className="h-4 w-4" />
          Edit page
        </a>
      </div>
    </div>
  )
}
