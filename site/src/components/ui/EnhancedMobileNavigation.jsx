import { useState, useEffect } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/router'
import { Dialog, Transition } from '@headlessui/react'
import { Fragment } from 'react'
import clsx from 'clsx'
import { 
  Bars3Icon, 
  XMarkIcon, 
  ChevronRightIcon,
  ArrowTopRightOnSquareIcon 
} from '@heroicons/react/24/outline'
import { Logo } from './Logo'
import { navigationItems } from './MainNavigation'

// Mobile navigation menu item component
function MobileNavItem({ item, onClose, level = 0 }) {
  const router = useRouter()
  const [isExpanded, setIsExpanded] = useState(false)
  const hasChildren = item.children && item.children.length > 0
  const isActive = router.pathname === item.href || 
    (item.href !== '/' && router.pathname.startsWith(item.href))

  const handleClick = () => {
    if (hasChildren) {
      setIsExpanded(!isExpanded)
    } else {
      onClose()
    }
  }

  return (
    <div className="space-y-1">
      <div className="flex items-center">
        {hasChildren ? (
          <button
            onClick={handleClick}
            className={clsx(
              'flex w-full items-center justify-between rounded-lg px-3 py-2 text-left transition-colors duration-200',
              'hover:bg-slate-100 dark:hover:bg-slate-800',
              isActive && 'bg-primary-50 text-primary-600 dark:bg-primary-900/20 dark:text-primary-400',
              !isActive && 'text-slate-700 dark:text-slate-300',
              level > 0 && 'ml-4'
            )}
          >
            <span className="font-medium">{item.name}</span>
            <ChevronRightIcon 
              className={clsx(
                'h-4 w-4 transition-transform duration-200',
                isExpanded && 'rotate-90'
              )}
            />
          </button>
        ) : (
          <Link
            href={item.href}
            onClick={handleClick}
            className={clsx(
              'flex w-full items-center justify-between rounded-lg px-3 py-2 transition-colors duration-200',
              'hover:bg-slate-100 dark:hover:bg-slate-800',
              isActive && 'bg-primary-50 text-primary-600 dark:bg-primary-900/20 dark:text-primary-400',
              !isActive && 'text-slate-700 dark:text-slate-300',
              level > 0 && 'ml-4'
            )}
            {...(item.external && { target: '_blank', rel: 'noopener noreferrer' })}
          >
            <span className="font-medium">{item.name}</span>
            {item.external && (
              <ArrowTopRightOnSquareIcon className="h-4 w-4" />
            )}
          </Link>
        )}
      </div>

      {/* Submenu items */}
      {hasChildren && isExpanded && (
        <div className="space-y-1 pl-4">
          {item.children.map((child) => (
            <MobileNavItem
              key={child.href}
              item={child}
              onClose={onClose}
              level={level + 1}
            />
          ))}
        </div>
      )}
    </div>
  )
}

// Enhanced mobile navigation component
export function EnhancedMobileNavigation() {
  const [isOpen, setIsOpen] = useState(false)
  const router = useRouter()

  // Close menu on route change
  useEffect(() => {
    const handleRouteChange = () => setIsOpen(false)
    router.events.on('routeChangeStart', handleRouteChange)
    return () => router.events.off('routeChangeStart', handleRouteChange)
  }, [router])

  return (
    <>
      {/* Mobile menu button */}
      <button
        type="button"
        onClick={() => setIsOpen(true)}
        className="lg:hidden relative p-2 text-slate-500 hover:text-slate-600 dark:text-slate-400 dark:hover:text-slate-300 transition-colors duration-200"
        aria-label="Open navigation menu"
      >
        <Bars3Icon className="h-6 w-6" />
      </button>

      {/* Mobile menu overlay */}
      <Transition show={isOpen} as={Fragment}>
        <Dialog onClose={setIsOpen} className="relative z-50 lg:hidden">
          {/* Backdrop */}
          <Transition.Child
            as={Fragment}
            enter="transition-opacity ease-linear duration-300"
            enterFrom="opacity-0"
            enterTo="opacity-100"
            leave="transition-opacity ease-linear duration-300"
            leaveFrom="opacity-100"
            leaveTo="opacity-0"
          >
            <div className="fixed inset-0 bg-slate-900/50 backdrop-blur-sm" />
          </Transition.Child>

          {/* Menu panel */}
          <div className="fixed inset-0 flex">
            <Transition.Child
              as={Fragment}
              enter="transition ease-in-out duration-300 transform"
              enterFrom="-translate-x-full"
              enterTo="translate-x-0"
              leave="transition ease-in-out duration-300 transform"
              leaveFrom="translate-x-0"
              leaveTo="-translate-x-full"
            >
              <Dialog.Panel className="relative mr-16 flex w-full max-w-xs flex-1 flex-col bg-white dark:bg-slate-900">
                {/* Header */}
                <div className="flex h-16 items-center justify-between px-4 border-b border-slate-200 dark:border-slate-800">
                  <Logo size="sm" />
                  <button
                    type="button"
                    onClick={() => setIsOpen(false)}
                    className="p-2 text-slate-500 hover:text-slate-600 dark:text-slate-400 dark:hover:text-slate-300 transition-colors duration-200"
                    aria-label="Close navigation menu"
                  >
                    <XMarkIcon className="h-6 w-6" />
                  </button>
                </div>

                {/* Navigation items */}
                <div className="flex-1 overflow-y-auto px-4 py-6">
                  <nav className="space-y-2">
                    {navigationItems.map((item) => (
                      <MobileNavItem
                        key={item.name}
                        item={item}
                        onClose={() => setIsOpen(false)}
                      />
                    ))}
                  </nav>
                </div>

                {/* Footer */}
                <div className="border-t border-slate-200 dark:border-slate-800 px-4 py-4">
                  <div className="text-xs text-slate-500 dark:text-slate-400">
                    © 2024 Sailfish. All rights reserved.
                  </div>
                </div>
              </Dialog.Panel>
            </Transition.Child>
          </div>
        </Dialog>
      </Transition>
    </>
  )
}
