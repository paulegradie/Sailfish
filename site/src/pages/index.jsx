import Head from 'next/head'
import Link from 'next/link'
import { useState, useEffect } from 'react'
import clsx from 'clsx'
import { ModernHero } from '@/components/ui/ModernHero'
import { FeaturesSection } from '@/components/ui/FeaturesSection'
import { SocialProofSection } from '@/components/ui/SocialProofSection'
import { DualPathCTA } from '@/components/ui/DualPathCTA'
import { Logo } from '@/components/ui/Logo'
import { MainNavigation } from '@/components/ui/MainNavigation'
import { EnhancedMobileNavigation } from '@/components/ui/EnhancedMobileNavigation'
import { ThemeSelector } from '@/components/ThemeSelector'

// GitHub icon component
function GitHubIcon(props) {
  return (
    <svg viewBox="0 0 20 20" aria-hidden="true" {...props}>
      <path
        fillRule="evenodd"
        d="M10 1.667c-4.605 0-8.334 3.823-8.334 8.544 0 3.78 2.385 6.974 5.698 8.106.417.075.573-.182.573-.406 0-.203-.011-.875-.011-1.592-2.093.397-2.635-.522-2.802-1.002-.094-.246-.5-1.005-.854-1.207-.291-.16-.708-.556-.01-.567.656-.01 1.124.62 1.281.876.75 1.292 1.948.929 2.427.705.073-.555.291-.929.531-1.143-1.854-.213-3.791-.95-3.791-4.218 0-.929.322-1.698.854-2.296-.083-.214-.375-1.09.083-2.265 0 0 .698-.224 2.292.876a7.576 7.576 0 0 1 2.083-.288c.709 0 1.417.096 2.084.288 1.593-1.11 2.291-.875 2.291-.875.459 1.174.167 2.05.084 2.263.53.599.854 1.357.854 2.297 0 3.278-1.948 4.005-3.802 4.219.302.266.563.78.563 1.58 0 1.143-.011 2.061-.011 2.35 0 .224.156.491.573.405a8.365 8.365 0 0 0 4.11-3.116 8.707 8.707 0 0 0 1.567-4.99c0-4.721-3.73-8.545-8.334-8.545Z"
        clipRule="evenodd"
      />
    </svg>
  )
}

// Header component for the landing page
function Header() {
  let [isScrolled, setIsScrolled] = useState(false)

  useEffect(() => {
    function onScroll() {
      setIsScrolled(window.scrollY > 0)
    }
    onScroll()
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => {
      window.removeEventListener('scroll', onScroll)
    }
  }, [])

  return (
    <header
      className={clsx(
        'sticky top-0 z-50 bg-white/95 backdrop-blur-sm border-b border-slate-200/50 transition-all duration-300',
        'dark:bg-slate-900/95 dark:border-slate-800/50',
        isScrolled && 'shadow-sm'
      )}
    >
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 items-center justify-between">
          {/* Logo */}
          <div className="flex items-center">
            <Link href="/" className="flex items-center">
              <Logo size="md" />
            </Link>
          </div>

          {/* Main Navigation - Desktop */}
          <MainNavigation className="flex-1 justify-center" />

          {/* Right side actions */}
          <div className="flex items-center gap-4">
            <ThemeSelector />
            <Link
              href="https://github.com/paulegradie/Sailfish"
              className="group p-2 text-slate-400 hover:text-slate-500 dark:hover:text-slate-300 transition-colors duration-200"
              aria-label="GitHub"
              target="_blank"
              rel="noopener noreferrer"
            >
              <GitHubIcon className="h-5 w-5" />
            </Link>
            <EnhancedMobileNavigation />
          </div>
        </div>
      </div>
    </header>
  )
}

export default function HomePage() {
  return (
    <>
      <Head>
        <title>Sailfish - Performance Testing Made Simple</title>
        <meta
          name="description"
          content="Write performance tests that are simple, consistent, and familiar. Sailfish brings statistical analysis and machine learning to .NET performance testing."
        />
        <meta name="keywords" content="performance testing, .NET, C#, benchmarking, statistical analysis, machine learning, enterprise" />
        
        {/* Open Graph */}
        <meta property="og:title" content="Sailfish - Performance Testing Made Simple" />
        <meta property="og:description" content="Write performance tests that are simple, consistent, and familiar. Sailfish brings statistical analysis and machine learning to .NET performance testing." />
        <meta property="og:type" content="website" />
        <meta property="og:url" content="https://sailfish.dev" />
        
        {/* Twitter */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="Sailfish - Performance Testing Made Simple" />
        <meta name="twitter:description" content="Write performance tests that are simple, consistent, and familiar. Sailfish brings statistical analysis and machine learning to .NET performance testing." />
        
        {/* Structured Data */}
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{
            __html: JSON.stringify({
              "@context": "https://schema.org",
              "@type": "SoftwareApplication",
              "name": "Sailfish",
              "description": "Performance testing framework for .NET with statistical analysis and machine learning capabilities",
              "applicationCategory": "DeveloperApplication",
              "operatingSystem": "Cross-platform",
              "programmingLanguage": "C#",
              "offers": [
                {
                  "@type": "Offer",
                  "price": "0",
                  "priceCurrency": "USD",
                  "name": "Open Source"
                },
                {
                  "@type": "Offer", 
                  "price": "2000",
                  "priceCurrency": "USD",
                  "name": "Enterprise"
                }
              ]
            })
          }}
        />
      </Head>

      <Header />

      <div className="min-h-screen">
        {/* Hero Section */}
        <ModernHero />
        
        {/* Features Section */}
        <FeaturesSection />
        
        {/* Social Proof Section */}
        <SocialProofSection />
        
        {/* Dual Path CTA Section */}
        <DualPathCTA />
        
        {/* Final CTA Section */}
        <section className="bg-slate-900 py-16">
          <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
            <div className="text-center">
              <h2 className="text-3xl font-bold text-white sm:text-4xl">
                Ready to build faster applications?
              </h2>
              <p className="mt-4 text-xl text-slate-300">
                Join thousands of developers using Sailfish for performance testing
              </p>
              <div className="mt-8 flex flex-col gap-4 sm:flex-row sm:justify-center sm:gap-6">
                <a
                  href="/docs/0/getting-started"
                  className="inline-flex items-center justify-center rounded-lg bg-primary-600 px-8 py-4 text-lg font-semibold text-white transition-colors hover:bg-primary-700"
                >
                  Get Started Free
                </a>
                <a
                  href="/enterprise/contact"
                  className="inline-flex items-center justify-center rounded-lg border-2 border-slate-600 px-8 py-4 text-lg font-semibold text-white transition-colors hover:border-slate-500 hover:bg-slate-800"
                >
                  Talk to Sales
                </a>
              </div>
              <p className="mt-4 text-sm text-slate-400">
                No credit card required • 5-minute setup • Free forever
              </p>
            </div>
          </div>
        </section>
      </div>
    </>
  )
}
