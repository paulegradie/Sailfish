import { StarIcon } from '@heroicons/react/24/solid'

const testimonials = [
  {
    content: "Sailfish transformed our performance testing workflow. The statistical analysis capabilities gave us confidence in our optimizations, and the enterprise support was invaluable during our scaling phase.",
    author: {
      name: "Sarah Chen",
      role: "VP of Engineering",
      company: "TechFlow Financial",
      avatar: "SC"
    },
    metrics: {
      improvement: "60%",
      description: "reduction in API response time"
    }
  },
  {
    content: "The machine learning capabilities in ScaleFish were game-changing. We could predict exactly how our system would perform at different load levels and optimize accordingly.",
    author: {
      name: "Michael Rodriguez",
      role: "Principal Engineer", 
      company: "ShopStream",
      avatar: "MR"
    },
    metrics: {
      improvement: "10x",
      description: "traffic handled with zero downtime"
    }
  },
  {
    content: "Starting with Sailfish Enterprise early was one of our best technical decisions. As we scaled, we never had to worry about performance because we had complete visibility.",
    author: {
      name: "Alex Thompson",
      role: "Founder & CTO",
      company: "DataVault",
      avatar: "AT"
    },
    metrics: {
      improvement: "100x",
      description: "user growth without performance issues"
    }
  }
]

const companies = [
  { name: "TechFlow", logo: "TF" },
  { name: "ShopStream", logo: "SS" },
  { name: "DataVault", logo: "DV" },
  { name: "CloudSync", logo: "CS" },
  { name: "DevTools Pro", logo: "DP" },
  { name: "ScaleUp", logo: "SU" }
]

const stats = [
  { value: "1M+", label: "Performance tests run" },
  { value: "10K+", label: "Developers using Sailfish" },
  { value: "45%", label: "Average performance improvement" },
  { value: "99.9%", label: "Uptime for enterprise customers" }
]

function TestimonialCard({ testimonial }) {
  return (
    <div className="relative overflow-hidden rounded-2xl bg-white p-8 shadow-lg ring-1 ring-slate-200 dark:bg-slate-900 dark:ring-slate-800">
      {/* Quote decoration */}
      <div className="absolute top-6 right-6 text-6xl text-slate-100 dark:text-slate-800 font-serif">
        "
      </div>
      
      {/* Stars */}
      <div className="flex gap-1 mb-6">
        {[...Array(5)].map((_, i) => (
          <StarIcon key={i} className="h-5 w-5 text-yellow-400" />
        ))}
      </div>
      
      {/* Content */}
      <blockquote className="text-slate-700 dark:text-slate-300 leading-relaxed">
        {testimonial.content}
      </blockquote>
      
      {/* Metrics */}
      <div className="mt-6 rounded-lg bg-primary-50 p-4 dark:bg-primary-900/20">
        <div className="text-2xl font-bold text-primary-600 dark:text-primary-400">
          {testimonial.metrics.improvement}
        </div>
        <div className="text-sm text-primary-700 dark:text-primary-300">
          {testimonial.metrics.description}
        </div>
      </div>
      
      {/* Author */}
      <div className="mt-6 flex items-center gap-4">
        <div className="flex h-12 w-12 items-center justify-center rounded-full bg-gradient-to-br from-primary-500 to-secondary-500 text-white font-semibold">
          {testimonial.author.avatar}
        </div>
        <div>
          <div className="font-semibold text-slate-900 dark:text-white">
            {testimonial.author.name}
          </div>
          <div className="text-sm text-slate-600 dark:text-slate-400">
            {testimonial.author.role}
          </div>
          <div className="text-sm text-slate-500 dark:text-slate-500">
            {testimonial.author.company}
          </div>
        </div>
      </div>
    </div>
  )
}

function CompanyLogo({ company }) {
  return (
    <div className="flex items-center justify-center">
      <div className="flex h-16 w-24 items-center justify-center rounded-lg bg-slate-100 dark:bg-slate-800">
        <div className="text-lg font-bold text-slate-600 dark:text-slate-400">
          {company.logo}
        </div>
      </div>
    </div>
  )
}

function StatsGrid() {
  return (
    <div className="grid grid-cols-2 gap-8 md:grid-cols-4">
      {stats.map((stat) => (
        <div key={stat.label} className="text-center">
          <div className="text-3xl font-bold text-slate-900 dark:text-white md:text-4xl">
            {stat.value}
          </div>
          <div className="mt-2 text-sm text-slate-600 dark:text-slate-400">
            {stat.label}
          </div>
        </div>
      ))}
    </div>
  )
}

export function SocialProofSection() {
  return (
    <section className="py-24 sm:py-32 bg-slate-50 dark:bg-slate-900/50">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mx-auto max-w-2xl text-center">
          <h2 className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white sm:text-4xl">
            Trusted by developers worldwide
          </h2>
          <p className="mt-6 text-lg leading-8 text-slate-600 dark:text-slate-400">
            Join thousands of developers and hundreds of companies who rely on Sailfish for their performance testing needs.
          </p>
        </div>

        {/* Stats */}
        <div className="mt-16">
          <StatsGrid />
        </div>

        {/* Company logos */}
        <div className="mt-16">
          <p className="text-center text-sm font-semibold text-slate-600 dark:text-slate-400 mb-8">
            TRUSTED BY LEADING COMPANIES
          </p>
          <div className="grid grid-cols-3 gap-8 md:grid-cols-6">
            {companies.map((company) => (
              <CompanyLogo key={company.name} company={company} />
            ))}
          </div>
        </div>

        {/* Testimonials */}
        <div className="mt-24">
          <div className="grid gap-8 lg:grid-cols-3">
            {testimonials.map((testimonial, index) => (
              <TestimonialCard key={index} testimonial={testimonial} />
            ))}
          </div>
        </div>

        {/* CTA */}
        <div className="mt-16 text-center">
          <div className="inline-flex flex-col items-center gap-4 rounded-2xl bg-gradient-to-r from-primary-600 to-secondary-600 p-8 text-white sm:flex-row sm:gap-8">
            <div className="text-left">
              <h3 className="text-xl font-semibold">
                Ready to join them?
              </h3>
              <p className="mt-1 text-primary-100">
                Start your performance testing journey today
              </p>
            </div>
            <div className="flex gap-4">
              <a
                href="/docs/0/getting-started"
                className="rounded-lg bg-white px-6 py-3 font-semibold text-primary-600 transition-colors hover:bg-slate-100"
              >
                Get Started Free
              </a>
              <a
                href="/case-studies"
                className="rounded-lg border-2 border-white px-6 py-3 font-semibold text-white transition-colors hover:bg-white/10"
              >
                Read Case Studies
              </a>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}
