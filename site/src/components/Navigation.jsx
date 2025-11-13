import Link from 'next/link'
import { useRouter } from 'next/router'
import clsx from 'clsx'

export function Navigation({ navigation, className }) {
  let router = useRouter()

  return (
    <nav className={clsx('text-base lg:text-sm', className)}>
      <ul role="list" className="space-y-9">
        {navigation.map((section) => (
          <li key={section.title}>
            <h2 className="font-display text-xs font-semibold uppercase tracking-wide text-slate-700 dark:text-slate-300">
              {section.title}
            </h2>
            <ul
              role="list"
              className="mt-3 space-y-1.5 border-l border-slate-200 dark:border-slate-800 lg:mt-3 lg:space-y-2"
            >
              {section.links.map((link) => (
                <li key={link.href} className="relative">
                  <Link
                    href={link.href}
                    className={clsx(
                      'block w-full pl-3.5 py-1 rounded-md before:pointer-events-none before:absolute before:-left-1 before:top-1/2 before:h-1.5 before:w-1.5 before:-translate-y-1/2 before:rounded-full',
                      link.href === router.pathname
                        ? 'font-semibold text-primary-500 before:bg-primary-500'
                        : 'text-slate-500 before:hidden before:bg-slate-300 hover:text-slate-600 hover:before:block dark:text-slate-400 dark:before:bg-slate-700 dark:hover:text-slate-300'
                    )}
                  >
                    <span className="inline-flex items-center">
                      <span>{link.title}</span>
                      {link.badge && (
                        <span className="ml-2 rounded border border-primary-500 px-1.5 py-0.5 text-[10px] font-semibold text-primary-500">
                          {link.badge}
                        </span>
                      )}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </li>
        ))}
      </ul>
    </nav>
  )
}
