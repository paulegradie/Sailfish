import { useState, useEffect } from 'react'
import clsx from 'clsx'

// Reading progress bar component
export function ReadingProgress({ className, target = 'article' }) {
  const [progress, setProgress] = useState(0)

  useEffect(() => {
    const updateProgress = () => {
      const article = document.querySelector(target)
      if (!article) return

      const articleTop = article.offsetTop
      const articleHeight = article.offsetHeight
      const windowHeight = window.innerHeight
      const scrollTop = window.scrollY

      // Calculate how much of the article has been scrolled through
      const articleBottom = articleTop + articleHeight
      const windowBottom = scrollTop + windowHeight

      if (scrollTop < articleTop) {
        // Haven't reached the article yet
        setProgress(0)
      } else if (windowBottom > articleBottom) {
        // Scrolled past the article
        setProgress(100)
      } else {
        // Currently reading the article
        const scrolledIntoArticle = scrollTop - articleTop
        const readableHeight = articleHeight - windowHeight
        const progressPercent = readableHeight > 0 
          ? Math.min(100, Math.max(0, (scrolledIntoArticle / readableHeight) * 100))
          : 100
        setProgress(progressPercent)
      }
    }

    // Update on scroll
    window.addEventListener('scroll', updateProgress, { passive: true })
    // Update on resize
    window.addEventListener('resize', updateProgress, { passive: true })
    // Initial update
    updateProgress()

    return () => {
      window.removeEventListener('scroll', updateProgress)
      window.removeEventListener('resize', updateProgress)
    }
  }, [target])

  return (
    <div className={clsx(
      'fixed top-0 left-0 right-0 z-50 h-1 bg-gray-200/50 dark:bg-gray-800/50',
      className
    )}>
      <div
        className="h-full gradient-primary transition-all duration-150 ease-out shadow-sm"
        style={{ width: `${progress}%` }}
      />
    </div>
  )
}

// Estimated reading time component
export function ReadingTime({ content, className }) {
  const wordsPerMinute = 200 // Average reading speed
  
  const calculateReadingTime = (text) => {
    if (!text) return 0
    const words = text.trim().split(/\s+/).length
    return Math.ceil(words / wordsPerMinute)
  }

  const readingTime = calculateReadingTime(content)

  if (readingTime === 0) return null

  return (
    <div className={clsx(
      'flex items-center gap-2 text-sm text-slate-500 dark:text-slate-400',
      className
    )}>
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
      <span>
        {readingTime} min read
      </span>
    </div>
  )
}

// Table of contents with progress tracking
export function TableOfContentsWithProgress({ tableOfContents, currentSection, className }) {
  const [visibleSections, setVisibleSections] = useState(new Set())

  useEffect(() => {
    const observerOptions = {
      rootMargin: '-20% 0px -35% 0px',
      threshold: 0
    }

    const observer = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (entry.isIntersecting) {
          setVisibleSections(prev => new Set([...prev, entry.target.id]))
        } else {
          setVisibleSections(prev => {
            const newSet = new Set(prev)
            newSet.delete(entry.target.id)
            return newSet
          })
        }
      })
    }, observerOptions)

    // Observe all headings
    tableOfContents.forEach((section) => {
      const element = document.getElementById(section.id)
      if (element) observer.observe(element)
      
      section.children?.forEach((child) => {
        const childElement = document.getElementById(child.id)
        if (childElement) observer.observe(childElement)
      })
    })

    return () => observer.disconnect()
  }, [tableOfContents])

  const isActive = (section) => {
    return section.id === currentSection || visibleSections.has(section.id)
  }

  const isChildActive = (section) => {
    return section.children?.some(child => visibleSections.has(child.id))
  }

  return (
    <nav className={clsx('space-y-2', className)}>
      {tableOfContents.map((section) => (
        <div key={section.id}>
          <a
            href={`#${section.id}`}
            className={clsx(
              'block py-1 text-sm transition-colors duration-200',
              isActive(section) || isChildActive(section)
                ? 'text-primary-600 dark:text-primary-400 font-medium'
                : 'text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-slate-200'
            )}
          >
            <div className="flex items-center gap-2">
              <div className={clsx(
                'h-1.5 w-1.5 rounded-full transition-colors duration-200',
                isActive(section) || isChildActive(section)
                  ? 'bg-primary-500'
                  : 'bg-slate-300 dark:bg-slate-600'
              )} />
              {section.title}
            </div>
          </a>
          
          {section.children && section.children.length > 0 && (
            <div className="ml-4 mt-1 space-y-1">
              {section.children.map((child) => (
                <a
                  key={child.id}
                  href={`#${child.id}`}
                  className={clsx(
                    'block py-0.5 text-xs transition-colors duration-200',
                    visibleSections.has(child.id)
                      ? 'text-primary-600 dark:text-primary-400 font-medium'
                      : 'text-slate-500 dark:text-slate-500 hover:text-slate-700 dark:hover:text-slate-300'
                  )}
                >
                  <div className="flex items-center gap-2">
                    <div className={clsx(
                      'h-1 w-1 rounded-full transition-colors duration-200',
                      visibleSections.has(child.id)
                        ? 'bg-primary-500'
                        : 'bg-slate-300 dark:bg-slate-600'
                    )} />
                    {child.title}
                  </div>
                </a>
              ))}
            </div>
          )}
        </div>
      ))}
    </nav>
  )
}

// Page completion indicator
export function PageCompletion({ className }) {
  const [completion, setCompletion] = useState(0)

  useEffect(() => {
    const updateCompletion = () => {
      const scrollTop = window.scrollY
      const docHeight = document.documentElement.scrollHeight - window.innerHeight
      const scrollPercent = docHeight > 0 ? (scrollTop / docHeight) * 100 : 0
      setCompletion(Math.min(100, Math.max(0, scrollPercent)))
    }

    window.addEventListener('scroll', updateCompletion, { passive: true })
    updateCompletion()

    return () => window.removeEventListener('scroll', updateCompletion)
  }, [])

  return (
    <div className={clsx(
      'flex items-center gap-2 text-xs text-slate-500 dark:text-slate-400',
      className
    )}>
      <div className="flex items-center gap-1">
        <div className="h-2 w-8 bg-slate-200 dark:bg-slate-700 rounded-full overflow-hidden">
          <div 
            className="h-full bg-primary-500 transition-all duration-150"
            style={{ width: `${completion}%` }}
          />
        </div>
        <span>{Math.round(completion)}%</span>
      </div>
    </div>
  )
}
