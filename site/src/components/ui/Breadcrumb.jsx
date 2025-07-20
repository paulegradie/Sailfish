import Link from 'next/link'
import { useRouter } from 'next/router'
import clsx from 'clsx'
import { ChevronRightIcon, HomeIcon } from '@heroicons/react/24/outline'

// Generate breadcrumb items from current route
function generateBreadcrumbs(pathname) {
  const segments = pathname.split('/').filter(Boolean)
  const breadcrumbs = [
    { name: 'Home', href: '/', icon: HomeIcon }
  ]

  let currentPath = ''
  
  segments.forEach((segment, index) => {
    currentPath += `/${segment}`
    
    // Convert segment to readable name
    let name = segment
      .split('-')
      .map(word => word.charAt(0).toUpperCase() + word.slice(1))
      .join(' ')

    // Special cases for common segments
    const specialCases = {
      'docs': 'Documentation',
      'api': 'API Reference',
      'enterprise': 'Enterprise',
      'community': 'Community',
      'pricing': 'Pricing'
    }

    if (specialCases[segment.toLowerCase()]) {
      name = specialCases[segment.toLowerCase()]
    }

    // Skip numeric segments (like version numbers)
    if (!/^\d+$/.test(segment)) {
      breadcrumbs.push({
        name,
        href: currentPath,
        isLast: index === segments.length - 1
      })
    }
  })

  return breadcrumbs
}

// Individual breadcrumb item component
function BreadcrumbItem({ item, isLast }) {
  const content = (
    <div className="flex items-center">
      {item.icon && (
        <item.icon className="h-4 w-4 mr-2 text-slate-400 dark:text-slate-500" />
      )}
      <span 
        className={clsx(
          'text-sm font-medium transition-colors duration-200',
          isLast 
            ? 'text-slate-900 dark:text-white' 
            : 'text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-300'
        )}
      >
        {item.name}
      </span>
    </div>
  )

  if (isLast) {
    return (
      <li className="flex items-center">
        {content}
      </li>
    )
  }

  return (
    <li className="flex items-center">
      <Link 
        href={item.href}
        className="flex items-center hover:bg-slate-50 dark:hover:bg-slate-800/50 rounded-md px-2 py-1 -mx-2 -my-1 transition-colors duration-200"
      >
        {content}
      </Link>
      <ChevronRightIcon className="h-4 w-4 mx-2 text-slate-400 dark:text-slate-500 flex-shrink-0" />
    </li>
  )
}

// Main breadcrumb navigation component
export function Breadcrumb({ className, showHome = true }) {
  const router = useRouter()
  const breadcrumbs = generateBreadcrumbs(router.pathname)

  // Don't show breadcrumbs on home page
  if (router.pathname === '/') {
    return null
  }

  // Filter out home if showHome is false
  const displayBreadcrumbs = showHome ? breadcrumbs : breadcrumbs.slice(1)

  // Don't render if only one item (would just be current page)
  if (displayBreadcrumbs.length <= 1) {
    return null
  }

  return (
    <nav 
      aria-label="Breadcrumb" 
      className={clsx(
        'flex items-center space-x-1 text-sm',
        className
      )}
    >
      <ol className="flex items-center space-x-1">
        {displayBreadcrumbs.map((item, index) => (
          <BreadcrumbItem
            key={item.href}
            item={item}
            isLast={index === displayBreadcrumbs.length - 1}
          />
        ))}
      </ol>
    </nav>
  )
}

// Compact breadcrumb for mobile
export function CompactBreadcrumb({ className }) {
  const router = useRouter()
  const breadcrumbs = generateBreadcrumbs(router.pathname)

  if (router.pathname === '/' || breadcrumbs.length <= 1) {
    return null
  }

  const currentPage = breadcrumbs[breadcrumbs.length - 1]
  const parentPage = breadcrumbs[breadcrumbs.length - 2]

  return (
    <nav 
      aria-label="Breadcrumb" 
      className={clsx('flex items-center text-sm', className)}
    >
      {parentPage && (
        <>
          <Link 
            href={parentPage.href}
            className="text-slate-500 hover:text-slate-700 dark:text-slate-400 dark:hover:text-slate-300 transition-colors duration-200"
          >
            {parentPage.name}
          </Link>
          <ChevronRightIcon className="h-4 w-4 mx-2 text-slate-400 dark:text-slate-500" />
        </>
      )}
      <span className="text-slate-900 dark:text-white font-medium">
        {currentPage.name}
      </span>
    </nav>
  )
}
