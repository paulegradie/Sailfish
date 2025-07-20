import { useEffect } from 'react'
import { useRouter } from 'next/router'

// Simple analytics tracking for documentation usage
export function DocumentationAnalytics() {
  const router = useRouter()

  useEffect(() => {
    const trackPageView = (url) => {
      // Only track documentation pages
      if (!url.startsWith('/docs')) return

      // Track with Google Analytics if available
      if (typeof window !== 'undefined' && window.gtag) {
        window.gtag('config', 'G-GVGQHPJLB3', {
          page_title: document.title,
          page_location: window.location.href,
          custom_map: {
            custom_parameter_1: 'documentation_section'
          }
        })

        // Track custom events for documentation usage
        window.gtag('event', 'page_view', {
          event_category: 'Documentation',
          event_label: url,
          custom_parameter_1: getDocumentationSection(url)
        })
      }

      // Track reading time and engagement
      trackReadingEngagement(url)
    }

    const handleRouteChange = (url) => {
      trackPageView(url)
    }

    // Track initial page load
    trackPageView(router.pathname)

    // Track route changes
    router.events.on('routeChangeComplete', handleRouteChange)

    return () => {
      router.events.off('routeChangeComplete', handleRouteChange)
    }
  }, [router])

  return null // This component doesn't render anything
}

// Track reading engagement metrics
function trackReadingEngagement(url) {
  if (typeof window === 'undefined') return

  let startTime = Date.now()
  let maxScroll = 0
  let isActive = true

  const trackScroll = () => {
    const scrollPercent = Math.round(
      (window.scrollY / (document.documentElement.scrollHeight - window.innerHeight)) * 100
    )
    maxScroll = Math.max(maxScroll, scrollPercent)
  }

  const trackVisibility = () => {
    isActive = !document.hidden
    if (!isActive) {
      // Track time spent when tab becomes inactive
      const timeSpent = Math.round((Date.now() - startTime) / 1000)
      if (timeSpent > 10 && window.gtag) { // Only track if spent more than 10 seconds
        window.gtag('event', 'engagement', {
          event_category: 'Documentation',
          event_label: url,
          value: timeSpent,
          custom_parameter_1: 'time_spent_seconds'
        })
      }
    } else {
      startTime = Date.now() // Reset start time when tab becomes active again
    }
  }

  const trackExit = () => {
    const timeSpent = Math.round((Date.now() - startTime) / 1000)
    
    if (window.gtag && timeSpent > 5) {
      // Track reading completion
      window.gtag('event', 'reading_completion', {
        event_category: 'Documentation',
        event_label: url,
        value: maxScroll,
        custom_parameter_1: 'max_scroll_percent'
      })

      // Track time spent
      window.gtag('event', 'time_on_page', {
        event_category: 'Documentation',
        event_label: url,
        value: timeSpent,
        custom_parameter_1: 'seconds'
      })
    }
  }

  // Add event listeners
  window.addEventListener('scroll', trackScroll, { passive: true })
  document.addEventListener('visibilitychange', trackVisibility)
  window.addEventListener('beforeunload', trackExit)

  // Cleanup function
  return () => {
    window.removeEventListener('scroll', trackScroll)
    document.removeEventListener('visibilitychange', trackVisibility)
    window.removeEventListener('beforeunload', trackExit)
  }
}

// Get documentation section from URL
function getDocumentationSection(url) {
  const sections = {
    '/docs/0/': 'Introduction',
    '/docs/1/': 'Sailfish Basics',
    '/docs/2/': 'Features',
    '/docs/3/': 'Advanced',
    '/docs/4/': 'Project'
  }

  for (const [path, section] of Object.entries(sections)) {
    if (url.startsWith(path)) {
      return section
    }
  }

  return 'Unknown'
}

// Search analytics tracking
export function trackSearchEvent(query, results, selectedResult = null) {
  if (typeof window === 'undefined' || !window.gtag) return

  window.gtag('event', 'search', {
    event_category: 'Documentation',
    search_term: query,
    custom_parameter_1: results.length,
    custom_parameter_2: selectedResult ? 'result_clicked' : 'search_performed'
  })

  if (selectedResult) {
    window.gtag('event', 'search_result_click', {
      event_category: 'Documentation',
      event_label: selectedResult.url,
      search_term: query,
      custom_parameter_1: selectedResult.section
    })
  }
}

// Copy code analytics tracking
export function trackCodeCopy(language, codeLength) {
  if (typeof window === 'undefined' || !window.gtag) return

  window.gtag('event', 'code_copy', {
    event_category: 'Documentation',
    event_label: language,
    value: codeLength,
    custom_parameter_1: 'characters'
  })
}

// Link click analytics tracking
export function trackLinkClick(linkUrl, linkText, context = 'documentation') {
  if (typeof window === 'undefined' || !window.gtag) return

  const isExternal = linkUrl.startsWith('http') && !linkUrl.includes(window.location.hostname)

  window.gtag('event', 'link_click', {
    event_category: context,
    event_label: linkUrl,
    custom_parameter_1: isExternal ? 'external' : 'internal',
    custom_parameter_2: linkText
  })
}

// Popular content tracking
export function getPopularContent() {
  // This would typically fetch from your analytics API
  // For now, return static data that could be populated from GA4 API
  return {
    popularPages: [
      { url: '/docs/0/getting-started', title: 'Getting Started', views: 1250 },
      { url: '/docs/0/installation', title: 'Installation', views: 980 },
      { url: '/docs/2/sailfish', title: 'Sailfish Core', views: 750 },
      { url: '/docs/1/sailfish-variables', title: 'Variables', views: 620 },
      { url: '/docs/2/saildiff', title: 'SailDiff', views: 580 }
    ],
    popularSearches: [
      { query: 'getting started', count: 340 },
      { query: 'installation', count: 280 },
      { query: 'variables', count: 190 },
      { query: 'regression detection', count: 150 },
      { query: 'machine learning', count: 120 }
    ],
    engagementMetrics: {
      averageTimeOnPage: 185, // seconds
      averageScrollDepth: 68, // percent
      bounceRate: 0.32 // 32%
    }
  }
}

// Component to display popular content
export function PopularContent({ className }) {
  const data = getPopularContent()

  return (
    <div className={className}>
      <h3 className="text-sm font-medium text-slate-900 dark:text-white mb-3">
        Popular Documentation
      </h3>
      <div className="space-y-2">
        {data.popularPages.slice(0, 5).map((page) => (
          <a
            key={page.url}
            href={page.url}
            onClick={() => trackLinkClick(page.url, page.title, 'popular_content')}
            className="block p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors"
          >
            <div className="flex items-center justify-between">
              <span className="text-sm font-medium text-slate-900 dark:text-white">
                {page.title}
              </span>
              <span className="text-xs text-slate-500 dark:text-slate-400">
                {page.views} views
              </span>
            </div>
          </a>
        ))}
      </div>
    </div>
  )
}
