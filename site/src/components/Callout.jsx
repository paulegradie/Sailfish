import clsx from 'clsx'

import { Icon } from '@/components/Icon'

const styles = {
  note: {
    container:
      'bg-primary-50 dark:bg-slate-800/60 dark:ring-1 dark:ring-slate-300/10',
    title: 'text-primary-900 dark:text-primary-400',
    body: 'text-primary-800 [--tw-prose-background:theme(colors.primary.50)] prose-a:text-primary-900 prose-code:text-primary-900 dark:text-slate-300 dark:prose-code:text-slate-300',
  },
  warning: {
    container:
      'bg-amber-50 dark:bg-slate-800/60 dark:ring-1 dark:ring-slate-300/10',
    title: 'text-amber-900 dark:text-amber-500',
    body: 'text-amber-800 [--tw-prose-underline:theme(colors.amber.400)] [--tw-prose-background:theme(colors.amber.50)] prose-a:text-amber-900 prose-code:text-amber-900 dark:text-slate-300 dark:[--tw-prose-underline:theme(colors.primary.600)] dark:prose-code:text-slate-300',
  },
  success: {
    container:
      'bg-primary-50 dark:bg-slate-800/60 dark:ring-1 dark:ring-slate-300/10',
    title: 'text-primary-900 dark:text-primary-400',
    body: 'text-primary-800 [--tw-prose-background:theme(colors.primary.50)] prose-a:text-primary-900 prose-code:text-primary-900 dark:text-slate-300 dark:prose-code:text-slate-300',
  },
}

const icons = {
  note: (props) => <Icon icon="lightbulb" {...props} />,
  warning: (props) => <Icon icon="warning" color="amber" {...props} />,
  success: (props) => <Icon icon="lightbulb" {...props} />,
}

export function Callout({ type = 'note', title, children }) {
  let IconComponent = icons[type] ?? icons.note
  let style = styles[type] ?? styles.note

  return (
    <div className={clsx('my-8 flex rounded-3xl p-6', style.container)}>
      <IconComponent className="h-8 w-8 flex-none" />
      <div className="ml-4 flex-auto">
        <p className={clsx('m-0 font-display text-xl', style.title)}>
          {title}
        </p>
        <div className={clsx('prose mt-2.5', style.body)}>{children}</div>
      </div>
    </div>
  )
}
