import Link from 'next/link'
import { 
  CodeBracketIcon, 
  BuildingOfficeIcon,
  ArrowRightIcon,
  CheckIcon,
  PlayIcon
} from '@heroicons/react/24/outline'

const developerFeatures = [
  "Complete performance testing framework",
  "Statistical analysis & outlier detection", 
  "Machine learning complexity estimation",
  "Automated regression detection",
  "Multiple output formats",
  "Community support"
]

const enterpriseFeatures = [
  "Everything in Open Source",
  "Advanced analytics dashboard",
  "Team collaboration tools",
  "Priority support & training",
  "Custom integrations",
  "Compliance & security features"
]

function PathCard({ 
  title, 
  subtitle, 
  description, 
  features, 
  primaryCTA, 
  secondaryCTA, 
  icon: Icon,
  gradient,
  popular = false 
}) {
  return (
    <div className={`relative overflow-hidden rounded-3xl ${popular ? 'ring-2 ring-primary-500' : 'ring-1 ring-slate-200 dark:ring-slate-800'} bg-white dark:bg-slate-900 p-8 transition-all hover:shadow-xl`}>
      {popular && (
        <div className="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2">
          <div className="rounded-full bg-primary-500 px-4 py-1 text-sm font-semibold text-white">
            Most Popular
          </div>
        </div>
      )}
      
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <div className={`flex h-12 w-12 items-center justify-center rounded-xl ${gradient}`}>
          <Icon className="h-6 w-6 text-white" />
        </div>
        <div>
          <h3 className="text-xl font-bold text-slate-900 dark:text-white">
            {title}
          </h3>
          <p className="text-sm text-slate-600 dark:text-slate-400">
            {subtitle}
          </p>
        </div>
      </div>
      
      {/* Description */}
      <p className="text-slate-700 dark:text-slate-300 leading-relaxed mb-8">
        {description}
      </p>
      
      {/* Features */}
      <div className="space-y-3 mb-8">
        {features.map((feature, index) => (
          <div key={index} className="flex items-center gap-3">
            <CheckIcon className="h-5 w-5 text-green-500 flex-shrink-0" />
            <span className="text-sm text-slate-700 dark:text-slate-300">
              {feature}
            </span>
          </div>
        ))}
      </div>
      
      {/* CTAs */}
      <div className="space-y-3">
        <Link
          href={primaryCTA.href}
          className={`flex w-full items-center justify-center gap-2 rounded-lg px-6 py-3 font-semibold transition-all ${primaryCTA.className}`}
        >
          {primaryCTA.icon && <primaryCTA.icon className="h-5 w-5" />}
          {primaryCTA.text}
        </Link>
        
        {secondaryCTA && (
          <Link
            href={secondaryCTA.href}
            className={`flex w-full items-center justify-center gap-2 rounded-lg px-6 py-3 font-semibold transition-all ${secondaryCTA.className}`}
          >
            {secondaryCTA.icon && <secondaryCTA.icon className="h-5 w-5" />}
            {secondaryCTA.text}
          </Link>
        )}
      </div>
    </div>
  )
}

export function DualPathCTA() {
  return (
    <section className="py-24 sm:py-32">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mx-auto max-w-2xl text-center mb-16">
          <h2 className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white sm:text-4xl">
            Choose your path
          </h2>
          <p className="mt-6 text-lg leading-8 text-slate-600 dark:text-slate-400">
            Whether you're an individual developer or part of a large organization, we have the right solution for your performance testing needs.
          </p>
        </div>

        {/* Path cards */}
        <div className="grid gap-8 lg:grid-cols-2 lg:gap-12">
          {/* Developer Path */}
          <PathCard
            title="For Developers"
            subtitle="Individual developers & small teams"
            description="Get started with the complete Sailfish performance testing framework. Perfect for individual developers, consultants, and small teams building high-performance applications."
            features={developerFeatures}
            icon={CodeBracketIcon}
            gradient="bg-gradient-to-br from-blue-500 to-cyan-500"
            primaryCTA={{
              text: "Get Started Free",
              href: "/docs/0/getting-started",
              icon: ArrowRightIcon,
              className: "bg-blue-600 text-white hover:bg-blue-700 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            }}
            secondaryCTA={{
              text: "View Documentation",
              href: "/docs",
              className: "border-2 border-blue-600 text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            }}
          />

          {/* Enterprise Path */}
          <PathCard
            title="For Enterprise"
            subtitle="Growing companies & large organizations"
            description="Scale your performance testing with enterprise-grade features, priority support, and advanced analytics. Perfect for companies that need more than open source tools."
            features={enterpriseFeatures}
            icon={BuildingOfficeIcon}
            gradient="bg-gradient-to-br from-green-500 to-emerald-500"
            popular={true}
            primaryCTA={{
              text: "Schedule Demo",
              href: "/enterprise/contact",
              icon: PlayIcon,
              className: "bg-green-600 text-white hover:bg-green-700 focus:ring-2 focus:ring-green-500 focus:ring-offset-2"
            }}
            secondaryCTA={{
              text: "View Pricing",
              href: "/pricing",
              className: "border-2 border-green-600 text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20 focus:ring-2 focus:ring-green-500 focus:ring-offset-2"
            }}
          />
        </div>

        {/* Bottom CTA */}
        <div className="mt-16 text-center">
          <div className="inline-flex items-center gap-2 text-sm text-slate-600 dark:text-slate-400">
            <div className="h-2 w-2 rounded-full bg-green-500" />
            <span>No credit card required</span>
            <div className="h-1 w-1 rounded-full bg-slate-400" />
            <span>5-minute setup</span>
            <div className="h-1 w-1 rounded-full bg-slate-400" />
            <span>Free forever</span>
          </div>
        </div>

        {/* Migration note */}
        <div className="mt-12 rounded-2xl bg-slate-100 p-8 dark:bg-slate-800">
          <div className="text-center">
            <h3 className="text-lg font-semibold text-slate-900 dark:text-white mb-2">
              Migrating from another tool?
            </h3>
            <p className="text-slate-600 dark:text-slate-400 mb-4">
              We have migration guides for BenchmarkDotNet, NBomber, k6, and other popular performance testing tools.
            </p>
            <Link
              href="/comparison"
              className="inline-flex items-center gap-2 text-primary-600 hover:text-primary-700 dark:text-primary-400 dark:hover:text-primary-300 font-medium"
            >
              View comparison guide
              <ArrowRightIcon className="h-4 w-4" />
            </Link>
          </div>
        </div>
      </div>
    </section>
  )
}
