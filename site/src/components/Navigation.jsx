import Link from 'next/link'
import { useRouter } from 'next/router'
import clsx from 'clsx'

export function Navigation({ navigation, className }) {
  let router = useRouter()

  return (
    <nav className={clsx('text-base lg:text-sm', className)}>
      <ul role="list" className="space-y-8">
        {navigation.map((section) => (
          <li key={section.title}>
            <h2 className="font-display font-semibold text-slate-900 dark:text-white mb-4">
              {section.title}
            </h2>
            <ul
              role="list"
              className="space-y-1 border-l-2 border-slate-100 dark:border-slate-800 pl-4"
            >
              {section.links.map((link) => (
                <li key={link.href} className="relative">
                  <Link
                    href={link.href}
                    className={clsx(
                      'block w-full py-2 px-3 rounded-lg transition-all duration-200',
                      'relative before:absolute before:-left-7 before:top-1/2 before:h-2 before:w-2 before:-translate-y-1/2 before:rounded-full before:transition-all before:duration-200',
                      link.href === router.pathname
                        ? 'font-semibold text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/20 before:bg-primary-500 before:scale-100'
                        : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200 hover:bg-slate-50 dark:hover:bg-slate-800/50 before:bg-slate-300 dark:before:bg-slate-600 before:scale-0 hover:before:scale-100'
                    )}
                  >
                    {link.title}
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
