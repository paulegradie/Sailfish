import { useState, useRef, useEffect, useMemo } from 'react'
import { useRouter } from 'next/router'
import Link from 'next/link'
import Fuse from 'fuse.js'
import { MagnifyingGlassIcon, XMarkIcon, DocumentTextIcon, ArrowRightIcon } from '@heroicons/react/24/outline'
import clsx from 'clsx'
import { searchData, searchOptions, popularSearches, quickLinks } from '@/lib/searchData'
import { trackSearchEvent } from '@/components/ui/Analytics'

// Search result component
function SearchResult({ result, onSelect, isSelected }) {
  const { item, matches } = result

  return (
    <Link
      href={item.url}
      onClick={onSelect}
      className={clsx(
        'block rounded-lg p-3 transition-colors',
        isSelected
          ? 'bg-primary-50 dark:bg-primary-900/20'
          : 'hover:bg-slate-50 dark:hover:bg-slate-700/50'
      )}
    >
      <div className="flex items-start gap-3">
        <DocumentTextIcon className="h-5 w-5 mt-0.5 text-slate-400 flex-shrink-0" />
        <div className="flex-1 min-w-0">
          <div className="flex items-center justify-between">
            <h3 className="font-medium text-slate-900 dark:text-white truncate">
              {item.title}
            </h3>
            <span className="text-xs text-slate-500 dark:text-slate-400 ml-2 flex-shrink-0">
              {item.section}
            </span>
          </div>
          <p className="mt-1 text-sm text-slate-600 dark:text-slate-400 line-clamp-2">
            {item.content}
          </p>
          {matches && matches.length > 0 && (
            <div className="mt-2 flex flex-wrap gap-1">
              {matches.slice(0, 3).map((match, index) => (
                <span
                  key={index}
                  className="inline-flex items-center px-2 py-1 rounded text-xs bg-primary-100 text-primary-700 dark:bg-primary-900/30 dark:text-primary-300"
                >
                  {match.key}: {match.value.substring(match.indices[0][0], match.indices[0][1] + 1)}
                </span>
              ))}
            </div>
          )}
        </div>
      </div>
    </Link>
  )
}

// Popular searches component
function PopularSearches({ onSearch }) {
  return (
    <div className="space-y-3">
      <h3 className="text-sm font-medium text-slate-900 dark:text-white">
        Popular Searches
      </h3>
      <div className="flex flex-wrap gap-2">
        {popularSearches.map((search) => (
          <button
            key={search}
            onClick={() => onSearch(search)}
            className="inline-flex items-center px-3 py-1.5 rounded-full text-sm bg-slate-100 text-slate-700 hover:bg-slate-200 dark:bg-slate-700 dark:text-slate-300 dark:hover:bg-slate-600 transition-colors"
          >
            {search}
          </button>
        ))}
      </div>
    </div>
  )
}

// Quick links component
function QuickLinks() {
  return (
    <div className="space-y-3">
      <h3 className="text-sm font-medium text-slate-900 dark:text-white">
        Quick Links
      </h3>
      <div className="space-y-1">
        {quickLinks.map((link) => (
          <Link
            key={link.url}
            href={link.url}
            className="flex items-center justify-between p-2 rounded-lg hover:bg-slate-50 dark:hover:bg-slate-700/50 transition-colors group"
          >
            <div>
              <div className="font-medium text-slate-900 dark:text-white text-sm">
                {link.title}
              </div>
              <div className="text-xs text-slate-500 dark:text-slate-400">
                {link.description}
              </div>
            </div>
            <ArrowRightIcon className="h-4 w-4 text-slate-400 group-hover:text-slate-600 dark:group-hover:text-slate-300 transition-colors" />
          </Link>
        ))}
      </div>
    </div>
  )
}

// Enhanced search box component
export function SearchBox({ className, placeholder = "Search documentation..." }) {
  const [isOpen, setIsOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [selectedIndex, setSelectedIndex] = useState(0)
  const inputRef = useRef(null)
  const router = useRouter()

  // Initialize Fuse.js for fuzzy search
  const fuse = useMemo(() => new Fuse(searchData, searchOptions), [])

  // Perform search
  const searchResults = useMemo(() => {
    if (!query.trim()) return []
    return fuse.search(query).slice(0, 8) // Limit to 8 results
  }, [query, fuse])

  // Handle keyboard shortcuts and navigation
  useEffect(() => {
    function handleKeyDown(event) {
      // Cmd/Ctrl + K to open search
      if ((event.metaKey || event.ctrlKey) && event.key === 'k') {
        event.preventDefault()
        setIsOpen(true)
        setTimeout(() => inputRef.current?.focus(), 100)
      }

      // Escape to close
      if (event.key === 'Escape') {
        setIsOpen(false)
        setQuery('')
        setSelectedIndex(0)
      }

      // Arrow key navigation when search is open
      if (isOpen && searchResults.length > 0) {
        if (event.key === 'ArrowDown') {
          event.preventDefault()
          setSelectedIndex(prev => (prev + 1) % searchResults.length)
        } else if (event.key === 'ArrowUp') {
          event.preventDefault()
          setSelectedIndex(prev => prev === 0 ? searchResults.length - 1 : prev - 1)
        } else if (event.key === 'Enter') {
          event.preventDefault()
          if (searchResults[selectedIndex]) {
            router.push(searchResults[selectedIndex].item.url)
            setIsOpen(false)
            setQuery('')
            setSelectedIndex(0)
          }
        }
      }
    }

    document.addEventListener('keydown', handleKeyDown)
    return () => document.removeEventListener('keydown', handleKeyDown)
  }, [isOpen, searchResults, selectedIndex, router])

  // Reset selected index when query changes
  useEffect(() => {
    setSelectedIndex(0)
  }, [query])

  // Close search on route change
  useEffect(() => {
    const handleRouteChange = () => {
      setIsOpen(false)
      setQuery('')
      setSelectedIndex(0)
    }

    router.events.on('routeChangeStart', handleRouteChange)
    return () => router.events.off('routeChangeStart', handleRouteChange)
  }, [router])

  const handleSubmit = (e) => {
    e.preventDefault()
    if (searchResults.length > 0) {
      router.push(searchResults[selectedIndex].item.url)
      setIsOpen(false)
      setQuery('')
      setSelectedIndex(0)
    }
  }

  const clearSearch = () => {
    setQuery('')
    setSelectedIndex(0)
    inputRef.current?.focus()
  }

  const handleSearchSelect = (search) => {
    setQuery(search)
    inputRef.current?.focus()
  }

  const handleResultSelect = (result = null) => {
    // Track search analytics
    if (query && result) {
      trackSearchEvent(query, searchResults, result.item)
    }

    setIsOpen(false)
    setQuery('')
    setSelectedIndex(0)
  }

  return (
    <div className={clsx('relative', className)}>
      {/* Search trigger button */}
      <button
        onClick={() => setIsOpen(true)}
        className={clsx(
          'flex w-full items-center gap-3 rounded-lg border border-gray-300 bg-white px-3 py-2 text-sm',
          'transition-all duration-200 hover:shadow-soft hover:-translate-y-0.5',
          'hover:border-gray-400 focus:border-primary-500 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
          'dark:border-gray-600 dark:bg-gray-800 dark:hover:border-gray-500 dark:focus:ring-offset-gray-900'
        )}
      >
        <MagnifyingGlassIcon className="h-4 w-4 text-slate-400" />
        <span className="flex-1 text-left text-slate-500 dark:text-slate-400">
          {placeholder}
        </span>
        <kbd className="hidden sm:inline-flex h-5 select-none items-center gap-1 rounded border border-slate-300 bg-slate-100 px-1.5 font-mono text-xs text-slate-600 dark:border-slate-600 dark:bg-slate-700 dark:text-slate-400">
          <span className="text-xs">⌘</span>K
        </kbd>
      </button>

      {/* Search modal overlay */}
      {isOpen && (
        <div className="fixed inset-0 z-50 flex items-start justify-center p-4 sm:p-6 md:p-20 animate-fade-in">
          {/* Backdrop */}
          <div
            className="fixed inset-0 bg-gray-900/50 backdrop-blur-md"
            onClick={() => setIsOpen(false)}
          />

          {/* Search modal */}
          <div className="relative w-full max-w-lg transform rounded-xl bg-white shadow-2xl ring-1 ring-black/5 dark:bg-gray-800 dark:ring-white/10 animate-scale-in">
            <form onSubmit={handleSubmit}>
              <div className="flex items-center border-b border-slate-200 dark:border-slate-700">
                <MagnifyingGlassIcon className="ml-4 h-5 w-5 text-slate-400" />
                <input
                  ref={inputRef}
                  type="text"
                  value={query}
                  onChange={(e) => setQuery(e.target.value)}
                  placeholder={placeholder}
                  className="flex-1 border-0 bg-transparent px-4 py-4 text-slate-900 placeholder-slate-500 focus:outline-none dark:text-white dark:placeholder-slate-400"
                  autoComplete="off"
                  autoCorrect="off"
                  autoCapitalize="off"
                  spellCheck="false"
                />
                {query && (
                  <button
                    type="button"
                    onClick={clearSearch}
                    className="mr-4 rounded-md p-1 text-slate-400 hover:text-slate-600 dark:hover:text-slate-300"
                  >
                    <XMarkIcon className="h-4 w-4" />
                  </button>
                )}
              </div>
            </form>

            {/* Search results */}
            <div className="max-h-96 overflow-y-auto p-4">
              {query ? (
                searchResults.length > 0 ? (
                  <div className="space-y-2">
                    <div className="flex items-center justify-between mb-3">
                      <p className="text-sm font-medium text-slate-900 dark:text-white">
                        Search Results
                      </p>
                      <span className="text-xs text-slate-500 dark:text-slate-400">
                        {searchResults.length} result{searchResults.length !== 1 ? 's' : ''}
                      </span>
                    </div>
                    {searchResults.map((result, index) => (
                      <SearchResult
                        key={result.item.id}
                        result={result}
                        onSelect={() => handleResultSelect(result)}
                        isSelected={index === selectedIndex}
                      />
                    ))}
                    <div className="mt-4 pt-3 border-t border-slate-200 dark:border-slate-700">
                      <p className="text-xs text-slate-500 dark:text-slate-400 text-center">
                        Use ↑↓ to navigate, Enter to select, Esc to close
                      </p>
                    </div>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <MagnifyingGlassIcon className="h-12 w-12 text-slate-300 dark:text-slate-600 mx-auto mb-3" />
                    <p className="text-sm font-medium text-slate-900 dark:text-white mb-1">
                      No results found
                    </p>
                    <p className="text-xs text-slate-500 dark:text-slate-400">
                      Try different keywords or check the spelling
                    </p>
                  </div>
                )
              ) : (
                <div className="space-y-6">
                  <PopularSearches onSearch={handleSearchSelect} />
                  <QuickLinks />
                </div>
              )}
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

// Compact search box for mobile
export function CompactSearchBox({ className }) {
  return (
    <button
      className={clsx(
        'flex items-center justify-center rounded-lg border border-slate-300 bg-white p-2 transition-colors',
        'hover:border-slate-400 focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500',
        'dark:border-slate-600 dark:bg-slate-800 dark:hover:border-slate-500',
        className
      )}
      aria-label="Search"
    >
      <MagnifyingGlassIcon className="h-5 w-5 text-slate-400" />
    </button>
  )
}
