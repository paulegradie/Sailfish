import { useState, useEffect } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/router'
import clsx from 'clsx'
import { ChevronDownIcon } from '@heroicons/react/24/outline'

// Main navigation items for the header
const navigationItems = [
  {
    name: 'Home',
    href: '/',
    description: 'Welcome to Sailfish'
  },
  {
    name: 'Documentation',
    href: '/docs',
    description: 'Complete guides and API reference',
    children: [
      { name: 'Getting Started', href: '/docs/0/getting-started', description: 'Quick start guide' },
      { name: 'Installation', href: '/docs/0/installation', description: 'Setup instructions' },
      { name: 'API Reference', href: '/docs/2/sailfish', description: 'Complete API documentation' },
      { name: 'Examples', href: '/docs/3/example-app', description: 'Sample applications' },
    ]
  },
  {
    name: 'Pricing',
    href: '/pricing',
    description: 'Simple, transparent pricing'
  },
  {
    name: 'Enterprise',
    href: '/enterprise',
    description: 'Solutions for large organizations',
    children: [
      { name: 'Enterprise Features', href: '/enterprise', description: 'Advanced capabilities for teams' },
      { name: 'Case Studies', href: '/case-studies', description: 'Customer success stories' },
      { name: 'Pricing', href: '/pricing', description: 'Enterprise pricing and plans' },
      { name: 'Contact Sales', href: '/enterprise/contact', description: 'Get in touch with our team' },
    ]
  },
  {
    name: 'Community',
    href: '/community',
    description: 'Join the Sailfish community',
    children: [
      { name: 'GitHub', href: 'https://github.com/paulegradie/Sailfish', description: 'Source code and issues', external: true },
      { name: 'Discussions', href: 'https://github.com/paulegradie/Sailfish/discussions', description: 'Community discussions', external: true },
      { name: 'Contributing', href: '/community/contributing', description: 'How to contribute' },
    ]
  }
]

// Dropdown menu component
function DropdownMenu({ item, isOpen, onToggle }) {
  const router = useRouter()

  if (!item.children) return null

  return (
    <div className="relative">
      <button
        onClick={onToggle}
        className={clsx(
          'flex items-center gap-1 px-3 py-2 text-sm font-medium transition-colors duration-200',
          'hover:text-primary-600 dark:hover:text-primary-400',
          router.pathname.startsWith(item.href)
            ? 'text-primary-600 dark:text-primary-400'
            : 'text-slate-700 dark:text-slate-300'
        )}
        aria-expanded={isOpen}
      >
        {item.name}
        <ChevronDownIcon 
          className={clsx(
            'h-4 w-4 transition-transform duration-200',
            isOpen && 'rotate-180'
          )} 
        />
      </button>

      {isOpen && (
        <div className="absolute top-full left-0 z-50 mt-2 w-80 rounded-xl bg-white p-4 shadow-large ring-1 ring-black/5 dark:bg-slate-800 dark:ring-white/10">
          <div className="space-y-1">
            {item.children.map((child) => (
              <Link
                key={child.href}
                href={child.href}
                className={clsx(
                  'block rounded-lg p-3 transition-colors duration-200',
                  'hover:bg-slate-50 dark:hover:bg-slate-700/50',
                  router.pathname === child.href && 'bg-primary-50 dark:bg-primary-900/20'
                )}
                {...(child.external && { target: '_blank', rel: 'noopener noreferrer' })}
              >
                <div className="flex items-center justify-between">
                  <div className="font-medium text-slate-900 dark:text-white">
                    {child.name}
                  </div>
                  {child.external && (
                    <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
                    </svg>
                  )}
                </div>
                <div className="mt-1 text-sm text-slate-500 dark:text-slate-400">
                  {child.description}
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

// Main navigation component
export function MainNavigation({ className }) {
  const router = useRouter()
  const [openDropdown, setOpenDropdown] = useState(null)

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside() {
      setOpenDropdown(null)
    }

    if (openDropdown) {
      document.addEventListener('click', handleClickOutside)
      return () => document.removeEventListener('click', handleClickOutside)
    }
  }, [openDropdown])

  // Close dropdown on route change
  useEffect(() => {
    setOpenDropdown(null)
  }, [router.pathname])

  return (
    <nav className={clsx('hidden lg:flex lg:items-center lg:space-x-1', className)}>
      {navigationItems.map((item) => {
        const hasChildren = item.children && item.children.length > 0
        const isActive = router.pathname === item.href || 
          (item.href !== '/' && router.pathname.startsWith(item.href))

        if (hasChildren) {
          return (
            <DropdownMenu
              key={item.name}
              item={item}
              isOpen={openDropdown === item.name}
              onToggle={(e) => {
                e.stopPropagation()
                setOpenDropdown(openDropdown === item.name ? null : item.name)
              }}
            />
          )
        }

        return (
          <Link
            key={item.name}
            href={item.href}
            className={clsx(
              'px-3 py-2 text-sm font-medium transition-colors duration-200 rounded-lg',
              'hover:text-primary-600 dark:hover:text-primary-400',
              'hover:bg-slate-50 dark:hover:bg-slate-800/50',
              isActive
                ? 'text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/20'
                : 'text-slate-700 dark:text-slate-300'
            )}
          >
            {item.name}
          </Link>
        )
      })}
    </nav>
  )
}

// Export navigation items for use in mobile navigation
export { navigationItems }
