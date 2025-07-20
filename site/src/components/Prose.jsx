import clsx from 'clsx'

export function Prose({ as: Component = 'div', className, animated = true, ...props }) {
  return (
    <Component
      className={clsx(
        className,
        // Base prose styles with enhanced readability
        'prose prose-slate max-w-none dark:prose-invert',
        'prose-lg leading-relaxed',

        // Animation support
        {
          'animate-fade-in': animated,
        },

        // Enhanced typography with design system integration
        'prose-headings:scroll-mt-28 prose-headings:font-display prose-headings:font-semibold prose-headings:tracking-tight lg:prose-headings:scroll-mt-[8.5rem]',
        'prose-headings:transition-colors prose-headings:duration-200',
        'prose-h1:text-4xl lg:prose-h1:text-5xl prose-h1:mb-8 prose-h1:text-gray-900 dark:prose-h1:text-white',
        'prose-h1:leading-tight prose-h1:text-balance',
        'prose-h2:text-2xl lg:prose-h2:text-3xl prose-h2:mt-12 prose-h2:mb-6 prose-h2:text-gray-900 dark:prose-h2:text-white',
        'prose-h2:border-b prose-h2:border-gray-200 dark:prose-h2:border-gray-700 prose-h2:pb-3',
        'prose-h3:text-xl lg:prose-h3:text-2xl prose-h3:mt-8 prose-h3:mb-4 prose-h3:text-gray-800 dark:prose-h3:text-gray-200',
        'prose-h4:text-lg lg:prose-h4:text-xl prose-h4:mt-6 prose-h4:mb-3 prose-h4:text-gray-700 dark:prose-h4:text-gray-300',

        // Enhanced body text with better readability
        'prose-p:text-gray-700 dark:prose-p:text-gray-300 prose-p:leading-relaxed prose-p:text-pretty',
        'prose-p:mb-6 prose-p:text-base lg:prose-p:text-lg',
        'prose-lead:text-xl lg:prose-lead:text-2xl prose-lead:text-gray-600 dark:prose-lead:text-gray-400 prose-lead:leading-relaxed',
        'prose-lead:mb-8 prose-lead:text-balance',

        // Enhanced links with design system colors and hover effects
        'prose-a:font-medium prose-a:text-primary-600 dark:prose-a:text-primary-400',
        'prose-a:no-underline prose-a:decoration-primary-300 prose-a:decoration-2 prose-a:underline-offset-4',
        'hover:prose-a:underline hover:prose-a:decoration-primary-500 dark:hover:prose-a:decoration-primary-300',
        'prose-a:transition-all prose-a:duration-200 hover:prose-a:text-primary-700 dark:hover:prose-a:text-primary-300',

        // Enhanced lists
        'prose-ul:my-6 prose-ol:my-6',
        'prose-li:text-slate-700 dark:prose-li:text-slate-300 prose-li:leading-relaxed',
        'prose-li:my-2',

        // Enhanced blockquotes
        'prose-blockquote:border-l-4 prose-blockquote:border-primary-200 dark:prose-blockquote:border-primary-800',
        'prose-blockquote:bg-primary-50 dark:prose-blockquote:bg-primary-900/10',
        'prose-blockquote:py-4 prose-blockquote:px-6 prose-blockquote:rounded-r-lg',
        'prose-blockquote:text-slate-700 dark:prose-blockquote:text-slate-300',
        'prose-blockquote:not-italic',

        // Enhanced inline code styles
        'prose-code:text-sm prose-code:font-medium prose-code:font-mono',
        'prose-code:bg-gray-100 dark:prose-code:bg-gray-800',
        'prose-code:text-primary-700 dark:prose-code:text-primary-300',
        'prose-code:px-2 prose-code:py-1 prose-code:rounded-md',
        'prose-code:before:content-none prose-code:after:content-none',
        'prose-code:border prose-code:border-gray-200 dark:prose-code:border-gray-700',

        // Enhanced pre/code blocks with design system
        'prose-pre:rounded-xl prose-pre:bg-gray-900 prose-pre:shadow-large prose-pre:ring-1 prose-pre:ring-gray-300/10',
        'dark:prose-pre:bg-gray-800/90 dark:prose-pre:shadow-none dark:prose-pre:ring-gray-300/10',
        'prose-pre:border prose-pre:border-gray-200 dark:prose-pre:border-gray-700',
        'prose-pre:overflow-x-auto prose-pre:scrollbar-thin',

        // Enhanced tables
        'prose-table:my-8',
        'prose-thead:border-b prose-thead:border-slate-200 dark:prose-thead:border-slate-700',
        'prose-th:text-left prose-th:font-semibold prose-th:text-slate-900 dark:prose-th:text-white',
        'prose-th:py-3 prose-th:px-4',
        'prose-td:py-3 prose-td:px-4 prose-td:border-b prose-td:border-slate-100 dark:prose-td:border-slate-800',
        'prose-td:text-slate-700 dark:prose-td:text-slate-300',

        // Enhanced horizontal rules
        'prose-hr:border-slate-200 dark:prose-hr:border-slate-700 prose-hr:my-12',

        // Enhanced images
        'prose-img:rounded-lg prose-img:shadow-lg',

        // Enhanced strong/bold text
        'prose-strong:text-slate-900 dark:prose-strong:text-white prose-strong:font-semibold',

        // Enhanced emphasis/italic text
        'prose-em:text-slate-700 dark:prose-em:text-slate-300'
      )}
      {...props}
    />
  )
}
